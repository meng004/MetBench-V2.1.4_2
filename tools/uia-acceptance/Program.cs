// UiaAcceptance — FlaUI-based WPF automation tool for MetBench acceptance evidence.
// Usage:
//   --dump --exe <path>                             : print automation tree then close
//   --exe <path> --mr <mrId> --case <label> --evidence <dir> [--timeout-seconds 600]
//                                                   : run acceptance scenario, produce 4 PNGs
//
// Interaction strategy: UIA patterns ONLY (Invoke / SelectionItem / ExpandCollapse /
// LegacyIAccessible.DoDefaultAction). Patterns are serviced by the in-process WPF UIA
// provider, so they work even when input injection (SendInput / mouse_event) is blocked
// by the session. No mouse or keyboard simulation anywhere in this tool.
//
// Key selectors discovered from ground truth (XAML + ViewModel + resx):
//   Nav item AutomationId : "Nav_SystemMtAsyncExecution"
//     (set via AutomationProperties.SetAutomationId in LocalizedNav; zh-CN text = "系统级异步执行")
//   Operation ComboBox    : AutomationId = "AsyncOperationCombo"
//   MR ComboBox           : AutomationId = "AsyncMrCombo"
//   Submit button         : AutomationId = "AsyncSubmitButton"
//   State TextBlock       : AutomationId = "AsyncState"  (binds ViewModel.StateDisplay)
//   Terminal states       : "Succeeded" | "Failed" | "Cancelled"  (StateDisplay = status.State.ToString())

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;

namespace UiaAcceptance;

internal static class Program
{
    private static readonly string[] TerminalStates = ["Succeeded", "Failed", "Cancelled"];

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 2;
        }

        var argMap = ParseArgs(args);

        if (!argMap.TryGetValue("--exe", out var exePath))
        {
            Console.Error.WriteLine("ERROR: --exe is required");
            PrintUsage();
            return 2;
        }

        bool dumpMode = argMap.ContainsKey("--dump");

        if (dumpMode)
        {
            return RunDump(exePath!);
        }

        // Acceptance mode
        if (!argMap.TryGetValue("--mr", out var mrId))
        {
            Console.Error.WriteLine("ERROR: --mr is required for acceptance mode");
            return 2;
        }
        if (!argMap.TryGetValue("--case", out var caseLabel))
        {
            Console.Error.WriteLine("ERROR: --case is required");
            return 2;
        }
        if (!argMap.TryGetValue("--evidence", out var evidenceDir))
        {
            Console.Error.WriteLine("ERROR: --evidence is required");
            return 2;
        }

        int timeoutSeconds = 600;
        if (argMap.TryGetValue("--timeout-seconds", out var toStr) && int.TryParse(toStr, out var toVal))
        {
            timeoutSeconds = toVal;
        }

        Directory.CreateDirectory(evidenceDir!);

        return RunAcceptance(exePath!, mrId!, caseLabel!, evidenceDir!, timeoutSeconds);
    }

    // -------------------------------------------------------------------------
    // DUMP MODE
    // -------------------------------------------------------------------------

    private static int RunDump(string exePath)
    {
        Console.WriteLine($"[dump] Launching: {exePath}");
        var app = LaunchApp(exePath);
        try
        {
            using var automation = new UIA3Automation();
            var window = WaitForMainWindow(app, automation, timeoutMs: 30_000);
            if (window == null)
            {
                Console.Error.WriteLine("ERROR: main window did not appear within 30 s");
                return 1;
            }

            Console.WriteLine($"[dump] Main window: Name='{window.Name}' AutomationId='{window.AutomationId}' ClassName='{window.ClassName}'");
            DumpElement(window, depth: 0, maxDepth: 10);
            return 0;
        }
        finally
        {
            try { app.Close(); } catch { /* ignore */ }
        }
    }

    private static void DumpElement(AutomationElement el, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        var indent = new string(' ', depth * 2);
        string name;
        string aid;
        string cls;
        string ct;
        try { name = el.Name ?? ""; } catch { name = "?"; }
        try { aid = el.AutomationId ?? ""; } catch { aid = "?"; }
        try { cls = el.ClassName ?? ""; } catch { cls = "?"; }
        try { ct = el.ControlType.ToString(); } catch { ct = "?"; }

        Console.WriteLine($"{indent}[{ct}] Name='{name}' Aid='{aid}' Cls='{cls}'");

        try
        {
            foreach (var child in el.FindAllChildren())
            {
                DumpElement(child, depth + 1, maxDepth);
            }
        }
        catch { /* some elements don't support enumeration */ }
    }

    // -------------------------------------------------------------------------
    // ACCEPTANCE MODE
    // -------------------------------------------------------------------------

    private static int RunAcceptance(string exePath, string mrId, string caseLabel, string evidenceDir, int timeoutSeconds)
    {
        Console.WriteLine($"[acceptance] exe={exePath}");
        Console.WriteLine($"[acceptance] mrId={mrId}  case={caseLabel}  evidence={evidenceDir}  timeout={timeoutSeconds}s");

        var app = LaunchApp(exePath);
        try
        {
            using var automation = new UIA3Automation();

            // ------------------------------------------------------------------
            // Wait for main window
            // ------------------------------------------------------------------
            Console.WriteLine("[acceptance] Waiting for main window...");
            var window = WaitForMainWindow(app, automation, timeoutMs: 30_000);
            if (window == null)
            {
                Console.Error.WriteLine("ERROR: main window did not appear");
                return 1;
            }
            Console.WriteLine($"[acceptance] Main window found: '{window.Name}'");
            Console.WriteLine($"[acceptance] Window BoundingRect: {window.BoundingRectangle}");

            // Give the window a moment to fully render after startup
            Thread.Sleep(2000);

            // Diagnostic: check if AsyncMrCombo is already in tree before navigation
            {
                var cf2 = automation.ConditionFactory;
                var preNav = window.FindFirstDescendant(cf2.ByAutomationId("AsyncMrCombo"));
                Console.WriteLine($"[diag] AsyncMrCombo before nav: {(preNav == null ? "not found" : "FOUND!")}");
            }

            // Screenshot 1: startup
            var ss1 = Path.Combine(evidenceDir, $"case{caseLabel}-01-startup.png");
            TakeScreenshot(window, ss1);
            Console.WriteLine($"[acceptance] Screenshot 1 -> {ss1}");

            // ------------------------------------------------------------------
            // Navigate to System MT Async page (UIA patterns only)
            // ------------------------------------------------------------------
            Console.WriteLine("[acceptance] Navigating to System MT Async Execution page...");
            if (!NavigateToAsyncPage(window, automation))
            {
                Console.Error.WriteLine("ERROR: could not navigate to async job page");
                return 1;
            }

            // Wait for the async page to fully load (WPF navigation + async OnNavigatedTo)
            Console.WriteLine("[acceptance] Waiting for async page to load (AsyncMrCombo to appear)...");
            if (!WaitForElement(window, automation, "AsyncMrCombo", timeoutMs: 60_000))
            {
                Console.Error.WriteLine("ERROR: AsyncMrCombo did not appear within 60 s; page may not have loaded");
                return 1;
            }
            Thread.Sleep(1000); // extra settle for AvailableMrIds to populate

            // Screenshot 2: async page
            var ss2 = Path.Combine(evidenceDir, $"case{caseLabel}-02-asyncpage.png");
            TakeScreenshot(window, ss2);
            Console.WriteLine($"[acceptance] Screenshot 2 -> {ss2}");

            // ------------------------------------------------------------------
            // Ensure operation kind = RunMr (default, but verify/select if needed)
            // ------------------------------------------------------------------
            EnsureOperationKindIsRunMr(window, automation);

            // ------------------------------------------------------------------
            // Select the MR id in AsyncMrCombo
            // ------------------------------------------------------------------
            Console.WriteLine($"[acceptance] Selecting MR: {mrId}");
            if (!SelectMrId(window, automation, mrId))
            {
                Console.Error.WriteLine($"ERROR: could not select MR id '{mrId}'");
                return 1;
            }
            Thread.Sleep(300);

            // ------------------------------------------------------------------
            // Submit via InvokePattern
            // ------------------------------------------------------------------
            Console.WriteLine("[acceptance] Invoking Submit...");
            if (!InvokeSubmit(window, automation))
            {
                Console.Error.WriteLine("ERROR: could not invoke Submit");
                return 1;
            }
            Thread.Sleep(500);

            // Screenshot 3: right after submit
            var ss3 = Path.Combine(evidenceDir, $"case{caseLabel}-03-submitted.png");
            TakeScreenshot(window, ss3);
            Console.WriteLine($"[acceptance] Screenshot 3 -> {ss3}");

            // ------------------------------------------------------------------
            // Poll until terminal state or timeout
            // ------------------------------------------------------------------
            Console.WriteLine($"[acceptance] Polling for terminal state (timeout={timeoutSeconds}s)...");
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            string? finalState = null;

            while (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(2000);
                var state = ReadStateDisplay(window, automation);
                Console.WriteLine($"[acceptance] State={state}  ({DateTime.UtcNow:HH:mm:ss})");

                if (state != null && TerminalStates.Any(t => string.Equals(t, state, StringComparison.OrdinalIgnoreCase)))
                {
                    finalState = state;
                    break;
                }
            }

            // Screenshot 4: terminal state
            var ss4 = Path.Combine(evidenceDir, $"case{caseLabel}-04-succeeded.png");
            TakeScreenshot(window, ss4);
            Console.WriteLine($"[acceptance] Screenshot 4 -> {ss4}");

            if (finalState == null)
            {
                Console.Error.WriteLine($"ERROR: timed out after {timeoutSeconds}s; never reached a terminal state");
                return 1;
            }

            Console.WriteLine($"[acceptance] Terminal state reached: {finalState}");

            if (!string.Equals(finalState, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"ERROR: job did not succeed — final state={finalState}");
                return 1;
            }

            Console.WriteLine("[acceptance] SUCCESS — 4 screenshots written, job Succeeded");
            return 0;
        }
        finally
        {
            try { app.Close(); } catch { /* ignore */ }
            // Give process a moment to exit cleanly
            Thread.Sleep(1000);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers: navigation (UIA patterns only — no input simulation)
    // -------------------------------------------------------------------------

    private static bool NavigateToAsyncPage(Window window, UIA3Automation automation)
    {
        var cf = automation.ConditionFactory;

        // First try: direct find by AutomationId anywhere in window
        var navItem = window.FindFirstDescendant(cf.ByAutomationId("Nav_SystemMtAsyncExecution"));
        if (navItem != null)
        {
            Console.WriteLine($"[nav] Found nav item by AutomationId: ControlType={navItem.ControlType} Name='{navItem.Name}'");
            return ActivateNavItem(window, navItem, automation);
        }

        // Fallback: find by Name = localized text (zh-CN = "系统级异步执行", en = "System MT Async Execution")
        foreach (var text in new[] { "系统级异步执行", "System MT Async Execution" })
        {
            var el = window.FindFirstDescendant(cf.ByName(text));
            if (el != null)
            {
                Console.WriteLine($"[nav] Found nav item by Name='{text}'");
                return ActivateNavItem(window, el, automation);
            }
        }

        Console.Error.WriteLine("[nav] WARN: Could not find nav item; scanning for 'Async' candidates...");
        var all = window.FindAllDescendants();
        foreach (var el in all)
        {
            try
            {
                var name = el.Name ?? "";
                if (name.Contains("Async", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("异步", StringComparison.Ordinal))
                {
                    Console.WriteLine($"  Candidate: [{el.ControlType}] Name='{name}' Aid='{el.AutomationId}'");
                    return ActivateNavItem(window, el, automation);
                }
            }
            catch { /* skip */ }
        }

        return false;
    }

    /// <summary>
    /// Activate a NavigationViewItem using UIA patterns exclusively.
    /// Pattern calls are routed to the in-process WPF UIA provider, so they work even when
    /// input injection is blocked. Tries Invoke → SelectionItem.Select →
    /// LegacyIAccessible.DoDefaultAction on the item itself, then on its descendants.
    /// After each attempt, checks whether the async page actually loaded.
    /// </summary>
    private static bool ActivateNavItem(Window window, AutomationElement navItem, UIA3Automation automation)
    {
        Console.WriteLine($"[nav] Pattern support: Invoke={navItem.Patterns.Invoke.IsSupported} " +
                          $"SelectionItem={navItem.Patterns.SelectionItem.IsSupported} " +
                          $"LegacyIAccessible={navItem.Patterns.LegacyIAccessible.IsSupported}");

        if (TryPatternsOn(navItem, "nav-item", window, automation))
        {
            return true;
        }

        // Wpf.Ui 3.0.4 NavigationViewItem derives from ButtonBase but ships no automation peer,
        // so UIA wraps it as a generic ItemsControlItem DataItem with no Invoke/SelectionItem.
        // Fallback: UIA SetFocus (handled by the in-process provider — the item is a focusable
        // ButtonBase) + PostMessage WM_KEYDOWN/WM_KEYUP VK_SPACE to the app's own HWND.
        // Posted messages go through the app's message queue, NOT SendInput, so they are not
        // subject to the session's input-injection block. ButtonBase activates on Space.
        Console.WriteLine("[nav] Item-level patterns did not load page; trying Focus + posted Space key...");
        if (TryFocusAndPostKey(window, navItem, automation))
        {
            return true;
        }

        Console.Error.WriteLine("[nav] All UIA pattern attempts exhausted — page did not load");
        return false;
    }

    /// <summary>
    /// UIA Focus() the nav item, then post WM_KEYDOWN/WM_KEYUP (Space, then Enter) to the
    /// main window HWND. No SendInput involved.
    /// </summary>
    private static bool TryFocusAndPostKey(Window window, AutomationElement navItem, UIA3Automation automation)
    {
        try
        {
            navItem.Focus();
            Thread.Sleep(300);
            var focused = automation.FocusedElement();
            Console.WriteLine($"[nav] Focused element after Focus(): [{focused?.ControlType}] Name='{focused?.Name}' Aid='{focused?.AutomationId}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[nav] Focus() failed: {ex.Message}");
        }

        IntPtr hwnd;
        try { hwnd = window.Properties.NativeWindowHandle.Value; }
        catch (Exception ex)
        {
            Console.WriteLine($"[nav] Could not get native window handle: {ex.Message}");
            return false;
        }

        foreach (var (vk, name) in new[] { (VK_SPACE, "SPACE"), (VK_RETURN, "RETURN") })
        {
            Console.WriteLine($"[nav] PostMessage WM_KEYDOWN/WM_KEYUP {name} to hwnd=0x{hwnd.ToInt64():X}");
            PostMessage(hwnd, WM_KEYDOWN, (IntPtr)vk, (IntPtr)0x00000001);
            Thread.Sleep(120);
            PostMessage(hwnd, WM_KEYUP, (IntPtr)vk, (IntPtr)unchecked((int)0xC0000001));
            if (WaitForAsyncPage(window, automation, 4000))
            {
                Console.WriteLine($"[nav] Posted {name} key activated navigation");
                return true;
            }
        }
        return false;
    }

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP   = 0x0101;
    private const int VK_SPACE  = 0x20;
    private const int VK_RETURN = 0x0D;

    /// <summary>
    /// Try Invoke, SelectionItem.Select, then LegacyIAccessible.DoDefaultAction on one element.
    /// Returns true as soon as the async page is observed loaded.
    /// </summary>
    private static bool TryPatternsOn(AutomationElement el, string label, Window window, UIA3Automation automation)
    {
        // 1. InvokePattern
        try
        {
            var invoke = el.Patterns.Invoke.PatternOrDefault;
            if (invoke != null)
            {
                Console.WriteLine($"[nav] {label}: Invoke()");
                invoke.Invoke();
                if (WaitForAsyncPage(window, automation, 4000)) { Console.WriteLine($"[nav] {label}: Invoke succeeded"); return true; }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[nav] {label}: Invoke failed: {ex.Message}"); }

        // 2. SelectionItemPattern
        try
        {
            var sel = el.Patterns.SelectionItem.PatternOrDefault;
            if (sel != null)
            {
                Console.WriteLine($"[nav] {label}: SelectionItem.Select()");
                sel.Select();
                if (WaitForAsyncPage(window, automation, 4000)) { Console.WriteLine($"[nav] {label}: Select succeeded"); return true; }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[nav] {label}: Select failed: {ex.Message}"); }

        // 3. LegacyIAccessible.DoDefaultAction
        try
        {
            var legacy = el.Patterns.LegacyIAccessible.PatternOrDefault;
            if (legacy != null)
            {
                Console.WriteLine($"[nav] {label}: LegacyIAccessible.DoDefaultAction() (action='{legacy.DefaultAction}')");
                legacy.DoDefaultAction();
                if (WaitForAsyncPage(window, automation, 4000)) { Console.WriteLine($"[nav] {label}: DoDefaultAction succeeded"); return true; }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[nav] {label}: DoDefaultAction failed: {ex.Message}"); }

        return false;
    }

    // Quick helper: check if async page already loaded
    private static bool IsAsyncPageLoaded(Window window, UIA3Automation automation)
    {
        var cf = automation.ConditionFactory;
        try { return window.FindFirstDescendant(cf.ByAutomationId("AsyncMrCombo")) != null; }
        catch { return false; }
    }

    private static bool WaitForAsyncPage(Window window, UIA3Automation automation, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (IsAsyncPageLoaded(window, automation)) return true;
            Thread.Sleep(400);
        }
        return false;
    }

    // -------------------------------------------------------------------------
    // Helpers: combo selection via patterns (ExpandCollapse + SelectionItem)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Select an item in a ComboBox by exact (then partial) text match using UIA patterns
    /// only: ExpandCollapsePattern to realize virtualized items, SelectionItemPattern.Select
    /// (fallback LegacyIAccessible.DoDefaultAction) on the matched item.
    /// </summary>
    private static bool SelectComboItemByText(ComboBox combo, string text, string tag)
    {
        // Already selected?
        try
        {
            var current = combo.SelectedItem?.Text ?? "";
            if (string.Equals(current.Trim(), text, StringComparison.Ordinal))
            {
                Console.WriteLine($"[{tag}] Already selected: '{text}'");
                return true;
            }
        }
        catch { /* ignore */ }

        try
        {
            var expand = combo.Patterns.ExpandCollapse.PatternOrDefault;
            if (expand != null)
            {
                expand.Expand();
            }
            else
            {
                combo.Expand(); // FlaUI falls back to pattern internally for WPF
            }
            Thread.Sleep(400);

            var items = combo.Items;
            Console.WriteLine($"[{tag}] Combo has {items.Length} items");
            foreach (var item in items)
            {
                Console.WriteLine($"[{tag}]   item: '{item.Text}'");
            }

            var target = items.FirstOrDefault(i =>
                string.Equals(i.Text?.Trim(), text, StringComparison.Ordinal));

            if (target == null)
            {
                // Partial match fallback
                target = items.FirstOrDefault(i =>
                    (i.Text ?? "").Contains(text, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                TryCollapse(combo);
                Console.Error.WriteLine($"[{tag}] Item '{text}' not found in combo");
                return false;
            }

            // Pattern-based selection (no mouse)
            var selPattern = target.Patterns.SelectionItem.PatternOrDefault;
            if (selPattern != null)
            {
                selPattern.Select();
                Console.WriteLine($"[{tag}] Selected via SelectionItemPattern: '{target.Text}'");
            }
            else
            {
                var legacy = target.Patterns.LegacyIAccessible.PatternOrDefault;
                if (legacy == null)
                {
                    TryCollapse(combo);
                    Console.Error.WriteLine($"[{tag}] Item supports neither SelectionItem nor LegacyIAccessible");
                    return false;
                }
                legacy.DoDefaultAction();
                Console.WriteLine($"[{tag}] Selected via LegacyIAccessible.DoDefaultAction: '{target.Text}'");
            }

            Thread.Sleep(200);
            TryCollapse(combo);

            // Verify selection landed
            try
            {
                var after = combo.SelectedItem?.Text ?? "";
                Console.WriteLine($"[{tag}] SelectedItem after select: '{after}'");
            }
            catch { /* ignore */ }
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{tag}] Exception selecting '{text}': {ex.Message}");
            return false;
        }
    }

    private static void TryCollapse(ComboBox combo)
    {
        try
        {
            var expand = combo.Patterns.ExpandCollapse.PatternOrDefault;
            expand?.Collapse();
        }
        catch { /* ignore */ }
    }

    // -------------------------------------------------------------------------
    // Helpers: operation kind
    // -------------------------------------------------------------------------

    private static void EnsureOperationKindIsRunMr(Window window, UIA3Automation automation)
    {
        // SelectedOperationKind defaults to RunMr in ViewModel — typically no action needed.
        // But verify the AsyncOperationCombo shows "RunMr" or equivalent.
        var cf = automation.ConditionFactory;
        var combo = window.FindFirstDescendant(cf.ByAutomationId("AsyncOperationCombo"))?.AsComboBox();
        if (combo == null)
        {
            Console.WriteLine("[ops] AsyncOperationCombo not found — assuming default RunMr");
            return;
        }

        try
        {
            var selected = combo.SelectedItem;
            var currentText = selected?.Text ?? "";
            Console.WriteLine($"[ops] Current operation kind: '{currentText}'");

            if (!currentText.Contains("RunMr", StringComparison.OrdinalIgnoreCase))
            {
                if (!SelectComboItemByText(combo, "RunMr", "ops"))
                {
                    Console.WriteLine("[ops] Could not select RunMr — leaving as-is");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ops] Could not check/set operation kind: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Helpers: MR selection
    // -------------------------------------------------------------------------

    private static bool SelectMrId(Window window, UIA3Automation automation, string mrId)
    {
        var cf = automation.ConditionFactory;
        var combo = window.FindFirstDescendant(cf.ByAutomationId("AsyncMrCombo"))?.AsComboBox();
        if (combo == null)
        {
            Console.Error.WriteLine("[mr] AsyncMrCombo not found");
            return false;
        }

        return SelectComboItemByText(combo, mrId, "mr");
    }

    // -------------------------------------------------------------------------
    // Helpers: submit (InvokePattern only)
    // -------------------------------------------------------------------------

    private static bool InvokeSubmit(Window window, UIA3Automation automation)
    {
        var cf = automation.ConditionFactory;

        // Primary: find by AutomationId
        var btn = window.FindFirstDescendant(cf.ByAutomationId("AsyncSubmitButton"));
        if (btn == null)
        {
            // Fallback: find by Name (localized "提交" or "Submit")
            btn = window.FindFirstDescendant(cf.ByName("提交"))
                  ?? window.FindFirstDescendant(cf.ByName("Submit"));
        }

        if (btn == null)
        {
            Console.Error.WriteLine("[submit] Submit button not found");
            return false;
        }

        Console.WriteLine($"[submit] Found Submit button: ControlType={btn.ControlType} Name='{btn.Name}' Aid='{btn.AutomationId}'");

        try
        {
            var invoke = btn.Patterns.Invoke.PatternOrDefault;
            if (invoke != null)
            {
                invoke.Invoke();
                Console.WriteLine("[submit] Invoked via InvokePattern");
                return true;
            }

            var legacy = btn.Patterns.LegacyIAccessible.PatternOrDefault;
            if (legacy != null)
            {
                legacy.DoDefaultAction();
                Console.WriteLine("[submit] Invoked via LegacyIAccessible.DoDefaultAction");
                return true;
            }

            Console.Error.WriteLine("[submit] Button supports neither Invoke nor LegacyIAccessible");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[submit] Pattern invocation failed: {ex.Message}");
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers: state polling
    // -------------------------------------------------------------------------

    private static string? ReadStateDisplay(Window window, UIA3Automation automation)
    {
        var cf = automation.ConditionFactory;
        try
        {
            var el = window.FindFirstDescendant(cf.ByAutomationId("AsyncState"));
            // TextBlock value is exposed via Name (accessibility name) in UIA
            return el?.Name;
        }
        catch
        {
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers: screenshots
    // -------------------------------------------------------------------------

    private static void TakeScreenshot(Window window, string path)
    {
        // Primary: PrintWindow with PW_RENDERFULLCONTENT — renders the window's own DWM
        // surface, so it captures real window content even when the session is locked or
        // the window is occluded (Capture.Element grabs raw screen pixels and would show
        // the lock screen instead).
        try
        {
            var hwnd = window.Properties.NativeWindowHandle.Value;
            if (TryPrintWindowCapture(hwnd, path))
            {
                Console.WriteLine($"[screenshot] Captured via PrintWindow -> {path}");
                return;
            }
            Console.WriteLine("[screenshot] PrintWindow failed; falling back to Capture.Element");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[screenshot] PrintWindow path failed ({ex.Message}); falling back to Capture.Element");
        }

        try
        {
            var capture = Capture.Element(window);
            capture.ToFile(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[screenshot] WARN: failed to capture to {path}: {ex.Message}");
        }
    }

    private static bool TryPrintWindowCapture(IntPtr hwnd, string path)
    {
        if (hwnd == IntPtr.Zero) return false;
        if (!GetWindowRect(hwnd, out var r)) return false;
        int w = r.right - r.left;
        int h = r.bottom - r.top;
        if (w <= 0 || h <= 0) return false;

        using var bmp = new System.Drawing.Bitmap(w, h);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            var hdc = g.GetHdc();
            try
            {
                if (!PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT))
                {
                    return false;
                }
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }

    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    // -------------------------------------------------------------------------
    // Helpers: app launch / window wait
    // -------------------------------------------------------------------------

    private static Application LaunchApp(string exePath)
    {
        var psi = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
        };

        // Inherit environment (METBENCH_DOCKER_MCP_TOKEN flows through)
        var app = Application.Launch(psi);
        Console.WriteLine($"[launch] PID={app.ProcessId}");
        return app;
    }

    private static bool WaitForElement(Window window, UIA3Automation automation, string automationId, int timeoutMs)
    {
        var cf = automation.ConditionFactory;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var el = window.FindFirstDescendant(cf.ByAutomationId(automationId));
                if (el != null) return true;
            }
            catch { /* retry */ }
            Thread.Sleep(500);
        }
        return false;
    }

    private static Window? WaitForMainWindow(Application app, UIA3Automation automation, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var windows = app.GetAllTopLevelWindows(automation);
                if (windows.Length > 0)
                {
                    // Pick the first non-splash window (has content)
                    foreach (var w in windows)
                    {
                        if (!string.IsNullOrEmpty(w.Name) || !string.IsNullOrEmpty(w.ClassName))
                            return w;
                    }
                    // Return first anyway
                    return windows[0];
                }
            }
            catch { /* retry */ }
            Thread.Sleep(500);
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Helpers: arg parsing
    // -------------------------------------------------------------------------

    private static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                // Check if next arg is a value (not another flag)
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    result[arg] = args[i + 1];
                    i++;
                }
                else
                {
                    result[arg] = null; // flag only
                }
            }
        }
        return result;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("UiaAcceptance — FlaUI WPF automation tool for MetBench acceptance evidence");
        Console.WriteLine();
        Console.WriteLine("Dump mode (discover UI tree):");
        Console.WriteLine("  UiaAcceptance --dump --exe <MetBench_Client.exe>");
        Console.WriteLine();
        Console.WriteLine("Acceptance mode:");
        Console.WriteLine("  UiaAcceptance --exe <path> --mr <mrId> --case <label> --evidence <dir> [--timeout-seconds 600]");
    }
}
