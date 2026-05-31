// Reusable UIA helpers — extracted from inline Program.cs so flows can compose them.
// Design: tiny, no state, throw on hard error so caller can decide retry/skip.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;

namespace Smokeshot;

public static class UiaHelpers
{
    // ===== Win32 interop =====

    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr i, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr dc, uint f);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a, uint b, bool f);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);

    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP   = 0x0004;
    const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    const uint MOUSEEVENTF_MOVE     = 0x0001;

    // ===== Window lifecycle =====

    /// <summary>Maximize the window (SW_MAXIMIZE = 3). Use to expand compact nav panes before screenshots.</summary>
    public static void MaximizeWindow(IntPtr hwnd) => ShowWindow(hwnd, 3);

    /// <summary>Show + foreground + small settle delay. Returns the AutomationElement for further finds.</summary>
    public static AutomationElement FocusAndAttach(IntPtr hwnd)
    {
        uint tid = GetWindowThreadProcessId(hwnd, out _);
        uint cur = GetCurrentThreadId();
        ShowWindow(hwnd, 9);                                  // SW_RESTORE
        SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, 3);    // HWND_TOPMOST + NOMOVE+NOSIZE
        AttachThreadInput(cur, tid, true);
        SetForegroundWindow(hwnd);
        Thread.Sleep(300);
        AttachThreadInput(cur, tid, false);
        SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, 3);    // HWND_NOTOPMOST
        Thread.Sleep(200);
        return AutomationElement.FromHandle(hwnd);
    }

    /// <summary>Find + dismiss any modal dialog the process owns (so subsequent clicks land on real window).</summary>
    public static void DismissDialogs(uint pid, IntPtr mainHwnd)
    {
        var allWindows = AutomationElement.RootElement.FindAll(TreeScope.Children,
            new PropertyCondition(AutomationElement.ProcessIdProperty, (int)pid));
        foreach (AutomationElement win in allWindows)
        {
            if (win.Current.NativeWindowHandle == (int)mainHwnd) continue;
            Console.WriteLine($"  Dialog: Name={win.Current.Name} HWND={win.Current.NativeWindowHandle}");
            var ok = win.FindFirst(TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, "OK"),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)));
            if (ok != null && TryInvoke(ok))
            {
                Console.WriteLine("  Dismissed dialog via OK");
                Thread.Sleep(500);
            }
        }
    }

    // ===== Element finding =====

    /// <summary>Find by Name + optional ControlType. Returns null if not found.</summary>
    public static AutomationElement? FindByName(AutomationElement root, string name, ControlType? controlType = null)
    {
        Condition cond = controlType is null
            ? new PropertyCondition(AutomationElement.NameProperty, name)
            : new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, name),
                new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
        return root.FindFirst(TreeScope.Descendants, cond);
    }

    /// <summary>Find a navigation item (Wpf.Ui's NavigationViewItem renders as DataItem).</summary>
    public static AutomationElement? FindNavItem(AutomationElement root, string name)
    {
        return FindByName(root, name, ControlType.DataItem) ?? FindByName(root, name);
    }

    /// <summary>Find a button by name (Primary/Secondary/Caution Wpf.Ui buttons all map to Button).</summary>
    public static AutomationElement? FindButton(AutomationElement root, string name)
        => FindByName(root, name, ControlType.Button);

    /// <summary>
    /// Find a button by its tooltip text (UIA HelpText property). Wpf.Ui icon-only buttons
    /// (Wpf.Ui SymbolIcon) typically have empty Name but always set HelpText via ToolTip="...".
    /// </summary>
    public static AutomationElement? FindButtonByTooltip(AutomationElement root, string tooltip)
    {
        var buttons = root.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        foreach (AutomationElement b in buttons)
        {
            try
            {
                var ht = b.GetCurrentPropertyValue(AutomationElement.HelpTextProperty) as string;
                if (!string.IsNullOrEmpty(ht) &&
                    ht.Equals(tooltip, StringComparison.OrdinalIgnoreCase))
                    return b;
            }
            catch { }
        }
        return null;
    }

    /// <summary>Find an edit control (TextBox / Wpf.Ui TextBox).</summary>
    public static AutomationElement? FindEdit(AutomationElement root, string name)
        => FindByName(root, name, ControlType.Edit);

    // ===== Actions =====

    /// <summary>Try InvokePattern.Invoke on an element. Returns true on success.</summary>
    public static bool TryInvoke(AutomationElement el)
    {
        try
        {
            if (el.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern inv)
            {
                inv.Invoke();
                return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>Try SelectionItemPattern.Select (for ComboBox items, ListView items).</summary>
    public static bool TrySelect(AutomationElement el)
    {
        try
        {
            if (el.GetCurrentPattern(SelectionItemPattern.Pattern) is SelectionItemPattern sel)
            {
                sel.Select();
                return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>Set the value of an edit element via ValuePattern. Returns true if successful.</summary>
    public static bool TrySetValue(AutomationElement el, string value)
    {
        try
        {
            if (el.GetCurrentPattern(ValuePattern.Pattern) is ValuePattern vp)
            {
                if (vp.Current.IsReadOnly) return false;
                vp.SetValue(value);
                return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Find nav item by name + activate it. Tries InvokePattern → SelectionItemPattern → mouse_event
    /// (Wpf.Ui's NavigationViewItem renders as DataItem which often exposes only SelectionItem; some
    /// builds expose neither, in which case absolute-coordinate mouse click is the last resort).
    /// Throws if all three strategies fail.
    /// </summary>
    public static void NavigateTo(AutomationElement root, string pageName, int settleMs = 1500)
    {
        var nav = FindNavItem(root, pageName) ?? throw new InvalidOperationException(
            $"Nav item '{pageName}' not found.");

        Console.WriteLine($"  NavItem '{pageName}': CT={nav.Current.ControlType?.ProgrammaticName} Class={nav.Current.ClassName}");

        // Try UIA patterns: SelectionItem first (Wpf.Ui DataItem nav responds to Select),
        // then Invoke, then keyboard focus+Enter, then mouse.
        if (TrySelect(nav))           { Thread.Sleep(settleMs); return; }
        if (TryInvoke(nav))           { Thread.Sleep(settleMs); return; }
        if (TryFocusAndEnter(nav))    { Thread.Sleep(settleMs); return; }
        if (TryClickViaMouse(nav))    { Thread.Sleep(settleMs); return; }

        throw new InvalidOperationException(
            $"Nav item '{pageName}' found but no activation pattern worked (Select/Invoke/FocusEnter/Mouse all failed).");
    }

    /// <summary>Set focus on element and send Enter key via SendKeys (WinForms). Last resort before mouse.</summary>
    public static bool TryFocusAndEnter(AutomationElement el)
    {
        try
        {
            el.SetFocus();
            Thread.Sleep(120);
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Physical-coordinate left-click on element's center. Last-resort when no UIA pattern works.
    /// Hardcodes 4K + DPI scale 2 per the original smokeshot defaults — adjust if running on a
    /// different setup. Pass screenW=0 to auto-detect from SM_CXSCREEN.
    /// </summary>
    public static bool TryClickViaMouse(AutomationElement el, int screenW = 0, int screenH = 0, int dpiScale = 0)
    {
        try
        {
            // Auto-detect screen dimensions if not specified
            if (screenW <= 0) screenW = GetSystemMetrics(0);   // SM_CXSCREEN
            if (screenH <= 0) screenH = GetSystemMetrics(1);   // SM_CYSCREEN
            if (dpiScale <= 0) dpiScale = 1;                   // UIA coords are already in physical pixels at 96 DPI

            try { el.SetFocus(); } catch { }
            Thread.Sleep(150);

            dynamic rawRect = el.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
            double rl = (double)rawRect.Left, rt = (double)rawRect.Top,
                   rw = (double)rawRect.Width, rh = (double)rawRect.Height;
            // UIA BoundingRectangle coordinates are in logical pixels (same as SetCursorPos units)
            int logX = (int)(rl + rw / 2);
            int logY = (int)(rt + rh / 2);
            uint absX = (uint)((long)logX * 65535 / (screenW - 1));
            uint absY = (uint)((long)logY * 65535 / (screenH - 1));

            Console.WriteLine($"  Mouse click: logX={logX} logY={logY} absX={absX} absY={absY} screen={screenW}x{screenH}");

            SetCursorPos(logX, logY);
            Thread.Sleep(120);
            mouse_event(MOUSEEVENTF_MOVE     | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            Thread.Sleep(80);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            Thread.Sleep(80);
            mouse_event(MOUSEEVENTF_LEFTUP   | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  TryClickViaMouse failed: {ex.Message}");
            return false;
        }
    }

    [DllImport("user32.dll")] static extern int GetSystemMetrics(int nIndex);

    /// <summary>Click a button by name. Throws if not found or not invokable.</summary>
    public static void ClickButton(AutomationElement root, string buttonName, int settleMs = 400)
    {
        var btn = FindButton(root, buttonName) ?? throw new InvalidOperationException(
            $"Button '{buttonName}' not found.");
        if (!TryInvoke(btn))
        {
            throw new InvalidOperationException($"Button '{buttonName}' not invokable.");
        }
        Thread.Sleep(settleMs);
    }

    /// <summary>Set TextBox value by Name. Returns true if found + set.</summary>
    public static bool SetEditByName(AutomationElement root, string name, string value)
    {
        var ed = FindEdit(root, name);
        return ed is not null && TrySetValue(ed, value);
    }

    /// <summary>
    /// Open a ComboBox whose current items include the given candidates, then select
    /// <paramref name="itemName"/>. If no ComboBox has the expected items, falls back
    /// to direct mouse click on the Nth ComboBox (by index).
    /// After expand, searches the full desktop subtree since WPF ComboBox popup may be
    /// a separate UIA element outside the app window.
    /// </summary>
    public static bool SelectComboBoxItem(AutomationElement root, string itemName, int settleMs = 600)
    {
        try
        {
            // Find ALL ComboBoxes and pick the one that contains the target item
            var allCombos = root.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox));
            Console.WriteLine($"  Found {allCombos.Count} ComboBox(es) on screen.");

            AutomationElement? combo = null;
            // Find the combo whose children already contain 'English' or '中文' (language options)
            string[] langHints = { "English", "中文" };
            foreach (AutomationElement c in allCombos)
            {
                var children = c.FindAll(TreeScope.Children, Condition.TrueCondition);
                bool hasLangItem = false;
                foreach (AutomationElement child in children)
                {
                    string cn = child.Current.Name ?? "";
                    if (langHints.Any(h => cn.Equals(h, StringComparison.OrdinalIgnoreCase)))
                    {
                        hasLangItem = true;
                        break;
                    }
                }
                if (hasLangItem) { combo = c; break; }
            }

            if (combo is null && allCombos.Count > 0)
            {
                Console.WriteLine("  No language ComboBox found by children scan; trying last ComboBox by index.");
                // Settings page language ComboBox is typically the last one added
                combo = allCombos.Cast<AutomationElement>().LastOrDefault();
            }

            if (combo is null)
            {
                Console.WriteLine("  SelectComboBoxItem: no ComboBox found.");
                return false;
            }

            Console.WriteLine($"  Found ComboBox: Name='{combo.Current.Name}' Class='{combo.Current.ClassName}' Children={combo.FindAll(TreeScope.Children, Condition.TrueCondition).Count}");

            // Expand it
            if (combo.GetCurrentPattern(ExpandCollapsePattern.Pattern) is ExpandCollapsePattern ecp)
            {
                ecp.Expand();
                Thread.Sleep(600);
                Console.WriteLine("  ComboBox expanded via ExpandCollapsePattern.");
            }
            else
            {
                TryClickViaMouse(combo);
                Thread.Sleep(600);
                Console.WriteLine("  ComboBox expanded via mouse click.");
            }

            // After expand: WPF ComboBox popup may be a separate UIA subtree on the desktop.
            // Try in order: (1) combo subtree, (2) full root subtree, (3) desktop root.
            AutomationElement? item = null;
            string[] searchScopes = { "combo", "root", "desktop" };
            AutomationElement?[] searchRoots = { combo, root, AutomationElement.RootElement };

            for (int i = 0; i < searchScopes.Length && item is null; i++)
            {
                Console.WriteLine($"  Searching '{itemName}' in {searchScopes[i]}...");
                try
                {
                    // First try exact name
                    item = searchRoots[i]!.FindFirst(TreeScope.Subtree,
                        new PropertyCondition(AutomationElement.NameProperty, itemName));
                    if (item is null)
                    {
                        // Also look for ListItem/DataItem with this name
                        var candidates = searchRoots[i]!.FindAll(TreeScope.Descendants,
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
                        foreach (AutomationElement c in candidates)
                        {
                            string n = c.Current.Name ?? "";
                            Console.WriteLine($"    ListItem found: Name='{n}'");
                            if (n.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                                n.Contains(itemName, StringComparison.OrdinalIgnoreCase))
                            {
                                item = c;
                                break;
                            }
                        }
                    }
                }
                catch (Exception searchEx)
                {
                    Console.WriteLine($"    Search {searchScopes[i]} failed: {searchEx.Message}");
                }
            }

            if (item is null)
            {
                Console.WriteLine($"  SelectComboBoxItem: item '{itemName}' not found after expand (tried combo+root+desktop).");
                // Try to use SelectionItem pattern directly on combo children
                try
                {
                    var children = combo.FindAll(TreeScope.Children, Condition.TrueCondition);
                    Console.WriteLine($"  Combo direct children: {children.Count}");
                    foreach (AutomationElement c in children)
                        Console.WriteLine($"    Child: Name='{c.Current.Name}' CT={c.Current.ControlType?.ProgrammaticName} Class={c.Current.ClassName}");
                }
                catch { }

                // Collapse and fail
                try { if (combo.GetCurrentPattern(ExpandCollapsePattern.Pattern) is ExpandCollapsePattern ec2) ec2.Collapse(); } catch { }
                return false;
            }

            Console.WriteLine($"  Found item: Name='{item.Current.Name}' CT={item.Current.ControlType?.ProgrammaticName}");

            if (TrySelect(item))
            {
                Thread.Sleep(settleMs);
                return true;
            }
            if (TryInvoke(item))
            {
                Thread.Sleep(settleMs);
                return true;
            }
            if (TryClickViaMouse(item))
            {
                Thread.Sleep(settleMs);
                return true;
            }

            Console.WriteLine($"  SelectComboBoxItem: item '{itemName}' found but no pattern worked.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  SelectComboBoxItem exception: {ex.Message}");
            return false;
        }
    }

    // ===== Wait =====

    /// <summary>Poll until predicate returns true or timeout. Returns true if matched, false if timed out.</summary>
    public static bool WaitFor(Func<bool> predicate, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(250);
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try { if (predicate()) return true; } catch { }
            Thread.Sleep(interval);
        }
        return false;
    }

    /// <summary>Wait until an element with the given Name appears under root.</summary>
    public static bool WaitForName(AutomationElement root, string name, TimeSpan timeout)
        => WaitFor(() => FindByName(root, name) is not null, timeout);

    // ===== Screenshot =====

    /// <summary>PrintWindow → Bitmap. Use after content is settled.</summary>
    public static Bitmap Capture(IntPtr hwnd)
    {
        GetClientRect(hwnd, out RECT r);
        var bmp = new Bitmap(Math.Max(r.R - r.L, 1), Math.Max(r.B - r.T, 1));
        using var g = Graphics.FromImage(bmp);
        PrintWindow(hwnd, g.GetHdc(), 2);                     // PW_RENDERFULLCONTENT
        g.ReleaseHdc();
        return bmp;
    }

    /// <summary>Capture + save PNG. Creates parent directory.</summary>
    public static void SaveScreenshot(IntPtr hwnd, string outPath)
    {
        var dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var bmp = Capture(hwnd);
        bmp.Save(outPath, ImageFormat.Png);
        Console.WriteLine($"  Saved: {outPath} ({bmp.Width}x{bmp.Height})");
    }

    // ===== Debug =====

    /// <summary>List all named descendants with their Name/CT/Class — useful when an automation lookup fails.</summary>
    public static void DumpNamedTree(AutomationElement root, int maxNameLen = 50)
    {
        var all = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        Console.WriteLine($"Named descendants: {all.Count}");
        foreach (AutomationElement e in all)
        {
            string n = e.Current.Name ?? "";
            if (n.Length > 0 && n.Length < maxNameLen)
                Console.WriteLine($"  Name={n,-32} CT={e.Current.ControlType?.ProgrammaticName,-32} Class={e.Current.ClassName}");
        }
    }
}
