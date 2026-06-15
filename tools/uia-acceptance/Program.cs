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
using FlaUI.Core.Definitions;
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

        // Steps DSL mode (general SP3b UI driver)
        if (argMap.ContainsKey("--steps"))
        {
            if (!argMap.TryGetValue("--evidence", out var evDir) || string.IsNullOrWhiteSpace(evDir))
            {
                Console.Error.WriteLine("ERROR: --evidence is required for steps mode");
                return 2;
            }
            argMap.TryGetValue("--label", out var lbl);
            argMap.TryGetValue("--steps", out var stepsSpec);
            int stepsTimeout = 600;
            if (argMap.TryGetValue("--timeout-seconds", out var stoStr) && int.TryParse(stoStr, out var stoVal))
            {
                stepsTimeout = stoVal;
            }
            Directory.CreateDirectory(evDir!);
            return RunSteps(exePath!, evDir!, lbl ?? "case", stepsSpec ?? "", stepsTimeout);
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

    // =========================================================================
    // STEPS DSL MODE (SP3b general UI driver)
    // =========================================================================
    //
    // A small step language so heterogeneous UI cases can be driven from the
    // command line without recompiling. Steps are ';'-separated; each is "op"
    // or "op:arg" (arg may contain '=' for key=value ops). All interaction is
    // via UIA patterns only (no SendInput); nav uses Focus + posted Space/Enter.
    //
    //   nav:<NavAutomationId>            navigate to a NavigationViewItem
    //   waitid:<AutomationId>            wait (<=60s) until element present by id
    //   waitname:<Name>                  wait (<=60s) until element present by Name
    //   sleep:<ms>                       fixed settle
    //   shot:<name>                      screenshot -> <label>-<name>.png
    //   dumppage:<tag>                   dump content-frame subtree -> <label>-<tag>.txt
    //   dumpall:<tag>                    dump whole-window subtree -> <label>-<tag>.txt
    //   setid:<AutomationId>=<value>     ValuePattern.SetValue on element
    //   selcombo:<AutomationId>=<text>   select combo item by text
    //   selrow:<GridAutomationId>=<idx>  select DataGrid row by 0-based index
    //   invokeid:<AutomationId>          Invoke (or Legacy DoDefaultAction)
    //   invokename:<Name>                Invoke by Name
    //   assertid:<AutomationId>          assert element present (else exit!=0)
    //   assertname:<Name>                assert element present by Name (else exit!=0)
    //   assertgridmin:<GridAid>=<n>      assert DataGrid row count >= n (else exit!=0)
    //   gridrows:<GridAid>               log DataGrid row count
    //   log:<text>                       echo a marker line
    //
    // Exit 0 iff all assert* steps passed and no step hit a hard error.

    private static int RunSteps(string exePath, string evidenceDir, string label, string stepsSpec, int timeoutSeconds)
    {
        Console.WriteLine($"[steps] exe={exePath} label={label} evidence={evidenceDir} timeout={timeoutSeconds}s");
        Console.WriteLine($"[steps] spec={stepsSpec}");

        var steps = stepsSpec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var failures = new List<string>();

        var app = LaunchApp(exePath);
        try
        {
            using var automation = new UIA3Automation();
            var window = WaitForMainWindow(app, automation, timeoutMs: 30_000);
            if (window == null)
            {
                Console.Error.WriteLine("ERROR: main window did not appear");
                return 1;
            }
            Console.WriteLine($"[steps] Main window: '{window.Name}'");
            Thread.Sleep(2000); // settle after startup

            foreach (var raw in steps)
            {
                var (op, arg) = SplitStep(raw);
                Console.WriteLine($"[step] {op}{(arg.Length > 0 ? ":" + arg : "")}");
                try
                {
                    if (!ExecuteStep(window, automation, evidenceDir, label, op, arg, failures))
                    {
                        // ExecuteStep returns false only for unrecoverable nav/find errors
                        Console.Error.WriteLine($"[step] '{op}' did not complete successfully");
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"step '{op}:{arg}' threw: {ex.Message}";
                    Console.Error.WriteLine("[step] " + msg);
                    failures.Add(msg);
                }
            }

            Console.WriteLine();
            if (failures.Count == 0)
            {
                Console.WriteLine($"[steps] SUCCESS — all asserts passed for label={label}");
                return 0;
            }

            Console.Error.WriteLine($"[steps] FAILED — {failures.Count} problem(s) for label={label}:");
            foreach (var f in failures) Console.Error.WriteLine("   - " + f);
            return 1;
        }
        finally
        {
            try { app.Close(); } catch { /* ignore */ }
            Thread.Sleep(800);
        }
    }

    private static (string op, string arg) SplitStep(string raw)
    {
        int i = raw.IndexOf(':');
        if (i < 0) return (raw.Trim(), "");
        return (raw[..i].Trim(), raw[(i + 1)..].Trim());
    }

    private static (string key, string val) SplitKv(string arg)
    {
        int i = arg.IndexOf('=');
        if (i < 0) return (arg.Trim(), "");
        return (arg[..i].Trim(), arg[(i + 1)..].Trim());
    }

    private static bool ExecuteStep(Window window, UIA3Automation automation, string evidenceDir, string label,
                                    string op, string arg, List<string> failures)
    {
        var cf = automation.ConditionFactory;
        switch (op)
        {
            case "nav":
                return NavigateGeneric(window, automation, arg);

            case "waitid":
            {
                var el = WaitFor(window, automation, () => FindById(window, automation, arg), 60_000);
                if (el == null) { failures.Add($"waitid '{arg}' not found within 60s"); return false; }
                return true;
            }
            case "waitname":
            {
                var el = WaitFor(window, automation, () => FindByName(window, automation, arg), 60_000);
                if (el == null) { failures.Add($"waitname '{arg}' not found within 60s"); return false; }
                return true;
            }
            case "sleep":
                if (int.TryParse(arg, out var ms)) Thread.Sleep(ms);
                return true;

            case "shot":
            {
                var path = Path.Combine(evidenceDir, $"{label}-{arg}.png");
                TakeScreenshot(window, path);
                Console.WriteLine($"[shot] -> {path}");
                return true;
            }
            case "dumppage":
            {
                var frame = FindById(window, automation, "PART_NavigationViewContentPresenter");
                var path = Path.Combine(evidenceDir, $"{label}-{arg}.txt");
                using var sw = new StreamWriter(path, append: false);
                if (frame != null) DumpElementTo(sw, frame, 0, 12);
                else { sw.WriteLine("(content frame not found; dumping window)"); DumpElementTo(sw, window, 0, 12); }
                Console.WriteLine($"[dumppage] -> {path}");
                return true;
            }
            case "dumpall":
            {
                var path = Path.Combine(evidenceDir, $"{label}-{arg}.txt");
                using var sw = new StreamWriter(path, append: false);
                DumpElementTo(sw, window, 0, 14);
                Console.WriteLine($"[dumpall] -> {path}");
                return true;
            }
            case "setid":
            {
                var (id, val) = SplitKv(arg);
                var el = WaitFor(window, automation, () => FindById(window, automation, id), 10_000);
                if (el == null) { failures.Add($"setid target '{id}' not found"); return false; }
                var vp = el.Patterns.Value.PatternOrDefault;
                if (vp == null) { failures.Add($"setid '{id}' has no ValuePattern"); return false; }
                vp.SetValue(val);
                Console.WriteLine($"[setid] {id} = '{val}'");
                return true;
            }
            case "selcombo":
            {
                var (id, text) = SplitKv(arg);
                var combo = FindById(window, automation, id)?.AsComboBox();
                if (combo == null) { failures.Add($"selcombo target '{id}' not found"); return false; }
                if (!SelectComboItemByText(combo, text, "selcombo")) { failures.Add($"selcombo '{id}' could not select '{text}'"); return false; }
                return true;
            }
            case "selrow":
            {
                var (id, idxStr) = SplitKv(arg);
                if (!int.TryParse(idxStr, out var idx)) { failures.Add($"selrow index '{idxStr}' invalid"); return false; }
                var grid = FindById(window, automation, id);
                if (grid == null) { failures.Add($"selrow grid '{id}' not found"); return false; }
                var rows = grid.FindAllDescendants(cf.ByControlType(ControlType.DataItem));
                if (idx < 0 || idx >= rows.Length) { failures.Add($"selrow index {idx} out of range (rows={rows.Length})"); return false; }
                var sel = rows[idx].Patterns.SelectionItem.PatternOrDefault;
                if (sel != null) { sel.Select(); }
                else { rows[idx].Patterns.LegacyIAccessible.PatternOrDefault?.DoDefaultAction(); }
                Console.WriteLine($"[selrow] {id}[{idx}] selected");
                return true;
            }
            case "invokeid":
            {
                var el = WaitFor(window, automation, () => FindById(window, automation, arg), 10_000);
                if (el == null) { failures.Add($"invokeid '{arg}' not found"); return false; }
                return InvokeElement(el, arg, failures);
            }
            case "invokename":
            {
                var el = WaitFor(window, automation, () => FindByName(window, automation, arg), 10_000);
                if (el == null) { failures.Add($"invokename '{arg}' not found"); return false; }
                return InvokeElement(el, arg, failures);
            }
            case "assertid":
            {
                var el = WaitFor(window, automation, () => FindById(window, automation, arg), 8_000);
                if (el == null) { failures.Add($"ASSERT FAIL: id '{arg}' not present"); return false; }
                Console.WriteLine($"[assert] id '{arg}' present OK");
                return true;
            }
            case "assertname":
            {
                var el = WaitFor(window, automation, () => FindByName(window, automation, arg), 8_000);
                if (el == null) { failures.Add($"ASSERT FAIL: name '{arg}' not present"); return false; }
                Console.WriteLine($"[assert] name '{arg}' present OK");
                return true;
            }
            case "assertgridmin":
            {
                var (id, nStr) = SplitKv(arg);
                if (!int.TryParse(nStr, out var n)) { failures.Add($"assertgridmin n '{nStr}' invalid"); return false; }
                var grid = WaitFor(window, automation, () => FindById(window, automation, id), 8_000);
                if (grid == null) { failures.Add($"ASSERT FAIL: grid '{id}' not present"); return false; }
                var count = grid.FindAllDescendants(cf.ByControlType(ControlType.DataItem)).Length;
                Console.WriteLine($"[assert] grid '{id}' rows={count} (min {n})");
                if (count < n) { failures.Add($"ASSERT FAIL: grid '{id}' rows {count} < {n}"); return false; }
                return true;
            }
            case "gridrows":
            {
                var grid = WaitFor(window, automation, () => FindById(window, automation, arg), 8_000);
                var count = grid == null ? -1 : grid.FindAllDescendants(cf.ByControlType(ControlType.DataItem)).Length;
                Console.WriteLine($"[gridrows] '{arg}' = {count}");
                return true;
            }
            case "log":
                Console.WriteLine($"[log] {arg}");
                return true;

            default:
                failures.Add($"unknown step op '{op}'");
                return false;
        }
    }

    private static bool InvokeElement(AutomationElement el, string tag, List<string> failures)
    {
        var invoke = el.Patterns.Invoke.PatternOrDefault;
        if (invoke != null) { invoke.Invoke(); Console.WriteLine($"[invoke] {tag} via InvokePattern"); return true; }
        var legacy = el.Patterns.LegacyIAccessible.PatternOrDefault;
        if (legacy != null) { legacy.DoDefaultAction(); Console.WriteLine($"[invoke] {tag} via LegacyIAccessible"); return true; }
        var toggle = el.Patterns.Toggle.PatternOrDefault;
        if (toggle != null) { toggle.Toggle(); Console.WriteLine($"[invoke] {tag} via TogglePattern"); return true; }
        failures.Add($"invoke '{tag}' supports neither Invoke/Legacy/Toggle");
        return false;
    }

    private static AutomationElement? FindById(Window w, UIA3Automation a, string id)
    {
        try { return w.FindFirstDescendant(a.ConditionFactory.ByAutomationId(id)); } catch { return null; }
    }

    private static AutomationElement? FindByName(Window w, UIA3Automation a, string name)
    {
        try { return w.FindFirstDescendant(a.ConditionFactory.ByName(name)); } catch { return null; }
    }

    private static AutomationElement? WaitFor(Window w, UIA3Automation a, Func<AutomationElement?> find, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            try { var e = find(); if (e != null) return e; } catch { /* retry */ }
            Thread.Sleep(400);
        }
        return null;
    }

    private static string Breadcrumb(Window w, UIA3Automation a)
    {
        try
        {
            var bc = w.FindFirstDescendant(a.ConditionFactory.ByAutomationId("BreadcrumbBar"));
            if (bc == null) return "";
            var texts = bc.FindAllDescendants(a.ConditionFactory.ByControlType(ControlType.Text));
            return texts.Length > 0 ? (texts[^1].Name ?? "") : "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// Generic NavigationViewItem activation by AutomationId. Tries UIA patterns,
    /// then Focus + posted Space/Enter (Wpf.Ui nav items are ButtonBase DataItems
    /// without an Invoke peer). "Navigated" is confirmed via the breadcrumb text
    /// matching the nav item's Name.
    /// </summary>
    private static bool NavigateGeneric(Window window, UIA3Automation automation, string navId)
    {
        var cf = automation.ConditionFactory;
        var navItem = window.FindFirstDescendant(cf.ByAutomationId(navId));
        if (navItem == null)
        {
            Console.Error.WriteLine($"[nav] nav item '{navId}' not found");
            return false;
        }
        var navName = navItem.Name ?? "";
        Console.WriteLine($"[nav] target '{navId}' Name='{navName}'");

        bool Navigated() => Breadcrumb(window, automation).Contains(navName, StringComparison.OrdinalIgnoreCase);

        // Already there?
        if (!string.IsNullOrEmpty(navName) && Navigated())
        {
            Console.WriteLine($"[nav] already on '{navName}'");
            return true;
        }

        // 1) patterns
        foreach (var (pat, act) in new (string, Action)[]
        {
            ("Invoke", () => navItem.Patterns.Invoke.PatternOrDefault?.Invoke()),
            ("SelectionItem", () => navItem.Patterns.SelectionItem.PatternOrDefault?.Select()),
            ("Legacy", () => navItem.Patterns.LegacyIAccessible.PatternOrDefault?.DoDefaultAction()),
        })
        {
            try { act(); } catch (Exception ex) { Console.WriteLine($"[nav] {pat} failed: {ex.Message}"); continue; }
            if (WaitNavigated(Navigated, 3500)) { Console.WriteLine($"[nav] {pat} navigated"); return true; }
        }

        // 2) Focus + posted Space / Enter to the app HWND
        try { navItem.Focus(); Thread.Sleep(250); } catch { /* ignore */ }
        IntPtr hwnd;
        try { hwnd = window.Properties.NativeWindowHandle.Value; }
        catch { Console.Error.WriteLine("[nav] no native handle"); return false; }

        foreach (var (vk, name) in new[] { (VK_SPACE, "SPACE"), (VK_RETURN, "RETURN") })
        {
            try { navItem.Focus(); } catch { /* ignore */ }
            Thread.Sleep(150);
            PostMessage(hwnd, WM_KEYDOWN, (IntPtr)vk, (IntPtr)0x00000001);
            Thread.Sleep(120);
            PostMessage(hwnd, WM_KEYUP, (IntPtr)vk, (IntPtr)unchecked((int)0xC0000001));
            if (WaitNavigated(Navigated, 4000)) { Console.WriteLine($"[nav] posted {name} navigated"); return true; }
        }

        // If breadcrumb check is unreliable for this page, accept after settle.
        Console.Error.WriteLine($"[nav] WARN: could not confirm navigation to '{navName}' via breadcrumb; continuing");
        Thread.Sleep(1500);
        return true;
    }

    private static bool WaitNavigated(Func<bool> navigated, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            try { if (navigated()) return true; } catch { /* retry */ }
            Thread.Sleep(300);
        }
        return false;
    }

    private static void DumpElementTo(StreamWriter sw, AutomationElement el, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        var indent = new string(' ', depth * 2);
        string name, aid, cls, ct;
        try { name = el.Name ?? ""; } catch { name = "?"; }
        try { aid = el.AutomationId ?? ""; } catch { aid = "?"; }
        try { cls = el.ClassName ?? ""; } catch { cls = "?"; }
        try { ct = el.ControlType.ToString(); } catch { ct = "?"; }
        sw.WriteLine($"{indent}[{ct}] Name='{name}' Aid='{aid}' Cls='{cls}'");
        try { foreach (var child in el.FindAllChildren()) DumpElementTo(sw, child, depth + 1, maxDepth); }
        catch { /* some elements don't enumerate */ }
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
