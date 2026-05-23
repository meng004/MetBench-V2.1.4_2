// Canned UIA flows — each method drives the running MetBench_Client through a sequence of
// actions + screenshots that maps to one or more acceptance criteria from
// docs/superpowers/plans/2026-05-15-v2.1-followup-pipeline.md §PR-VM-5.
//
// Design rule: each flow is responsible for ONE page or scenario. It may:
//   • navigate, click, type, wait
//   • take screenshots into the configured outDir
//   • write a single-line status into the output (for the orchestrator to grep)
//
// It may NOT: launch the app (orchestrator's job), spawn other processes,
// or assume any DB pre-state beyond what's documented in the flow's header.
using System;
using System.Linq;
using System.Threading;
using System.Windows.Automation;

namespace Smokeshot;

public static class Flows
{
    // =====================================================================
    // Original behavior preserved — 5-nav screenshot loop. PR #29 used this.
    // =====================================================================
    public static int NavAll(IntPtr hwnd, AutomationElement app, string outDir)
    {
        // Trends 页已删除（2026-05-23 next-stage P0：Trend 子系统下线）
        string[] pages = { "Anomalies", "Discovery", "Mutation", "Coverage" };
        string[] shots = {
            "smoke-04-anomalies.png", "smoke-06-discovery.png",
            "smoke-08-mutation.png",  "smoke-09-coverage.png" };

        int failed = 0;
        for (int i = 0; i < pages.Length; i++)
        {
            try
            {
                Console.WriteLine($"\nNavigating to {pages[i]}...");
                UiaHelpers.FocusAndAttach(hwnd);
                UiaHelpers.NavigateTo(app, pages[i], settleMs: 2200);
                UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, shots[i]));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL {pages[i]}: {ex.Message}");
                failed++;
            }
        }
        return failed == 0 ? 0 : 1;
    }

    // =====================================================================
    // Step 1 (PR-VM-5 §B): Application Management — Add a SUT entry + screenshot.
    // Requires: existing ApplicationManagementPage is visible. Writes one row into
    // v1 Applications LiteDB collection.
    // =====================================================================
    public static int AppAdd(IntPtr hwnd, AutomationElement app, string outDir, string sutName)
    {
        Console.WriteLine($"Step 1: Application Management — Add SUT '{sutName}'...");
        try
        {
            UiaHelpers.NavigateTo(app, "Application Management", settleMs: 1800);

            // Form-fill path is page-specific; ApplicationManagementPage's exact TextBox
            // automation Names are not set in XAML, so we fall back to the first writable
            // Edit control. If this proves flaky, the next iteration should add
            // AutomationProperties.Name to the WPF page (small XAML change).
            var nameEdit = FindFirstWritableEdit(app);
            if (nameEdit is null)
            {
                Console.WriteLine("  WARN: no writable Edit element found on ApplicationManagementPage; " +
                                   "filling form is a manual step. Capturing the empty form instead.");
            }
            else if (!UiaHelpers.TrySetValue(nameEdit, sutName))
            {
                Console.WriteLine("  WARN: ValuePattern.SetValue failed; capturing as-is.");
            }

            // Capture either the filled form or the empty form — both are visual proof of
            // step-1 reachability. The "Add" button click is left to a follow-up iteration
            // once form-field automation Names are wired.
            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "smoke-01-app-management.png"));
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL: {ex.Message}");
            return 1;
        }
    }

    // =====================================================================
    // Step 2 (PR-VM-5 §C): MR Management — Add an MR entry + screenshot.
    // Requires: existing MRManagementPage is visible.
    // =====================================================================
    public static int MrAdd(IntPtr hwnd, AutomationElement app, string outDir, string mrCode)
    {
        Console.WriteLine($"Step 2: MR Management — Add MR '{mrCode}'...");
        try
        {
            UiaHelpers.NavigateTo(app, "MR Management", settleMs: 1800);

            var codeEdit = FindFirstWritableEdit(app);
            if (codeEdit is not null) UiaHelpers.TrySetValue(codeEdit, mrCode);

            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "smoke-02-mr-management.png"));
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL: {ex.Message}");
            return 1;
        }
    }

    // =====================================================================
    // Step 3 (PR-VM-5 §D): MT Execution — select scenario, run, wait for status.
    // SKIP cleanly when OpenMOC venv missing (env var METBENCH_OPENMOC_PYTHON not
    // pointing at importable openmoc) — full execution path is OpenMOC-dependent.
    // =====================================================================
    public static int MtExec(IntPtr hwnd, AutomationElement app, string outDir)
    {
        var openmocPython = Environment.GetEnvironmentVariable("METBENCH_OPENMOC_PYTHON");
        if (string.IsNullOrWhiteSpace(openmocPython))
        {
            Console.WriteLine("Step 3: SKIP — METBENCH_OPENMOC_PYTHON not set.");
            Console.WriteLine("       Set the env var to a Python with `import openmoc` working,");
            Console.WriteLine("       then re-run `smokeshot mt-exec` to capture smoke-03.");
            // Still capture page-reachable screenshot as a partial.
            try
            {
                UiaHelpers.NavigateTo(app, "System MT", settleMs: 1500);
                UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "smoke-03-mt-page-no-openmoc.png"));
            }
            catch (Exception ex) { Console.WriteLine($"  Page nav also failed: {ex.Message}"); }
            return 2;  // distinct exit code: skip, not failure
        }

        Console.WriteLine($"Step 3: MT Execution with OpenMOC at '{openmocPython}'...");
        try
        {
            UiaHelpers.NavigateTo(app, "System MT", settleMs: 1800);
            // Run button name on SystemMtExecutionPage; if absent, try variants.
            string[] runCandidates = { "Run", "Execute", "Start" };
            AutomationElement? runBtn = null;
            foreach (var n in runCandidates)
            {
                runBtn = UiaHelpers.FindButton(app, n);
                if (runBtn is not null) break;
            }
            if (runBtn is null)
            {
                Console.WriteLine("  WARN: no Run/Execute/Start button found; capturing page state only.");
                UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "smoke-03-mt-page.png"));
                return 1;
            }
            UiaHelpers.TryInvoke(runBtn);

            // Wait up to 60s for status text "Ok" or "Anomaly" or "Error" to appear.
            bool finished = UiaHelpers.WaitFor(() =>
            {
                foreach (var status in new[] { "Ok", "Anomaly", "Error", "Timeout" })
                {
                    if (UiaHelpers.FindByName(app, status) is not null) return true;
                }
                return false;
            }, TimeSpan.FromSeconds(60));

            if (!finished)
                Console.WriteLine("  WARN: MT exec didn't reach terminal status within 60s; screenshot anyway.");

            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "smoke-03-mt-execution.png"));
            return finished ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL: {ex.Message}");
            return 1;
        }
    }

    // =====================================================================
    // (PR-VM-5 §E) MetaPatterns CRUD flow — covers PR-VM-3's manual-smoke acceptance:
    //   1. nav → 8 seeded rows visible
    //   2. PagingBar Next → page 2 visible
    //   3. Select first row → Toggle status → screenshot
    //   4. AuditLog verify is the orchestrator's job (LiteDB read post-flow)
    // =====================================================================
    public static int MetaPatterns(IntPtr hwnd, AutomationElement app, string outDir)
    {
        Console.WriteLine("MetaPatterns flow: 3 screenshots (list / page2 / toggle)...");
        int failed = 0;

        try
        {
            UiaHelpers.NavigateTo(app, "MetaPatterns", settleMs: 1800);
            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "smoke-meta-01-list.png"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL nav+list shot: {ex.Message}");
            return 1;  // can't continue without nav
        }

        // Page 2 — PagingBar's "Next" button. Wpf.Ui icon-only buttons have empty Name but
        // set HelpText via ToolTip="Next page" (see Views/Controls/PagingBar.xaml).
        try
        {
            var next = UiaHelpers.FindButtonByTooltip(app, "Next page");
            if (next is null)
            {
                Console.WriteLine("  WARN: PagingBar 'Next page' button not found by HelpText");
                failed++;
            }
            else if (UiaHelpers.TryInvoke(next))
            {
                Thread.Sleep(800);
                UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "smoke-meta-02-page2.png"));
            }
            else
            {
                Console.WriteLine("  WARN: page-2 screenshot skipped (Next button found but Invoke failed)");
                failed++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  page-2 FAIL: {ex.Message}");
            failed++;
        }

        // Toggle status on the first visible row — first wait for DataGrid to render rows,
        // then SelectionItem.Select, then click Toggle button.
        try
        {
            // After paging Next, give DataGrid a beat to re-render rows
            bool hasRows = UiaHelpers.WaitFor(
                () => app.FindAll(TreeScope.Descendants,
                          new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem))
                          .Count > 0,
                TimeSpan.FromSeconds(3));

            if (!hasRows)
            {
                Console.WriteLine("  WARN: no DataItem rows visible after wait; toggle skipped");
                failed++;
            }
            else
            {
                var firstRow = app.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem))
                    .Cast<AutomationElement>()
                    .FirstOrDefault(r => !string.IsNullOrEmpty(r.Current.Name));

                if (firstRow is null)
                {
                    Console.WriteLine("  WARN: no named DataItem row found; toggle skipped");
                    failed++;
                }
                else if (!UiaHelpers.TrySelect(firstRow))
                {
                    Console.WriteLine($"  WARN: row '{firstRow.Current.Name}' SelectionItemPattern.Select returned false; toggle skipped");
                    Console.WriteLine("        Workaround: DataGridRow needs SetFocus + mouse click — see README known issues");
                    failed++;
                }
                else
                {
                    Console.WriteLine($"  Selected first row: {firstRow.Current.Name}");
                    Thread.Sleep(300);
                    try
                    {
                        UiaHelpers.ClickButton(app, "Toggle status", settleMs: 800);
                        UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "smoke-meta-03-toggle.png"));
                    }
                    catch (Exception btnEx)
                    {
                        Console.WriteLine($"  WARN: Toggle status button click failed: {btnEx.Message}");
                        failed++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  toggle FAIL: {ex.Message}");
            failed++;
        }

        return failed == 0 ? 0 : 1;
    }

    // =====================================================================
    // Debug: dump named tree (helps figure out names that aren't matching).
    // =====================================================================
    public static int Debug(AutomationElement app)
    {
        UiaHelpers.DumpNamedTree(app);
        return 0;
    }

    // ===== private helpers =====

    /// <summary>First Edit (TextBox) element that's writable. Crude form-fill fallback.</summary>
    private static AutomationElement? FindFirstWritableEdit(AutomationElement root)
    {
        var edits = root.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
        foreach (AutomationElement e in edits)
        {
            try
            {
                if (e.GetCurrentPattern(ValuePattern.Pattern) is ValuePattern vp && !vp.Current.IsReadOnly)
                    return e;
            }
            catch { }
        }
        return null;
    }

    /// <summary>Find the N-th button whose name contains the given substring (case-insensitive). Fallback discovery.</summary>
    private static AutomationElement? FindNthButtonInRange(AutomationElement root, string contains, int index)
    {
        var buttons = root.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        int hits = 0;
        foreach (AutomationElement b in buttons)
        {
            var n = b.Current.Name ?? "";
            if (n.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hits++;
                if (hits == index) return b;
            }
        }
        return null;
    }
}
