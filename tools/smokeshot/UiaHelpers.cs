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

        if (TryInvoke(nav))          { Thread.Sleep(settleMs); return; }
        if (TrySelect(nav))          { Thread.Sleep(settleMs); return; }
        if (TryClickViaMouse(nav))   { Thread.Sleep(settleMs); return; }

        throw new InvalidOperationException(
            $"Nav item '{pageName}' found but no activation pattern worked (Invoke/Select/Mouse all failed).");
    }

    /// <summary>
    /// Physical-coordinate left-click on element's center. Last-resort when no UIA pattern works.
    /// Hardcodes 4K + DPI scale 2 per the original smokeshot defaults — adjust if running on a
    /// different setup.
    /// </summary>
    private static bool TryClickViaMouse(AutomationElement el, int screenW = 3840, int screenH = 2160, int dpiScale = 2)
    {
        try
        {
            try { el.SetFocus(); } catch { }
            Thread.Sleep(150);

            dynamic rawRect = el.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
            double rl = (double)rawRect.Left, rt = (double)rawRect.Top,
                   rw = (double)rawRect.Width, rh = (double)rawRect.Height;
            int physX = (int)(rl + rw / 2);
            int physY = (int)(rt + rh / 2);
            int logX  = physX / dpiScale, logY = physY / dpiScale;
            uint absX = (uint)((long)physX * 65535 / (screenW - 1));
            uint absY = (uint)((long)physY * 65535 / (screenH - 1));

            SetCursorPos(logX, logY);
            Thread.Sleep(120);
            mouse_event(MOUSEEVENTF_MOVE     | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            Thread.Sleep(80);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            Thread.Sleep(80);
            mouse_event(MOUSEEVENTF_LEFTUP   | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            return true;
        }
        catch
        {
            return false;
        }
    }

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
