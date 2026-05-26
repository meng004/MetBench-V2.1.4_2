# T1 UI MR CRUD Windows VM Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Windows-verified WPF System MT MR CRUD workflow so a non-author user can list, inspect, create, edit, validate, and save System MT MR manifest assets without hand-editing `SUT/<sut>/catalog.json`.

**Architecture:** Keep System MT manifest editing in a small cross-platform BLL.Core service with xUnit coverage, then expose it through a WPF page in `MetBench_Client`. The WPF page edits draft `MrBindingDefinition` rows for one selected SUT manifest at a time, validates through the existing catalog document validation path, writes the manifest only on explicit Save, and never touches Method MT or legacy method-level MR pages.

**Tech Stack:** .NET 8, WPF `net8.0-windows7.0`, Wpf.Ui, CommunityToolkit.Mvvm, System.Text.Json, xUnit, Windows 11 VM over SSH/RDP, optional FlaUI/UIAutomation smoke checks.

---

## Scope And Non-Goals

This is a Windows/VM implementation plan. Cloud agents may inspect source, but WPF build/run/interaction evidence must come from the Parallels Windows 11 VM.

This plan is only for **System MT MR asset CRUD**. It must not retrofit the legacy method-level `MRManagementPage` into System MT. It must not add T4 discovery binding. It must not execute T1 multi-env work. It must not add new SUTs or new MR semantics.

The first UI slice intentionally supports one manifest file at a time and one MR binding row at a time. It does not need a visual JSON editor, multi-file diff view, or drag-and-drop asset import.

## Hard Environment Boundary

- Cloud/Linux: allowed for cross-platform service tests and static review.
- Windows SSH: required for `dotnet build MetBench_Client/MetBench_Client.csproj`, `dotnet build MetBench.sln`, app launch, logs, and optional automation.
- Windows RDP: required for visible WPF verification.
- WPF source may be edited in a branch, but completion cannot be claimed until the Windows VM build and visible interaction pass.

Use the known VM command shape:

```bash
rtk env TERM=xterm-256color ssh -tt codex@10.211.55.3
```

Windows user: `codex`.

## Preconditions

- [ ] PR-0 docs-only control gate is merged, or this plan is being added to the same docs-only PR before PR-0 merge.
- [ ] PR-1 T1 multi-env and PR-2 T4 binder are not mixed into this branch.
- [ ] Branch is created from latest `origin/main`.
- [ ] `docs/status/current.md` says `T1 UI MR CRUD` is open and Windows/VM scoped.
- [ ] `CLAUDE.md` §3 confirms `MetBench_Client` is Windows-only.
- [ ] Windows VM is reachable by SSH.
- [ ] RDP session is open or can be opened by the operator for visible UI verification.
- [ ] Stop if the requested implementation requires changing Method MT execution or typed predicate runtime.

## Files

- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/ISystemMtManifestCatalogEditor.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtManifestCatalogEditor.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtManifestDescriptor.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtManifestEditResult.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtMrBindingDraft.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Editing/SystemMtManifestCatalogEditorTests.cs`
- Create: `MetBench_Client/ViewModels/SystemMtMrCatalogViewModel.cs`
- Create: `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`
- Create: `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml.cs`
- Modify: `MetBench_Client/App.xaml.cs`
- Modify: `MetBench_Client/ViewModels/MainWindowViewModel.cs`
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

## UI Acceptance Contract

The first shippable UI must expose these user operations:

- Select a SUT manifest from discovered `SUT/<sut>/catalog.json` files.
- List existing MR bindings from that manifest.
- Select one MR and inspect its core fields.
- Create a new MR draft by copying required defaults from the selected manifest.
- Edit `MrId`, `DisplayName`, `Description`, `TransformationName`, `AssertionTypeCode`, `ValueName`, `EquationKey`, `MetaPattern`, `SampleCaseRelativePath`, `WorkRootName`, `TimeoutSeconds`, and one `factor` default parameter.
- Validate the current draft using existing catalog validation.
- Save the draft into the selected manifest only after validation succeeds.
- Show explicit status messages for validation errors and save success.
- Reload the manifest after save and show the new/updated MR row.

The UI must include stable automation ids:

- `SystemMtMrCatalogPage`
- `ComboBox_SystemMtManifest`
- `DataGrid_SystemMtMrBindings`
- `TextBox_MrId`
- `TextBox_DisplayName`
- `TextBox_TransformationName`
- `ComboBox_AssertionTypeCode`
- `TextBox_ValueName`
- `TextBox_Factor`
- `Button_NewMrDraft`
- `Button_ValidateMrDraft`
- `Button_SaveMrDraft`
- `TextBlock_SystemMtMrCatalogStatus`

## Task 1: Cross-Platform Manifest Editor Red Tests

**Files:**
- Create: `MetBench_SystemMT.Tests/SystemMT/Catalog/Editing/SystemMtManifestCatalogEditorTests.cs`

- [ ] **Step 1: Add failing test for manifest discovery**

Add this test class skeleton:

```csharp
using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Catalog.Editing;
using Xunit;

namespace MetBench_SystemMT.Tests.SystemMT.Catalog.Editing;

public sealed class SystemMtManifestCatalogEditorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "metbench-manifest-editor-" + Guid.NewGuid().ToString("N"));

    public SystemMtManifestCatalogEditorTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void ListManifests_returns_catalog_json_files_with_sut_ids()
    {
        WriteManifest("heat_equation", "heat-scale");
        WriteManifest("openmoc", "openmoc-scale");

        var editor = new SystemMtManifestCatalogEditor(_root);

        var manifests = editor.ListManifests();

        Assert.Equal(new[] { "heat_equation", "openmoc" }, manifests.Select(m => m.SutId).ToArray());
        Assert.All(manifests, m => Assert.EndsWith("catalog.json", m.ManifestPath, StringComparison.Ordinal));
    }

    private void WriteManifest(string sutId, string mrId)
    {
        var dir = Path.Combine(_root, sutId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "catalog.json"), $$"""
        {
          "program": {
            "program_kind": "{{sutId}}",
            "equation_key": "fourier",
            "program_type": "Num",
            "runner_script": "runner.py",
            "input_adapter_script": "input.py",
            "output_adapter_script": "output.py",
            "input_parser_script": "input_parser.py",
            "output_parser_script": "output_parser.py",
            "python_executable_kind": "system"
          },
          "mrs": [
            {
              "mr_id": "{{mrId}}",
              "sut_name": "{{sutId}}",
              "display_name": "{{mrId}} display",
              "transformation_name": "ScaleAmplitude",
              "assertion_type_code": "greater",
              "assertion_name": "greater",
              "value_name": "max_temperature",
              "default_parameters": { "factor": "2" },
              "transform_steps": [
                { "transformation_name": "ScaleAmplitude", "target_field_path": "initial.amplitude" }
              ],
              "equation_key": "fourier",
              "meta_pattern": "Mono",
              "sample_case": "sample/base.json",
              "work_root_name": "{{mrId}}",
              "timeout_seconds": 30
            }
          ]
        }
        """);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
```

- [ ] **Step 2: Run focused test and confirm red**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtManifestCatalogEditorTests
```

Expected: fail because `MetBench_BLL.SystemMT.Catalog.Editing` types do not exist.

## Task 2: Implement Manifest Discovery And Load

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtManifestDescriptor.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/ISystemMtManifestCatalogEditor.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtManifestCatalogEditor.cs`

- [ ] **Step 1: Add descriptor and interface**

Create:

```csharp
namespace MetBench_BLL.SystemMT.Catalog.Editing;

public sealed record SystemMtManifestDescriptor(
    string SutId,
    string ManifestPath,
    string DisplayName);

public interface ISystemMtManifestCatalogEditor
{
    IReadOnlyList<SystemMtManifestDescriptor> ListManifests();
    SystemMtCatalogDocument Load(string sutId);
}
```

- [ ] **Step 2: Implement discovery and load**

Create `SystemMtManifestCatalogEditor` with:

```csharp
namespace MetBench_BLL.SystemMT.Catalog.Editing;

public sealed class SystemMtManifestCatalogEditor : ISystemMtManifestCatalogEditor
{
    private readonly string _sutRoot;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public SystemMtManifestCatalogEditor(string sutRoot)
    {
        _sutRoot = sutRoot ?? throw new ArgumentNullException(nameof(sutRoot));
    }

    public IReadOnlyList<SystemMtManifestDescriptor> ListManifests()
    {
        if (!Directory.Exists(_sutRoot))
            return Array.Empty<SystemMtManifestDescriptor>();

        return Directory.GetDirectories(_sutRoot)
            .Select(dir => new { SutId = Path.GetFileName(dir), ManifestPath = Path.Combine(dir, "catalog.json") })
            .Where(x => File.Exists(x.ManifestPath))
            .OrderBy(x => x.SutId, StringComparer.Ordinal)
            .Select(x => new SystemMtManifestDescriptor(x.SutId, x.ManifestPath, x.SutId))
            .ToList();
    }

    public SystemMtCatalogDocument Load(string sutId)
    {
        var path = ResolveManifestPath(sutId);
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<SystemMtCatalogDocument>(json, JsonOptions)
            ?? throw new CatalogValidationException($"Manifest '{path}' deserialized to null");
        doc.Validate();
        return doc;
    }

    private string ResolveManifestPath(string sutId)
    {
        if (string.IsNullOrWhiteSpace(sutId))
            throw new ArgumentException("SUT id is required", nameof(sutId));
        if (sutId.Contains("..", StringComparison.Ordinal) || sutId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException($"Invalid SUT id '{sutId}'", nameof(sutId));
        var path = Path.Combine(_sutRoot, sutId, "catalog.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"System MT manifest not found for SUT '{sutId}'", path);
        return path;
    }
}
```

- [ ] **Step 3: Run focused test and confirm green**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtManifestCatalogEditorTests
```

Expected: pass for discovery test.

## Task 3: Add Draft Validation And Save

**Files:**
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtMrBindingDraft.cs`
- Create: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtManifestEditResult.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Editing/ISystemMtManifestCatalogEditor.cs`
- Modify: `MetBench_BLL.Core/SystemMT/Catalog/Editing/SystemMtManifestCatalogEditor.cs`
- Test: `MetBench_SystemMT.Tests/SystemMT/Catalog/Editing/SystemMtManifestCatalogEditorTests.cs`

- [ ] **Step 1: Add failing tests for validate and save**

Add tests:

```csharp
[Fact]
public void ValidateDraft_rejects_blank_mr_id_without_writing_file()
{
    WriteManifest("heat_equation", "heat-scale");
    var editor = new SystemMtManifestCatalogEditor(_root);
    var before = File.ReadAllText(Path.Combine(_root, "heat_equation", "catalog.json"));

    var result = editor.ValidateDraft("heat_equation", new SystemMtMrBindingDraft { MrId = "" });

    Assert.False(result.Success);
    Assert.Contains(result.Errors, e => e.Contains("MrId", StringComparison.OrdinalIgnoreCase));
    Assert.Equal(before, File.ReadAllText(Path.Combine(_root, "heat_equation", "catalog.json")));
}

[Fact]
public void SaveDraft_adds_new_binding_when_validation_passes()
{
    WriteManifest("heat_equation", "heat-scale");
    var editor = new SystemMtManifestCatalogEditor(_root);

    var draft = SystemMtMrBindingDraft.NewForSut("heat_equation");
    draft.MrId = "heat-new-scale";
    draft.DisplayName = "Heat new scale";
    draft.TransformationName = "ScaleAmplitude";
    draft.AssertionTypeCode = "greater";
    draft.AssertionName = "greater";
    draft.ValueName = "max_temperature";
    draft.EquationKey = "fourier";
    draft.MetaPattern = "Mono";
    draft.SampleCaseRelativePath = "sample/base.json";
    draft.WorkRootName = "heat-new-scale";
    draft.TimeoutSeconds = 30;
    draft.Factor = "2";
    draft.TransformStepName = "ScaleAmplitude";
    draft.TransformTargetFieldPath = "initial.amplitude";

    var result = editor.SaveDraft("heat_equation", draft);

    Assert.True(result.Success, string.Join("; ", result.Errors));
    var doc = editor.Load("heat_equation");
    Assert.Contains(doc.Mrs, mr => mr.MrId == "heat-new-scale");
}
```

- [ ] **Step 2: Extend interface**

Add:

```csharp
SystemMtManifestEditResult ValidateDraft(string sutId, SystemMtMrBindingDraft draft);
SystemMtManifestEditResult SaveDraft(string sutId, SystemMtMrBindingDraft draft);
```

- [ ] **Step 3: Implement draft DTO**

Create properties matching the UI acceptance contract and methods:

```csharp
public static SystemMtMrBindingDraft NewForSut(string sutId);
public MrBindingDefinition ToBinding();
public static SystemMtMrBindingDraft FromBinding(MrBindingDefinition binding);
```

`ToBinding()` must build exactly one transform step from `TransformStepName` and `TransformTargetFieldPath`, and must put `Factor` into `DefaultParameters["factor"]` only when non-blank.

- [ ] **Step 4: Implement validate and save**

`ValidateDraft` must:

- Load the existing manifest.
- Convert draft to `MrBindingDefinition`.
- Call `binding.Validate()`.
- Build a temporary document with existing program and MR list where this draft replaces same `MrId` or appends a new row.
- Call `doc.Validate()`.
- Return `Success=false` with error messages on exception.
- Never write files.

`SaveDraft` must:

- Call `ValidateDraft`.
- Stop if validation fails.
- Replace existing binding with same `MrId` or append new binding.
- Serialize using snake_case and indented JSON.
- Write atomically through `catalog.json.tmp` then replace.

- [ ] **Step 5: Run focused tests**

Run:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtManifestCatalogEditorTests
```

Expected: pass.

## Task 4: Add WPF Page And ViewModel Red Build

**Files:**
- Create: `MetBench_Client/ViewModels/SystemMtMrCatalogViewModel.cs`
- Create: `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`
- Create: `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml.cs`
- Modify: `MetBench_Client/App.xaml.cs`
- Modify: `MetBench_Client/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Add page and navigation references before implementation**

Add DI registrations in `App.xaml.cs`:

```csharp
services.AddSingleton<ISystemMtManifestCatalogEditor>(provider =>
    new SystemMtManifestCatalogEditor(
        provider.GetRequiredService<LauncherOptions>().SutRoot));
services.AddScoped<Views.Pages.SystemMtMrCatalogPage>();
services.AddScoped<ViewModels.SystemMtMrCatalogViewModel>();
```

Add navigation item in `MainWindowViewModel.InitializeViewModel()` after `System MT`:

```csharp
new NavigationViewItem()
{
    Content = "System MR Catalog",
    Icon = new SymbolIcon { Symbol = SymbolRegular.Library24 },
    TargetPageType = typeof(Views.Pages.SystemMtMrCatalogPage)
},
```

- [ ] **Step 2: Run Windows build and confirm red if page types are missing**

On Windows VM SSH:

```powershell
cd <repo>
dotnet build MetBench_Client/MetBench_Client.csproj
```

Expected before creating page/viewmodel: fail with missing `SystemMtMrCatalogPage` or `SystemMtMrCatalogViewModel`.

## Task 5: Implement WPF ViewModel

**Files:**
- Create: `MetBench_Client/ViewModels/SystemMtMrCatalogViewModel.cs`

- [ ] **Step 1: Implement ViewModel**

Use `ObservableObject`, `INavigationAware`, `[ObservableProperty]`, and `[RelayCommand]`.

Required public observable properties:

- `ObservableCollection<SystemMtManifestDescriptor> Manifests`
- `SystemMtManifestDescriptor? SelectedManifest`
- `ObservableCollection<SystemMtMrBindingDraft> Bindings`
- `SystemMtMrBindingDraft? SelectedBinding`
- `string StatusMessage`
- `bool IsBusy`

Required commands:

- `ReloadCommand`
- `NewMrDraftCommand`
- `ValidateMrDraftCommand`
- `SaveMrDraftCommand`

Behavior:

- `OnNavigatedTo()` loads manifests and first manifest bindings.
- changing `SelectedManifest` reloads bindings.
- changing `SelectedBinding` exposes edit fields through the selected draft object.
- `NewMrDraft` creates `SystemMtMrBindingDraft.NewForSut(SelectedManifest.SutId)` and selects it.
- `ValidateMrDraft` calls editor `ValidateDraft`.
- `SaveMrDraft` calls editor `SaveDraft`, then reloads and reselects the saved MR.
- error messages must go to `StatusMessage`, not only `MessageBox`.

- [ ] **Step 2: Ensure no dispatcher/manual thread code is added**

All command methods should be async `Task` or sync command methods. Do not manually marshal to UI thread unless a concrete failure requires it.

## Task 6: Implement WPF XAML And Code-Behind

**Files:**
- Create: `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml`
- Create: `MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml.cs`

- [ ] **Step 1: Add code-behind**

Create:

```csharp
using Wpf.Ui.Controls;

namespace MetBench_Client.Views.Pages;

public partial class SystemMtMrCatalogPage : INavigableView<ViewModels.SystemMtMrCatalogViewModel>
{
    public ViewModels.SystemMtMrCatalogViewModel ViewModel { get; }

    public SystemMtMrCatalogPage(ViewModels.SystemMtMrCatalogViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
```

- [ ] **Step 2: Add XAML layout**

The page must:

- root `<Page>` with `AutomationProperties.AutomationId="SystemMtMrCatalogPage"`.
- use Wpf.Ui controls consistently.
- bind through `{Binding ViewModel.X}`.
- include manifest selector, MR grid, editor fields, validate/save buttons, and status text.
- set all automation ids listed in the UI Acceptance Contract.

- [ ] **Step 3: Windows build**

On Windows VM SSH:

```powershell
cd <repo>
dotnet build MetBench_Client/MetBench_Client.csproj
```

Expected: 0 errors.

## Task 7: Cross-Platform And Windows Verification

**Files:**
- No new source unless failures require fixes.

- [ ] **Step 1: Run cross-platform tests**

On a machine with .NET 8:

```bash
rtk dotnet test MetBench_SystemMT.Tests --no-restore --filter FullyQualifiedName~SystemMtManifestCatalogEditorTests
rtk dotnet test MetBench_SystemMT.Tests --no-restore
```

Expected: 0 failures. Existing external runtime tests may skip with explicit reasons.

- [ ] **Step 2: Run Windows solution build**

On Windows VM SSH:

```powershell
cd <repo>
dotnet build MetBench.sln
```

Expected: 0 errors.

- [ ] **Step 3: Launch app from Windows SSH**

On Windows VM SSH:

```powershell
cd <repo>
dotnet run --project MetBench_Client/MetBench_Client.csproj
```

Expected: WPF app appears in the visible RDP session.

- [ ] **Step 4: Manual RDP verification**

In RDP:

1. Open navigation item `System MR Catalog`.
2. Confirm manifest combo is populated.
3. Select a lightweight SUT such as `heat_equation`.
4. Confirm MR grid lists existing MRs.
5. Select an MR and confirm fields populate.
6. Click `New`.
7. Fill a unique `MrId` such as `ui-smoke-heat-scale-<date>`.
8. Fill required fields using valid values:
   - `DisplayName`: `UI smoke heat scale`
   - `TransformationName`: `ScaleAmplitude`
   - `AssertionTypeCode`: `greater`
   - `ValueName`: `max_temperature`
   - `EquationKey`: `fourier`
   - `MetaPattern`: `Mono`
   - `SampleCaseRelativePath`: `sample/base.json`
   - `WorkRootName`: same as `MrId`
   - `TimeoutSeconds`: `30`
   - `Factor`: `2`
   - transform step target: `initial.amplitude`
9. Click `Validate`.
10. Confirm status says validation passed.
11. Click `Save`.
12. Confirm status says save succeeded.
13. Reload page and confirm the new MR appears.

- [ ] **Step 5: Cleanup smoke MR if it was only for manual verification**

If the smoke MR should not stay in the real catalog, remove it through the same UI and save, or revert only the test smoke row. Do not revert unrelated user changes.

## Task 8: Optional UIAutomation Smoke

**Files:**
- Create only if needed: `tools/smokeshot/system_mr_catalog_smoke.ps1`

- [ ] **Step 1: Prefer existing UIAutomation helpers**

Check existing `docs/uat/reports/*/_uat_helpers.ps1` and `tools/smokeshot/` before adding a new automation script.

- [ ] **Step 2: Add a small smoke script only if manual RDP is too slow to repeat**

The smoke script should verify:

- `SystemMtMrCatalogPage` exists.
- `ComboBox_SystemMtManifest` exists.
- `DataGrid_SystemMtMrBindings` exists.
- `Button_ValidateMrDraft` exists.
- `Button_SaveMrDraft` exists.

It does not need to perform full editing if manual RDP evidence already covers the workflow.

## Task 9: Docs, Status, Review, Commit, PR

**Files:**
- Modify: `docs/status/current.md`
- Modify: `docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md`

- [ ] **Step 1: Update status ledger**

After implementation and VM verification pass, update:

- `T1 UI MR CRUD`: controlled.
- Evidence: Windows `dotnet build MetBench_Client/MetBench_Client.csproj`, Windows `dotnet build MetBench.sln`, RDP visible workflow, optional UIAutomation smoke.
- Note that this completes UI MR CRUD, not T1 multi-env or T4 binder.

- [ ] **Step 2: Update active plan index**

Move this plan from gated/pending to completed after merge. Keep PR-1 T1 multi-env and PR-2 T4 binder states unchanged unless those PRs already merged.

- [ ] **Step 3: Layer 1 self-review**

Check:

- WPF page is System MT only.
- Legacy `MRManagementPage` behavior is not broken.
- No Method MT execution changes.
- Manifest writes happen only on explicit Save.
- Validate does not write.
- Save validates before writing.
- UI has stable automation ids.
- VM build and visible RDP workflow evidence exists.

- [ ] **Step 4: Layer 2 maintainer review**

Ask:

- Can a non-author add and validate a System MT MR without editing JSON?
- Can monitoring distinguish this from CLI CRUD?
- Does this PR accidentally broaden MR semantics?
- Does the UI make invalid manifests harder, not easier, to create?
- Is the Windows validation evidence strong enough?

- [ ] **Step 5: Commit**

Run:

```bash
rtk git status --short
rtk git add MetBench_BLL.Core/SystemMT/Catalog/Editing MetBench_SystemMT.Tests/SystemMT/Catalog/Editing MetBench_Client/ViewModels/SystemMtMrCatalogViewModel.cs MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml MetBench_Client/Views/Pages/SystemMtMrCatalogPage.xaml.cs MetBench_Client/App.xaml.cs MetBench_Client/ViewModels/MainWindowViewModel.cs docs/status/current.md docs/superpowers/plans/2026-05-25-metbench-active-plan-index.md
rtk git commit -m "feat(t1): add Windows UI for System MT MR catalog CRUD"
```

- [ ] **Step 6: PR**

Open PR title:

```text
feat(t1): add Windows UI for System MT MR catalog CRUD
```

PR body must include:

- Summary.
- Cross-platform tests.
- Windows build commands and outputs.
- RDP visible workflow evidence.
- Whether optional UIAutomation smoke was run.
- Explicit scope: System MT MR manifest CRUD only; no Method MT; no T4 binder; no T1 multi-env.

## Acceptance Criteria

- A user can operate System MT MR manifest CRUD through WPF without opening JSON manually.
- Validate catches invalid MR drafts before Save.
- Save writes only the selected manifest and only after validation passes.
- Existing MR execution page still lists MRs after save.
- `MetBench_SystemMT.Tests` passes or existing external-runtime tests skip with explicit reasons.
- Windows `MetBench_Client` build passes.
- Windows visible RDP workflow passes.
- Status ledger no longer marks UI MR CRUD open after merge.

## Stop Conditions

Stop and report instead of coding if:

- PR-0 is not merged and this plan is not intentionally being added to PR-0 docs-only.
- The branch includes PR-1 T1 multi-env or PR-2 T4 binder code.
- Windows VM SSH is unavailable.
- RDP cannot be used for visible UI verification.
- The design requires editing Method MT or legacy method-level MR execution.
- The implementation would bypass `MrBindingDefinition.Validate()` or `SystemMtCatalogDocument.Validate()`.
- The implementation would save manifest changes during validation.
- The WPF app cannot be built on Windows after reasonable local fixes.
