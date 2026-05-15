// Smoke test navigator: navigate MetBench to each page and take PrintWindow screenshots
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;

class SmokeNav
{
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
    [DllImport("user32.dll", SetLastError = true)] static extern bool IsWindowEnabled(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP   = 0x0004;
    const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    const uint MOUSEEVENTF_MOVE     = 0x0001;

    static Bitmap Capture(IntPtr hwnd)
    {
        GetClientRect(hwnd, out RECT r);
        var bmp = new Bitmap(r.R - r.L, r.B - r.T);
        using var g = Graphics.FromImage(bmp);
        PrintWindow(hwnd, g.GetHdc(), 2);
        g.ReleaseHdc();
        return bmp;
    }

    static void FocusWindow(IntPtr hwnd)
    {
        uint tid = GetWindowThreadProcessId(hwnd, out _);
        uint cur = GetCurrentThreadId();
        ShowWindow(hwnd, 9);
        SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, 3);
        AttachThreadInput(cur, tid, true);
        SetForegroundWindow(hwnd);
        Thread.Sleep(300);
        AttachThreadInput(cur, tid, false);
        SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, 3);
    }

    // Dismiss any modal dialog belonging to this process
    static void DismissDialogs(uint pid, IntPtr mainHwnd)
    {
        var root = AutomationElement.RootElement;
        var allWindows = root.FindAll(TreeScope.Children,
            new PropertyCondition(AutomationElement.ProcessIdProperty, (int)pid));
        foreach (AutomationElement win in allWindows)
        {
            if (win.Current.NativeWindowHandle == (int)mainHwnd) continue; // skip main window
            Console.WriteLine($"  Dialog: Name={win.Current.Name} HWND={win.Current.NativeWindowHandle}");
            // Try invoking the OK button
            var ok = win.FindFirst(TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, "OK"),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)));
            if (ok != null)
            {
                try
                {
                    ((InvokePattern)ok.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
                    Console.WriteLine("  Dismissed dialog via OK");
                    Thread.Sleep(500);
                }
                catch (Exception ex) { Console.WriteLine($"  Failed to dismiss: {ex.Message}"); }
            }
        }
    }

    static void ClickAndShot(IntPtr hwnd, AutomationElement appEl, string navName, string shotPath, int screenW = 3840, int screenH = 2160, int dpiScale = 2)
    {
        bool enabled = IsWindowEnabled(hwnd);
        Console.WriteLine($"  Main window enabled: {enabled}");

        var el = appEl.FindFirst(TreeScope.Descendants,
            new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, navName),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem)));

        if (el == null)
        {
            el = appEl.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, navName));
        }

        if (el == null) { Console.WriteLine($"  NOT FOUND: {navName}"); return; }

        string ct = el.Current.ControlType?.ProgrammaticName ?? "?";
        Console.WriteLine($"  Found {navName}: CT={ct} Class={el.Current.ClassName}");

        dynamic rawRect = el.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
        double rl = (double)rawRect.Left, rt = (double)rawRect.Top,
               rw = (double)rawRect.Width, rh = (double)rawRect.Height;
        int physX = (int)(rl + rw / 2);
        int physY = (int)(rt + rh / 2);
        Console.WriteLine($"  {navName}: phys=({physX},{physY}) sz=({(int)rw}x{(int)rh})");

        // Try InvokePattern
        bool invoked = false;
        try
        {
            if (el.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern inv)
            {
                inv.Invoke();
                invoked = true;
                Console.WriteLine($"  {navName}: invoked via InvokePattern");
            }
        }
        catch { }

        if (!invoked)
        {
            // Try focus + keyboard: Tab to nav then arrow down
            // Actually: use SetFocus on the element then send Enter
            try
            {
                el.SetFocus();
                Thread.Sleep(200);
                // Send Enter key via SendKeys would require WinForms... skip
            }
            catch { }

            int logX = physX / dpiScale, logY = physY / dpiScale;
            uint absX = (uint)((long)physX * 65535 / (screenW - 1));
            uint absY = (uint)((long)physY * 65535 / (screenH - 1));
            SetCursorPos(logX, logY);
            Thread.Sleep(150);
            mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, absX, absY, 0, UIntPtr.Zero);
            Console.WriteLine($"  {navName}: clicked via mouse_event phys=({physX},{physY})");
        }

        Thread.Sleep(2200);
        using var bmp = Capture(hwnd);
        bmp.Save(shotPath, ImageFormat.Png);
        Console.WriteLine($"  Saved: {shotPath} ({bmp.Width}x{bmp.Height})");
    }

    static void Main(string[] args)
    {
        var procs = Process.GetProcessesByName("MetBench_Client");
        if (procs.Length == 0) { Console.WriteLine("MetBench not running"); return; }
        var hwnd = procs[0].MainWindowHandle;
        var pid = (uint)procs[0].Id;
        Console.WriteLine($"MetBench PID={pid} HWND={hwnd}");

        ShowWindow(hwnd, 3);
        Thread.Sleep(800);
        FocusWindow(hwnd);
        Thread.Sleep(400);

        // Dismiss any modal dialogs
        Console.WriteLine("Checking for modal dialogs...");
        DismissDialogs(pid, hwnd);

        var appEl = AutomationElement.FromHandle(hwnd);
        Console.WriteLine($"App UIA: Name={appEl.Current.Name} Enabled={appEl.Current.IsEnabled}");
        Console.WriteLine($"Main window enabled (Win32): {IsWindowEnabled(hwnd)}");

        if (args.Length > 0 && args[0] == "--debug")
        {
            var all = appEl.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            Console.WriteLine($"Descendants: {all.Count}");
            foreach (AutomationElement e in all)
            {
                string n = e.Current.Name ?? "";
                if (n.Length > 0 && n.Length < 40)
                    Console.WriteLine($"  Name={n,-32} CT={e.Current.ControlType?.ProgrammaticName,-28} Enabled={e.Current.IsEnabled}");
            }
            return;
        }

        var baseDir = @"C:\Users\limeng\MetBench-V2.1.4_2\docs\screenshots\v2-ship-2026-05-14\";
        string[] pages = { "Anomalies", "Discovery", "Mutation", "Coverage", "Trends" };
        string[] shots = { "smoke-04-anomalies.png", "smoke-06-discovery.png", "smoke-08-mutation.png", "smoke-09-coverage.png", "smoke-10-trends.png" };

        for (int i = 0; i < pages.Length; i++)
        {
            Console.WriteLine($"\nNavigating to {pages[i]}...");
            FocusWindow(hwnd);
            ClickAndShot(hwnd, appEl, pages[i], baseDir + shots[i]);
        }
        Console.WriteLine("\nDone.");
    }
}
