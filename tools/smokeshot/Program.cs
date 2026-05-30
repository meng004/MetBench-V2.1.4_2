// smokeshot — UIA driver for MetBench_Client screenshot smoke flows.
//
// Usage:
//   smokeshot nav-all [--out DIR]            5-page navigation loop (PR #29 behavior)
//   smokeshot app-add <sutName> [--out DIR]  Step 1: Application Management add SUT
//   smokeshot mr-add  <mrCode>  [--out DIR]  Step 2: MR Management add MR
//   smokeshot mt-exec [--out DIR]            Step 3: MT execution (skips if OpenMOC venv missing)
//   smokeshot metapatterns [--out DIR]       MetaPatterns CRUD: list / page2 / toggle screenshots
//   smokeshot i18n-smoke [--out DIR]         Client i18n: language switch en<->zh + fallback evidence
//   smokeshot debug                          Dump named UIA tree of running app
//
// Exit codes: 0 = ok, 1 = failure, 2 = skipped (env not ready), 3 = app not running.
//
// Requires: MetBench_Client.exe already running. Orchestrator (run_full_smoke.ps1)
// launches it and waits for window before invoking each flow.
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Smokeshot;

internal static class Program
{
    private const string DefaultOutDir = @"C:\Users\limeng\MetBench-V2.1.4_2\docs\screenshots\v2-ship-2026-05-14\";

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var cmd = args[0].ToLowerInvariant();
        var outDir = ParseOutDir(args);
        Directory.CreateDirectory(outDir);

        var procs = Process.GetProcessesByName("MetBench_Client");
        if (procs.Length == 0)
        {
            Console.Error.WriteLine("MetBench_Client.exe not running — launch the WPF app first.");
            return 3;
        }

        var hwnd = procs[0].MainWindowHandle;
        var pid = (uint)procs[0].Id;
        Console.WriteLine($"MetBench PID={pid} HWND={hwnd}");

        // 1. Bring window forward (most flows assume the page is interactive)
        Thread.Sleep(400);
        var app = UiaHelpers.FocusAndAttach(hwnd);
        UiaHelpers.DismissDialogs(pid, hwnd);
        Console.WriteLine($"App UIA: Name={app.Current.Name} Enabled={app.Current.IsEnabled}");

        // 2. Dispatch flow
        try
        {
            return cmd switch
            {
                "nav-all"      => Flows.NavAll(hwnd, app, outDir),
                "app-add"      => Flows.AppAdd(hwnd, app, outDir, RequirePositional(args, 1, "sutName")),
                "mr-add"       => Flows.MrAdd(hwnd, app, outDir, RequirePositional(args, 1, "mrCode")),
                "mt-exec"      => Flows.MtExec(hwnd, app, outDir),
                "metapatterns" => Flows.MetaPatterns(hwnd, app, outDir),
                "i18n-smoke"   => Flows.I18nSmoke(hwnd, app, outDir),
                "debug"        => Flows.Debug(app),
                _              => UnknownCommand(cmd),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unhandled error: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static int UnknownCommand(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}");
        PrintUsage();
        return 1;
    }

    private static string ParseOutDir(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--out") return args[i + 1];
        return DefaultOutDir;
    }

    private static string RequirePositional(string[] args, int index, string label)
    {
        // Args layout: cmd [positional ...] [--out DIR]
        // Skip the cmd; treat anything before "--out" at position `index` as positional.
        int positionalIndex = 0;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--out") { i++; continue; }  // skip flag + its value
            if (positionalIndex == index - 1) return args[i];
            positionalIndex++;
        }
        throw new ArgumentException($"Missing positional argument '{label}' at slot {index}.");
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  smokeshot nav-all [--out DIR]            5-page navigation loop");
        Console.Error.WriteLine("  smokeshot app-add <sutName> [--out DIR]  Step 1: Application Management add SUT");
        Console.Error.WriteLine("  smokeshot mr-add  <mrCode>  [--out DIR]  Step 2: MR Management add MR");
        Console.Error.WriteLine("  smokeshot mt-exec [--out DIR]            Step 3: MT execution (skips if no OpenMOC)");
        Console.Error.WriteLine("  smokeshot metapatterns [--out DIR]       MetaPatterns CRUD screenshots");
        Console.Error.WriteLine("  smokeshot i18n-smoke   [--out DIR]       Client i18n language switch + fallback evidence");
        Console.Error.WriteLine("  smokeshot debug                          Dump named UIA tree");
        Console.Error.WriteLine("Exit codes: 0=ok 1=fail 2=skipped 3=app-not-running");
    }
}
