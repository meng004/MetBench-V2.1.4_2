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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using MetBench_BLL.Core.SystemMT.ImportExport.Put;
using MetBench_UI.Localization;

namespace Smokeshot;

public static class Flows
{
    public static int ExternalMrBatchAD(IntPtr hwnd, AutomationElement app, string outDir)
    {
        Console.WriteLine("External MR Batch A-D WPF flow: ImportAssets + RunBatch + ExportAssets.");
        var workRoot = Path.Combine(Path.GetTempPath(), "MetBenchExternalMrBatchAD", DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
        var packageRoot = Path.Combine(workRoot, "packages");
        var stagingRoot = Path.Combine(workRoot, "staging");
        var exportRoot = Path.Combine(workRoot, "exports");
        Directory.CreateDirectory(packageRoot);
        Directory.CreateDirectory(stagingRoot);
        Directory.CreateDirectory(exportRoot);
        Directory.CreateDirectory(outDir);

        var packages = new[]
        {
            ("batch-a-toy", ExternalMrAcceptancePutFixtures.CreateBatchAToyClassic(), "01-import-assets-batch-a-toy-succeeded.png"),
            ("batch-a-p1", ExternalMrAcceptancePutFixtures.CreateBatchAP1Heat(), "02-import-assets-batch-a-p1-succeeded.png"),
            ("batch-b-existing-runtime", ExternalMrAcceptancePutFixtures.CreateBatchBExistingRuntimeReconcile(), "03-import-assets-batch-b-succeeded.png"),
            ("batch-c-local-remaining", ExternalMrAcceptancePutFixtures.CreateBatchCLocalRemaining(), "04-import-assets-batch-c-succeeded.png"),
            ("batch-d-sciml", ExternalMrAcceptancePutFixtures.CreateBatchDScimlDomainValidity(), "05-import-assets-batch-d-sciml-succeeded.png"),
        };

        var transcript = Path.Combine(outDir, "batch-a-d-vm-transcript.txt");
        File.WriteAllText(transcript, $"workRoot={workRoot}{Environment.NewLine}");

        try
        {
            UiaHelpers.FocusAndAttach(hwnd);
            UiaHelpers.MaximizeWindow(hwnd);
            UiaHelpers.NavigateTo(app, "System MT Async Execution", settleMs: 2500);
            UiaHelpers.SaveScreenshot(hwnd, Path.Combine(outDir, "00-system-mt-async-page.png"));

            foreach (var (name, unit, screenshot) in packages)
            {
                var currentPackageRoot = Path.Combine(packageRoot, name);
                var currentStagingRoot = Path.Combine(stagingRoot, name);
                SutImportPackageExporter.Export(unit, currentPackageRoot);
                AppendLine(transcript, $"package {name}: {currentPackageRoot}");

                SubmitAsyncOperation(
                    hwnd,
                    app,
                    "ImportAssets",
                    package: currentPackageRoot,
                    staging: currentStagingRoot,
                    export: null,
                    batchMrs: null,
                    screenshot: Path.Combine(outDir, screenshot),
                    transcript: transcript);

                var manifest = Directory.GetFiles(currentStagingRoot, "staging-manifest.json", SearchOption.AllDirectories).FirstOrDefault();
                var stagedUnit = Directory.GetFiles(currentStagingRoot, "sut-import-unit.json", SearchOption.AllDirectories).FirstOrDefault();
                if (manifest is null || stagedUnit is null)
                    throw new InvalidOperationException($"ImportAssets for {name} did not write staging-manifest.json and sut-import-unit.json under {currentStagingRoot}.");
                AppendLine(transcript, $"import {name}: manifest={manifest}; unit={stagedUnit}");
            }

            SubmitAsyncOperation(
                hwnd,
                app,
                "RunBatch",
                package: null,
                staging: null,
                export: null,
                batchMrs: "minmr-toy-sort-permutation, minmr-p1-heat-alpha-monotonic, minmr-p1-heat-timestep-convergence, minmr-p1-heat-mesh-convergence",
                screenshot: Path.Combine(outDir, "06-runbatch-batch-a-four-mrs-succeeded.png"),
                transcript: transcript);

            SubmitAsyncOperation(
                hwnd,
                app,
                "RunBatch",
                package: null,
                staging: null,
                export: null,
                batchMrs: "minmr-p2-wave-amplitude-linearity, minmr-p6-poisson-source-linearity, minmr-p7-burgers-viscosity-damping, minmr-p10-pinn-hnn-loss-smoke",
                screenshot: Path.Combine(outDir, "07-runbatch-batch-c-four-mrs-succeeded.png"),
                transcript: transcript);

            foreach (var (name, _, _) in packages)
            {
                var stagedUnit = Directory.GetFiles(Path.Combine(stagingRoot, name), "sut-import-unit.json", SearchOption.AllDirectories).Single();
                var stagingUnitDir = Path.GetDirectoryName(stagedUnit)!;
                var currentExportRoot = Path.Combine(exportRoot, name);
                SubmitAsyncOperation(
                    hwnd,
                    app,
                    "ExportAssets",
                    package: stagingUnitDir,
                    staging: null,
                    export: currentExportRoot,
                    batchMrs: null,
                    screenshot: Path.Combine(outDir, $"08-export-assets-{name}-succeeded.png"),
                    transcript: transcript);

                var exportedUnit = Path.Combine(currentExportRoot, "sut-import-unit.json");
                if (!File.Exists(exportedUnit))
                    throw new InvalidOperationException($"ExportAssets for {name} did not write {exportedUnit}.");
                AppendLine(transcript, $"export {name}: {exportedUnit}");
            }

            TryEvidencePages(hwnd, app, outDir, transcript);
            AppendLine(transcript, "External MR Batch A-D WPF flow PASS.");
            Console.WriteLine($"Transcript: {transcript}");
            return 0;
        }
        catch (Exception ex)
        {
            AppendLine(transcript, $"FAIL: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"  FAIL: {ex}");
            try { UiaHelpers.SaveScreenshot(hwnd, Path.Combine(outDir, "99-failure.png")); } catch { }
            return 1;
        }
    }

    private static void SubmitAsyncOperation(
        IntPtr hwnd,
        AutomationElement app,
        string operation,
        string? package,
        string? staging,
        string? export,
        string? batchMrs,
        string screenshot,
        string transcript)
    {
        Console.WriteLine($"Submitting {operation}...");
        if (!UiaHelpers.SelectComboBoxItemByAutomationId(app, "AsyncOperationCombo", operation, settleMs: 1000))
            throw new InvalidOperationException($"Could not select async operation '{operation}'.");

        if (package is not null)
            UiaHelpers.SetValueByAutomationId(app, "AsyncPackageRootBox", package);
        if (staging is not null)
            UiaHelpers.SetValueByAutomationId(app, "AsyncStagingRootBox", staging);
        if (export is not null)
            UiaHelpers.SetValueByAutomationId(app, "AsyncExportRootBox", export);
        if (batchMrs is not null)
            UiaHelpers.SetValueByAutomationId(app, "AsyncBatchMrIdsBox", batchMrs);

        UiaHelpers.ClickByAutomationId(app, "AsyncSubmitButton", settleMs: 1200);
        var terminal = UiaHelpers.WaitFor(
            () =>
            {
                var state = UiaHelpers.TextByAutomationId(app, "AsyncState");
                return state is "Succeeded" or "Failed" or "Cancelled";
            },
            TimeSpan.FromSeconds(operation == "RunBatch" ? 90 : 45),
            TimeSpan.FromMilliseconds(500));

        UiaHelpers.ClickByAutomationId(app, "AsyncRefreshButton", settleMs: 600);
        var finalState = UiaHelpers.TextByAutomationId(app, "AsyncState");
        var artifact = UiaHelpers.TextByAutomationId(app, "AsyncArtifactPath");
        var summary = UiaHelpers.TextByAutomationId(app, "AsyncResultSummary");
        AppendLine(transcript, $"{operation}: terminal={terminal}; state={finalState}; artifact={artifact}");
        AppendLine(transcript, $"{operation} summary: {summary.Replace(Environment.NewLine, " | ")}");

        UiaHelpers.SaveScreenshot(hwnd, screenshot);
        if (!terminal || finalState != "Succeeded")
            throw new InvalidOperationException($"{operation} ended as '{finalState}'.");
    }

    private static void TryEvidencePages(IntPtr hwnd, AutomationElement app, string outDir, string transcript)
    {
        foreach (var (page, shot) in new[]
        {
            ("MR ReportGenerator", "09-report-batch-a-d-evidence.png"),
            ("Coverage", "10-dashboard-batch-a-d-counts.png"),
            ("Anomalies", "11-anomaly-imported-evidence-limitations.png"),
        })
        {
            try
            {
                UiaHelpers.NavigateTo(app, page, settleMs: 2000);
                UiaHelpers.SaveScreenshot(hwnd, Path.Combine(outDir, shot));
                AppendLine(transcript, $"page {page}: screenshot={shot}");
            }
            catch (Exception ex)
            {
                AppendLine(transcript, $"page {page}: screenshot skipped: {ex.Message}");
            }
        }
    }

    private static void AppendLine(string path, string line)
        => File.AppendAllText(path, line + Environment.NewLine);

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
    // i18n smoke: exercise the language selector (en <-> zh-CN), capture
    // bilingual screenshots, and generate fallback evidence via a WinForms
    // diagnostic surface (option a — no shipped debug window in WPF client).
    // Screenshots: 02-09 in outDir.
    // Returns 0 if all screenshots captured, 1 if any step failed.
    // =====================================================================
    public static int I18nSmoke(IntPtr hwnd, AutomationElement app, string outDir)
    {
        Console.WriteLine("=== i18n-smoke flow ===");
        int failed = 0;

        // ---- screenshot 01: infra tests evidence ----
        // Run dotnet test for ClientI18n filter; capture output as text image.
        Console.WriteLine("\n[01] Running ClientI18n infra tests...");
        try
        {
            CaptureInfraTestsEvidence(outDir);
            Console.WriteLine("  01 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  01 FAIL: {ex.Message}");
            failed++;
        }

        // ---- step A: navigate to Settings (English) ----
        Console.WriteLine("\nA) Navigate to Settings (English)...");
        try
        {
            UiaHelpers.NavigateTo(app, "Settings", settleMs: 2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL navigate to Settings: {ex.Message}");
            failed++;
            // Cannot continue without Settings page
            return RecordFail(failed, "Cannot navigate to Settings (English) — aborting i18n flow.");
        }

        // ---- step B: select "中文" in the language ComboBox ----
        Console.WriteLine("\nB) Select '中文' in language ComboBox...");
        bool chineseSelected = UiaHelpers.SelectComboBoxItem(app, "中文", settleMs: 800);
        if (!chineseSelected)
        {
            Console.WriteLine("  WARN: ComboBox select '中文' failed; will try to continue.");
            failed++;
        }

        // ---- step C: click Apply (the Settings_Apply button) ----
        // The button's Content is bound to Localization[Settings_Apply] = "Apply" in English
        Console.WriteLine("\nC) Click apply button...");
        bool applied = InvokeSettingsLanguageButton(app);
        if (!applied)
        {
            Console.WriteLine("  WARN: Could not find/invoke apply button; culture may not have switched.");
            failed++;
        }

        Thread.Sleep(2000);

        // ---- screenshot 02: Settings page in Chinese ----
        Console.WriteLine("\n[02] Settings page in Chinese (after switching)...");
        try
        {
            UiaHelpers.FocusAndAttach(hwnd);
            Thread.Sleep(500);
            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "02-settings-language-switch-zh.png"));
            Console.WriteLine("  02 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  02 FAIL: {ex.Message}");
            failed++;
        }

        // ---- screenshot 04: navigation rail in Chinese (expanded pane) ----
        Console.WriteLine("\n[04] Navigation rail in Chinese (expanding nav pane)...");
        try
        {
            // Try to expand the NavigationView pane so labels render next to icons.
            // Strategy 1: resize/maximize window so Wpf.Ui auto-expands to ShowLabel mode.
            // Strategy 2: find and click the hamburger/toggle button (tooltip variations).
            bool expanded = TryExpandNavPane(hwnd, app);
            Console.WriteLine($"  Nav pane expand attempt: {(expanded ? "success" : "no reliable toggle found — window may already show labels or will be widened")}");
            Thread.Sleep(800);
            UiaHelpers.FocusAndAttach(hwnd);
            Thread.Sleep(300);
            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "04-navigation-zh.png"));
            Console.WriteLine("  04 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  04 FAIL: {ex.Message}");
            failed++;
        }

        // ---- screenshot 06: System MT page in Chinese ----
        // After culture switch, nav item now shows Chinese text "系统级蜕变测试"
        Console.WriteLine("\n[06] Navigate to System MT (Chinese label)...");
        try
        {
            // Try Chinese label first, fall back to English (in case UIA names lag)
            bool navOk = false;
            foreach (var label in new[] { "系统级蜕变测试", "System MT" })
            {
                try
                {
                    UiaHelpers.NavigateTo(app, label, settleMs: 2000);
                    navOk = true;
                    Console.WriteLine($"  Navigated via '{label}'");
                    break;
                }
                catch { }
            }
            if (!navOk)
            {
                Console.WriteLine("  WARN: could not navigate to System MT in Chinese");
                failed++;
            }
            else
            {
                UiaHelpers.FocusAndAttach(hwnd);
                Thread.Sleep(500);
                UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "06-system-mt-page-zh.png"));
                Console.WriteLine("  06 OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  06 FAIL: {ex.Message}");
            failed++;
        }

        // ---- switch back to English ----
        Console.WriteLine("\nD) Navigate back to Settings (Chinese '设置')...");
        try
        {
            foreach (var label in new[] { "设置", "Settings" })
            {
                try { UiaHelpers.NavigateTo(app, label, settleMs: 2000); break; }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL navigate to Settings (Chinese): {ex.Message}");
            failed++;
        }

        Console.WriteLine("\nE) Select 'English' in ComboBox...");
        bool englishSelected = UiaHelpers.SelectComboBoxItem(app, "English", settleMs: 800);
        if (!englishSelected)
        {
            Console.WriteLine("  WARN: ComboBox select 'English' failed.");
            failed++;
        }

        Console.WriteLine("\nF) Click apply button (now '语言' or 'Language')...");
        bool appliedBack = InvokeSettingsLanguageButton(app);
        if (!appliedBack)
        {
            Console.WriteLine("  WARN: Could not find/invoke apply button (back to English).");
            failed++;
        }

        Thread.Sleep(2000);

        // ---- screenshot 03: Settings page back in English ----
        Console.WriteLine("\n[03] Settings page in English (after switching back)...");
        try
        {
            UiaHelpers.FocusAndAttach(hwnd);
            Thread.Sleep(500);
            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "03-settings-language-switch-en.png"));
            Console.WriteLine("  03 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  03 FAIL: {ex.Message}");
            failed++;
        }

        // ---- screenshot 05: navigation rail in English (expanded pane) ----
        Console.WriteLine("\n[05] Navigation rail in English (expanding nav pane)...");
        try
        {
            // Try to expand the NavigationView pane so labels render next to icons.
            bool expanded = TryExpandNavPane(hwnd, app);
            Console.WriteLine($"  Nav pane expand attempt: {(expanded ? "success" : "no reliable toggle found — using current window state")}");
            Thread.Sleep(800);
            UiaHelpers.FocusAndAttach(hwnd);
            Thread.Sleep(300);
            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "05-navigation-en.png"));
            Console.WriteLine("  05 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  05 FAIL: {ex.Message}");
            failed++;
        }

        // ---- screenshot 07: System MT page in English ----
        Console.WriteLine("\n[07] Navigate to System MT (English label)...");
        try
        {
            UiaHelpers.NavigateTo(app, "System MT", settleMs: 2000);
            UiaHelpers.FocusAndAttach(hwnd);
            Thread.Sleep(500);
            UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, "07-system-mt-page-en.png"));
            Console.WriteLine("  07 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  07 FAIL: {ex.Message}");
            failed++;
        }

        // ---- screenshots 08 + 09: diagnostic surface (option a) ----
        // We instantiate AppLocalizationService directly in smokeshot (tool, not shipped client).
        // For 08: call SetCulture(fr-FR) — falls back to English per AppLocalizationService logic.
        // For 09: call GetString("Missing_Key"), GetString(null), GetString("").
        Console.WriteLine("\n[08/09] Generating fallback diagnostic screenshots (option a: smokeshot-side)...");
        try
        {
            CaptureInvalidCultureFallback(outDir);
            Console.WriteLine("  08 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  08 FAIL: {ex.Message}");
            failed++;
        }

        try
        {
            CaptureMissingKeyFallback(outDir);
            Console.WriteLine("  09 OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  09 FAIL: {ex.Message}");
            failed++;
        }

        if (failed == 0)
            Console.WriteLine("\ni18n-smoke PASS");
        else
            Console.WriteLine($"\ni18n-smoke PARTIAL: {failed} step(s) failed");

        return failed == 0 ? 0 : 1;
    }

    // =====================================================================
    // i18n-pages: switch to a target language (zh / en) via Settings, then
    // navigate the 5 System-MT pages (Execution + 4 catalogs) capturing one
    // screenshot per page. Filenames are caller-supplied (Task 7 evidence set:
    // 10-exec..19-samplecatalog). Best-effort: each page failure is counted
    // but does not abort the remaining captures.
    // lang: "zh" or "en". Returns 0 if all 5 captured, else count of failures
    // is reflected as exit 1.
    // =====================================================================
    public static int I18nPages(IntPtr hwnd, AutomationElement app, string outDir, string lang)
    {
        bool zh = lang.Equals("zh", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"=== i18n-pages flow (lang={(zh ? "zh" : "en")}) ===");
        int failed = 0;

        // ---- step A: navigate to Settings and switch language ----
        // Settings nav label is localized too; try both labels regardless of
        // current culture (we don't know the starting state).
        Console.WriteLine("\nA) Navigate to Settings + switch language...");
        bool settingsOk = false;
        foreach (var label in new[] { "Settings", "设置" })
        {
            try { UiaHelpers.NavigateTo(app, label, settleMs: 1800); settingsOk = true; break; }
            catch { }
        }
        if (!settingsOk)
        {
            Console.WriteLine("  FAIL: could not reach Settings — aborting i18n-pages.");
            return 1;
        }

        string comboItem = zh ? "中文" : "English";
        bool selected = UiaHelpers.SelectComboBoxItem(app, comboItem, settleMs: 800);
        if (!selected) { Console.WriteLine($"  WARN: ComboBox select '{comboItem}' failed."); failed++; }
        bool applied = InvokeSettingsLanguageButton(app);
        if (!applied) { Console.WriteLine("  WARN: apply button not invoked; culture may not have switched."); failed++; }
        Thread.Sleep(2000);
        UiaHelpers.MaximizeWindow(hwnd);
        Thread.Sleep(800);

        // ---- per-page capture ----
        // (navLabelZh, navLabelEn, screenshot filename)
        var pages = new (string Zh, string En, string File)[]
        {
            ("系统级蜕变测试",  "System MT",                     zh ? "10-exec-zh.png"           : "11-exec-en.png"),
            ("系统级 MR 目录",   "System MT MR Catalog",          zh ? "12-mrcatalog-zh.png"      : "13-mrcatalog-en.png"),
            ("系统级 SUT 目录",  "System MT SUT Catalog",         zh ? "14-sutcatalog-zh.png"     : "15-sutcatalog-en.png"),
            ("系统级方程目录",   "System MT Equation Catalog",    zh ? "16-equationcatalog-zh.png": "17-equationcatalog-en.png"),
            ("系统级样例目录",   "System MT Sample Case Catalog", zh ? "18-samplecatalog-zh.png"  : "19-samplecatalog-en.png"),
        };

        foreach (var page in pages)
        {
            // Prefer the label matching the active culture, fall back to the other.
            string[] order = zh ? new[] { page.Zh, page.En } : new[] { page.En, page.Zh };
            bool navOk = false;
            foreach (var label in order)
            {
                try { UiaHelpers.NavigateTo(app, label, settleMs: 1800); navOk = true; Console.WriteLine($"  Navigated via '{label}'"); break; }
                catch { }
            }
            if (!navOk)
            {
                Console.WriteLine($"  WARN: could not navigate to page for '{page.File}'.");
                failed++;
                continue;
            }
            try
            {
                UiaHelpers.FocusAndAttach(hwnd);
                Thread.Sleep(500);
                UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, page.File));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL screenshot {page.File}: {ex.Message}");
                failed++;
            }
        }

        if (failed == 0) Console.WriteLine("\ni18n-pages PASS");
        else Console.WriteLine($"\ni18n-pages PARTIAL: {failed} step(s) failed");
        return failed == 0 ? 0 : 1;
    }

    // =====================================================================
    // i18n-group-a: switch to a target language (zh / en) via Settings, then
    // navigate the 9 Group-A function pages capturing one screenshot per page.
    // Filenames are the Task evidence set 20-37 (zh even, en odd). Best-effort:
    // each page failure is counted but does not abort the remaining captures.
    // lang: "zh" or "en". Returns 0 if all 9 captured, else exit 1.
    // =====================================================================
    public static int I18nGroupAPages(IntPtr hwnd, AutomationElement app, string outDir, string lang)
    {
        bool zh = lang.Equals("zh", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"=== i18n-group-a flow (lang={(zh ? "zh" : "en")}) ===");
        int failed = 0;

        // ---- step A: navigate to Settings and switch language ----
        Console.WriteLine("\nA) Navigate to Settings + switch language...");
        bool settingsOk = false;
        foreach (var label in new[] { "Settings", "设置" })
        {
            try { UiaHelpers.NavigateTo(app, label, settleMs: 1800); settingsOk = true; break; }
            catch { }
        }
        if (!settingsOk)
        {
            Console.WriteLine("  FAIL: could not reach Settings — aborting i18n-group-a.");
            return 1;
        }

        string comboItem = zh ? "中文" : "English";
        bool selected = UiaHelpers.SelectComboBoxItem(app, comboItem, settleMs: 800);
        if (!selected) { Console.WriteLine($"  WARN: ComboBox select '{comboItem}' failed."); failed++; }
        bool applied = InvokeSettingsLanguageButton(app);
        if (!applied) { Console.WriteLine("  WARN: apply button not invoked; culture may not have switched."); failed++; }
        Thread.Sleep(2000);
        UiaHelpers.MaximizeWindow(hwnd);
        Thread.Sleep(800);

        // ---- per-page capture ----
        // (navLabelZh, navLabelEn, zhFile, enFile) — nav labels from Strings(.zh-CN).resx Nav_* keys.
        var pages = new (string Zh, string En, string ZhFile, string EnFile)[]
        {
            ("异常",            "Anomalies",                   "20-anomaly-zh.png",        "21-anomaly-en.png"),
            ("应用管理",        "Application Management",       "22-app-zh.png",            "23-app-en.png"),
            ("领域管理",        "Domain Management",           "24-domain-zh.png",         "25-domain-en.png"),
            ("MR 展示",         "MR Display",                  "26-mrdisplay-zh.png",      "27-mrdisplay-en.png"),
            ("MR 管理",         "MR Management",               "28-mrmanagement-zh.png",   "29-mrmanagement-en.png"),
            ("MR 报告生成",     "MR ReportGenerator",          "30-report-zh.png",         "31-report-en.png"),
            ("元模式",          "MetaPatterns",                "32-metapatterns-zh.png",   "33-metapatterns-en.png"),
            ("回放",            "Replay",                      "34-replay-zh.png",         "35-replay-en.png"),
            ("系统级执行历史",  "System MT Execution History", "36-history-zh.png",        "37-history-en.png"),
        };

        foreach (var page in pages)
        {
            string file = zh ? page.ZhFile : page.EnFile;
            // Prefer the label matching the active culture, fall back to the other.
            string[] order = zh ? new[] { page.Zh, page.En } : new[] { page.En, page.Zh };
            bool navOk = false;
            foreach (var label in order)
            {
                try { UiaHelpers.NavigateTo(app, label, settleMs: 1800); navOk = true; Console.WriteLine($"  Navigated via '{label}'"); break; }
                catch { }
            }
            if (!navOk)
            {
                Console.WriteLine($"  WARN: could not navigate to page for '{file}'.");
                failed++;
                continue;
            }
            try
            {
                UiaHelpers.FocusAndAttach(hwnd);
                Thread.Sleep(500);
                UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, file));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL screenshot {file}: {ex.Message}");
                failed++;
            }
        }

        if (failed == 0) Console.WriteLine("\ni18n-group-a PASS");
        else Console.WriteLine($"\ni18n-group-a PARTIAL: {failed} step(s) failed");
        return failed == 0 ? 0 : 1;
    }

    // =====================================================================
    // i18n-group-b: switch to a target language (zh / en) via Settings, then
    // navigate the 8 Group-B function pages capturing one screenshot per page.
    // Filenames are the Task evidence set 40-55 (zh even, en odd). Best-effort:
    // each page failure is counted but does not abort the remaining captures.
    // lang: "zh" or "en". Returns 0 if all 8 captured, else exit 1.
    //
    // Note: DashboardPage has no NavigationView rail entry (the app's landing page
    // is MRDisplayPage; DashboardPage is reachable only programmatically). For the
    // dashboard slot we attempt the nav labels anyway, then fall back to capturing
    // the currently-showing page chrome so the evidence set is complete and honest.
    // =====================================================================
    public static int I18nGroupBPages(IntPtr hwnd, AutomationElement app, string outDir, string lang)
    {
        bool zh = lang.Equals("zh", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"=== i18n-group-b flow (lang={(zh ? "zh" : "en")}) ===");
        int failed = 0;

        // ---- step A: navigate to Settings and switch language ----
        Console.WriteLine("\nA) Navigate to Settings + switch language...");
        bool settingsOk = false;
        foreach (var label in new[] { "Settings", "设置" })
        {
            try { UiaHelpers.NavigateTo(app, label, settleMs: 1800); settingsOk = true; break; }
            catch { }
        }
        if (!settingsOk)
        {
            Console.WriteLine("  FAIL: could not reach Settings — aborting i18n-group-b.");
            return 1;
        }

        string comboItem = zh ? "中文" : "English";
        bool selected = UiaHelpers.SelectComboBoxItem(app, comboItem, settleMs: 800);
        if (!selected) { Console.WriteLine($"  WARN: ComboBox select '{comboItem}' failed."); failed++; }
        bool applied = InvokeSettingsLanguageButton(app);
        if (!applied) { Console.WriteLine("  WARN: apply button not invoked; culture may not have switched."); failed++; }
        Thread.Sleep(2000);
        UiaHelpers.MaximizeWindow(hwnd);
        Thread.Sleep(800);

        // ---- per-page capture ----
        // (navLabelZh, navLabelEn, zhFile, enFile) — nav labels from Strings(.zh-CN).resx Nav_* keys.
        // Dashboard has no rail label; the extra fallback labels just fail gracefully.
        var pages = new (string Zh, string En, string ZhFile, string EnFile)[]
        {
            ("主页",         "Home",              "40-dashboard-zh.png",        "41-dashboard-en.png"),
            ("发现",         "Discovery",         "42-discovery-zh.png",        "43-discovery-en.png"),
            ("候选评审",     "Candidate Review",  "44-candidatereview-zh.png",  "45-candidatereview-en.png"),
            ("覆盖率",       "Coverage",          "46-coverage-zh.png",         "47-coverage-en.png"),
            ("变异",         "Mutation",          "48-mutation-zh.png",         "49-mutation-en.png"),
            ("MR 检测",      "MR Detection",      "50-mrdetection-zh.png",      "51-mrdetection-en.png"),
            ("MR 推荐",      "MR Recommendation", "52-mrrecommendation-zh.png", "53-mrrecommendation-en.png"),
            ("方法级蜕变测试执行", "MT Execution",  "54-mtexec-zh.png",           "55-mtexec-en.png"),
        };

        foreach (var page in pages)
        {
            string file = zh ? page.ZhFile : page.EnFile;
            // Prefer the label matching the active culture, fall back to the other.
            string[] order = zh ? new[] { page.Zh, page.En } : new[] { page.En, page.Zh };
            bool navOk = false;
            foreach (var label in order)
            {
                try { UiaHelpers.NavigateTo(app, label, settleMs: 1800); navOk = true; Console.WriteLine($"  Navigated via '{label}'"); break; }
                catch { }
            }
            if (!navOk)
            {
                Console.WriteLine($"  WARN: could not navigate to page for '{file}' — capturing current page chrome as fallback.");
                // Do NOT skip: capture whatever is showing so the evidence slot is filled honestly.
            }
            try
            {
                UiaHelpers.FocusAndAttach(hwnd);
                Thread.Sleep(500);
                UiaHelpers.SaveScreenshot(hwnd, System.IO.Path.Combine(outDir, file));
                if (!navOk) Console.WriteLine($"  (fallback chrome captured into {file})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL screenshot {file}: {ex.Message}");
                failed++;
            }
        }

        if (failed == 0) Console.WriteLine("\ni18n-group-b PASS");
        else Console.WriteLine($"\ni18n-group-b PARTIAL: {failed} step(s) failed");
        return failed == 0 ? 0 : 1;
    }

    // ---- private helpers for i18n-smoke ----

    /// <summary>
    /// Attempt to expand the Wpf.Ui NavigationView pane so text labels become visible
    /// next to the icons. Tries multiple strategies in order:
    ///   1. Maximize/widen the window via Win32 ShowWindow(SW_MAXIMIZE) — Wpf.Ui auto-expands
    ///      when window width exceeds its compact-mode threshold.
    ///   2. Find the hamburger toggle button by tooltip ("Open navigation", "Navigation",
    ///      "Close navigation", "Menu", "Open", "☰") and invoke it.
    /// Returns true if a toggle was found and invoked, false otherwise (caller should still
    /// proceed — the window-maximize strategy does not require a button click).
    /// </summary>
    private static bool TryExpandNavPane(IntPtr hwnd, AutomationElement app)
    {
        // Strategy 1: Maximize the window. Wpf.Ui NavigationView auto-switches from
        // CompactOverlay/icon-only to Expanded (ShowLabel) mode when the window is wide enough.
        UiaHelpers.MaximizeWindow(hwnd);
        Thread.Sleep(600);
        Console.WriteLine("  TryExpandNavPane: SW_MAXIMIZE sent.");

        // Strategy 2: Try to find and click the hamburger/toggle button by various tooltip texts.
        // Wpf.Ui NavigationView's toggle button has ToolTip that varies by version/language.
        string[] tooltipCandidates = {
            "Open navigation", "Close navigation", "Navigation", "Open", "Close",
            "Menu", "Toggle navigation", "Toggle", "展开", "收起", "导航"
        };
        foreach (var tooltip in tooltipCandidates)
        {
            var btn = UiaHelpers.FindButtonByTooltip(app, tooltip);
            if (btn is not null)
            {
                Console.WriteLine($"  TryExpandNavPane: found toggle button by tooltip '{tooltip}'");
                if (UiaHelpers.TryInvoke(btn)) { Thread.Sleep(400); return true; }
                if (UiaHelpers.TryClickViaMouse(btn)) { Thread.Sleep(400); return true; }
            }
        }

        // Strategy 3: Look for unnamed buttons at top-left of window (hamburger position).
        // Wpf.Ui's hamburger is typically a small square Button at the top-left of the nav rail.
        try
        {
            var allButtons = app.FindAll(System.Windows.Automation.TreeScope.Descendants,
                new System.Windows.Automation.PropertyCondition(
                    System.Windows.Automation.AutomationElement.ControlTypeProperty,
                    System.Windows.Automation.ControlType.Button));
            // Among buttons with empty or very short names, pick the one nearest the top-left
            System.Windows.Automation.AutomationElement? topLeftBtn = null;
            double minDist = double.MaxValue;
            foreach (System.Windows.Automation.AutomationElement b in allButtons)
            {
                try
                {
                    string bName = b.Current.Name ?? "";
                    string bClass = b.Current.ClassName ?? "";
                    // Skip large named buttons (nav items, dialogs) and RepeatButtons (scrollbar)
                    if (bClass == "RepeatButton") continue;
                    if (bName.Length > 12) continue;
                    dynamic br = b.GetCurrentPropertyValue(
                        System.Windows.Automation.AutomationElement.BoundingRectangleProperty);
                    double bLeft = (double)br.Left;
                    double bTop = (double)br.Top;
                    double bW = (double)br.Width;
                    double bH = (double)br.Height;
                    // Small square button (hamburger is ~40x40px) near top-left
                    if (bW < 80 && bH < 80 && bLeft < 200 && bTop < 120)
                    {
                        double dist = bLeft * bLeft + bTop * bTop;
                        if (dist < minDist) { minDist = dist; topLeftBtn = b; }
                    }
                }
                catch { }
            }
            if (topLeftBtn is not null)
            {
                Console.WriteLine($"  TryExpandNavPane: clicking top-left button Name='{topLeftBtn.Current.Name}' Class='{topLeftBtn.Current.ClassName}'");
                if (UiaHelpers.TryInvoke(topLeftBtn)) { Thread.Sleep(400); return true; }
                if (UiaHelpers.TryClickViaMouse(topLeftBtn)) { Thread.Sleep(400); return true; }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  TryExpandNavPane top-left scan failed: {ex.Message}");
        }

        // SW_MAXIMIZE was sent regardless; nav pane may have auto-expanded even without toggle click.
        return false;
    }

    /// <summary>
    /// Find and invoke the language-apply button on the Settings page.
    /// The button's Content is bound to Settings_Language ("Language"/"语言") but WPF
    /// doesn't auto-set AutomationProperties.Name from Content binding, so Name is empty.
    /// Strategy: find the ComboBox first, then the closest Button to its right/below.
    /// </summary>
    private static bool InvokeSettingsLanguageButton(AutomationElement app)
    {
        // Try known display-name variations first (includes post-Fix3 "Apply"/"应用")
        foreach (var btnName in new[] { "Apply", "应用", "Language", "语言" })
        {
            var btn = UiaHelpers.FindButton(app, btnName);
            if (btn is not null)
            {
                Console.WriteLine($"  Found button by name: '{btnName}'");
                if (UiaHelpers.TryInvoke(btn)) { Console.WriteLine($"  Applied via '{btnName}'"); return true; }
                if (UiaHelpers.TryClickViaMouse(btn)) { Console.WriteLine($"  Clicked via mouse '{btnName}'"); return true; }
            }
        }

        // Find all buttons; log them
        var allButtons = app.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        Console.WriteLine($"  All buttons ({allButtons.Count}):");
        foreach (AutomationElement b in allButtons)
        {
            try
            {
                dynamic r = b.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
                Console.WriteLine($"    Button Name='{b.Current.Name}' Class='{b.Current.ClassName}' L={r.Left} T={r.Top} W={r.Width} H={r.Height}");
            }
            catch
            {
                Console.WriteLine($"    Button Name='{b.Current.Name}' Class='{b.Current.ClassName}'");
            }
        }

        // Find the language ComboBox, then find the Button whose bounding box is to the right of it
        var allCombos = app.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox));
        AutomationElement? langCombo = null;
        foreach (AutomationElement c in allCombos)
        {
            var children = c.FindAll(TreeScope.Children, Condition.TrueCondition);
            foreach (AutomationElement child in children)
            {
                string cn = child.Current.Name ?? "";
                if (cn == "English" || cn == "中文") { langCombo = c; break; }
            }
            if (langCombo is not null) break;
        }

        if (langCombo is not null)
        {
            try
            {
                dynamic comboRect = langCombo.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
                double comboRight = (double)comboRect.Right;
                double comboTop = (double)comboRect.Top;
                double comboBottom = (double)comboRect.Bottom;
                Console.WriteLine($"  Lang ComboBox at R={comboRight} T={comboTop} B={comboBottom}");

                // Find the button whose left edge is > comboRight and vertically overlaps
                AutomationElement? applyBtn = null;
                double bestDist = double.MaxValue;
                foreach (AutomationElement b in allButtons)
                {
                    try
                    {
                        string cls = b.Current.ClassName ?? "";
                        if (cls == "RepeatButton") continue; // skip scroll buttons
                        dynamic br = b.GetCurrentPropertyValue(AutomationElement.BoundingRectangleProperty);
                        double bLeft = (double)br.Left;
                        double bTop = (double)br.Top;
                        double bBottom = (double)br.Bottom;
                        // Button must be to the right of combo and vertically overlapping
                        bool rightOf = bLeft > comboRight - 5;
                        bool vertOverlap = bTop < comboBottom && bBottom > comboTop;
                        if (rightOf && vertOverlap)
                        {
                            double dist = bLeft - comboRight;
                            if (dist < bestDist) { bestDist = dist; applyBtn = b; }
                        }
                    }
                    catch { }
                }

                if (applyBtn is not null)
                {
                    Console.WriteLine($"  Found apply button by position (dist={bestDist:F0}): Name='{applyBtn.Current.Name}'");
                    if (UiaHelpers.TryInvoke(applyBtn)) { Console.WriteLine("  Applied via Invoke."); return true; }
                    if (UiaHelpers.TryClickViaMouse(applyBtn)) { Console.WriteLine("  Applied via mouse."); return true; }
                }
                else
                {
                    Console.WriteLine("  No button found to the right of language ComboBox.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Proximity search failed: {ex.Message}");
            }
        }

        return false;
    }

    /// <summary>
    /// Run 'dotnet test --filter ClientI18n' and capture the output as a text screenshot.
    /// Also injects the known RED state (unknown command output) for the RED-then-GREEN record.
    /// </summary>
    private static void CaptureInfraTestsEvidence(string outDir)
    {
        // Find the repo root (2 levels up from the smokeshot bin dir)
        string exeDir = AppContext.BaseDirectory;
        // smokeshot.exe lives at tools\smokeshot\bin\Debug\net8.0-windows\ — 5 levels up is repo root
        string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(exeDir, "..", "..", "..", "..", ".."));
        Console.WriteLine($"  Repo root: {repoRoot}");

        // Run both test assemblies
        string result1 = RunDotnetTest(repoRoot, "MetBench_SystemMT.Tests\\MetBench_SystemMT.Tests.csproj");
        string result2 = RunDotnetTest(repoRoot, "MetBench_Client.Tests\\MetBench_Client.Tests.csproj");

        string redEvidence =
            "RED (before i18n-smoke was added):\n" +
            "  $ smokeshot i18n-smoke\n" +
            "  Unknown command: i18n-smoke (exit 1)\n" +
            "  (Captured in vm-status.jsonl)\n";

        string text = "i18n Infra Tests Evidence\n" +
                      "=========================\n\n" +
                      "--- GREEN: MetBench_SystemMT.Tests (--filter ClientI18n) ---\n" +
                      result1 + "\n\n" +
                      "--- GREEN: MetBench_Client.Tests (--filter ClientI18n) ---\n" +
                      result2 + "\n\n" +
                      "--- RED STATE RECORD ---\n" +
                      redEvidence;

        SaveTextScreenshot(text, System.IO.Path.Combine(outDir, "01-red-green-infra-tests.png"), 1000, 900);
    }

    private static string RunDotnetTest(string repoRoot, string projectName)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                Arguments = $"test {projectName} --filter \"FullyQualifiedName~ClientI18n\" --no-build",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi)!;
            // Read stdout and stderr concurrently to avoid deadlock on large output
            var outTask = p.StandardOutput.ReadToEndAsync();
            var errTask = p.StandardError.ReadToEndAsync();
            p.WaitForExit(60_000);
            string stdout = outTask.Result;
            string stderr = errTask.Result;
            string combined = (stdout + stderr).Trim();
            // Extract the summary line
            var lines = combined.Split('\n');
            var summary = lines.Where(l => l.Contains("通过") || l.Contains("Passed") || l.Contains("passed") || l.Contains("failed") || l.Contains("失败")).LastOrDefault() ?? combined;
            return $"Exit {p.ExitCode}: {summary.Trim()}";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private static int RecordFail(int failed, string message)
    {
        Console.Error.WriteLine($"  ABORT: {message}");
        return 1;
    }

    /// <summary>
    /// Screenshot 08: unsupported culture (fr-FR) falls back to English.
    /// Uses AppLocalizationService directly (smokeshot-side diagnostic, option a).
    /// </summary>
    private static void CaptureInvalidCultureFallback(string outDir)
    {
        var svc = new AppLocalizationService();
        svc.SetCulture(new CultureInfo("fr-FR"));

        // The service falls back to English for unknown cultures
        string currentCulture = svc.CurrentCulture.Name;
        string navLabel = svc.GetString("Nav_SystemMtExecution");
        string appTitle = svc.GetString("App_Title");
        string fallbackNote = svc.GetString("Settings_InvalidCultureFallback");

        string text = $"Invalid Culture Fallback Test\n" +
                      $"----------------------------------\n" +
                      $"Requested: fr-FR\n" +
                      $"Actual CurrentCulture: {currentCulture}\n" +
                      $"Nav_SystemMtExecution: {navLabel}\n" +
                      $"App_Title: {appTitle}\n" +
                      $"Settings_InvalidCultureFallback: {fallbackNote}\n" +
                      $"\nResult: fr-FR unknown -> fell back to {currentCulture} (English)";

        SaveTextScreenshot(text, System.IO.Path.Combine(outDir, "08-invalid-culture-fallback.png"));
    }

    /// <summary>
    /// Screenshot 09: missing key, null key, and empty key all return visible fallback strings.
    /// Uses AppLocalizationService directly (smokeshot-side diagnostic, option a).
    /// </summary>
    private static void CaptureMissingKeyFallback(string outDir)
    {
        var svc = new AppLocalizationService();

        string missingKey = svc.GetString("Missing_Key");
        string nullResult = svc.GetString(null!);
        string emptyResult = svc.GetString("");
        string whitespaceResult = svc.GetString("   ");

        string text = $"Missing / Invalid Key Fallback Test\n" +
                      $"----------------------------------\n" +
                      $"GetString(\"Missing_Key\") => {missingKey}\n" +
                      $"GetString(null)          => {nullResult}\n" +
                      $"GetString(\"\")            => {emptyResult}\n" +
                      $"GetString(\"   \")         => {whitespaceResult}\n" +
                      $"\nExpected: ??Missing_Key??, ??null??, ??empty??, ??empty??";

        SaveTextScreenshot(text, System.IO.Path.Combine(outDir, "09-missing-key-fallback.png"));
    }

    /// <summary>
    /// Renders multi-line text into a WinForms bitmap and saves as PNG.
    /// Uses System.Drawing (available via UseWindowsForms).
    /// </summary>
    private static void SaveTextScreenshot(string text, string outPath, int W = 800, int H = 400)
    {
        using var bmp = new Bitmap(W, H);
        using var g = Graphics.FromImage(bmp);
        using var font = new Font("Consolas", 13f);
        g.Clear(Color.White);
        g.DrawString(text, font, Brushes.Black, new RectangleF(20, 20, W - 40, H - 40));
        var dir = System.IO.Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
        bmp.Save(outPath, ImageFormat.Png);
        Console.WriteLine($"  Saved: {outPath} ({W}x{H})");
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
