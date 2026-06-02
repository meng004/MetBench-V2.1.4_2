using MetBench_BLL.SystemMT.Catalog;
using MetBench_BLL.SystemMT.Catalog.Editing;
using MetBench_BLL.SystemMT.Metadata.Editing;
using MetBench_Client.ViewModels;
using MetBench_UI.Localization;
using Xunit;

namespace MetBench_Client.Tests.ClientI18n;

/// <summary>
/// PR-5 explanation surfaces. Asserts each catalog ViewModel exposes the
/// already-persisted explanation/profile fields for the selected row, and that
/// empty fields fall back to the shared localized "unavailable" text rather
/// than rendering blank.
/// </summary>
public sealed class SystemMtExplanationCardTests
{
    private static (SystemMtEquationCatalogViewModel vm, LocalizedTextProvider loc) BuildEquationVm()
    {
        var localization = new AppLocalizationService();
        var loc = new LocalizedTextProvider(localization);
        return (new SystemMtEquationCatalogViewModel(new FakeEquationEditor(), loc), loc);
    }

    [WpfFact]
    public void Equation_built_in_row_exposes_explanation_fields()
    {
        var (vm, _) = BuildEquationVm();

        // "bateman" is a built-in seed equation in SystemMtMetadataCatalog.Equations.
        vm.SelectedEquation = new EquationListItem(
            "bateman", "Bateman", "dN_i/dt", EquationSourceKinds.BuiltIn);

        Assert.NotNull(vm.Draft);
        Assert.Equal("ODE", vm.Draft!.EquationClass);
        Assert.Equal("linear decay chain", vm.Draft.EquationFamily);

        Assert.Equal("ODE", vm.EquationClassDisplay);
        Assert.Equal("linear decay chain", vm.EquationFamilyDisplay);
        Assert.Contains("N_i", vm.PrimaryVariablesDisplay);
        Assert.False(string.IsNullOrWhiteSpace(vm.PhysicalMeaningDisplay));
        Assert.False(string.IsNullOrWhiteSpace(vm.BenchmarkRationaleDisplay));
        Assert.Contains("superposition", vm.ExpectedLawsDisplay);
    }

    [WpfFact]
    public void Equation_empty_draft_falls_back_to_localized_unavailable()
    {
        var (vm, loc) = BuildEquationVm();
        var unavailable = loc["SystemMt_Explanation_Unavailable"];

        vm.NewEquationDraftCommand.Execute(null);

        Assert.Equal(unavailable, vm.EquationClassDisplay);
        Assert.Equal(unavailable, vm.EquationFamilyDisplay);
        Assert.Equal(unavailable, vm.PrimaryVariablesDisplay);
        Assert.Equal(unavailable, vm.PhysicalMeaningDisplay);
        Assert.Equal(unavailable, vm.BenchmarkRationaleDisplay);
        Assert.Equal(unavailable, vm.ExpectedLawsDisplay);
    }

    [WpfFact]
    public void Sut_selected_row_exposes_profile_fields()
    {
        var localization = new AppLocalizationService();
        var loc = new LocalizedTextProvider(localization);
        var draft = new SystemMtSutProgramDraft
        {
            SutName = "heat_equation",
            ProgramName = "heat_equation",
            ProfileProgramType = "Num",
            SolverMethod = "forward-euler",
            RuntimeKey = "system",
            InputContract = "json:initial.amplitude",
            OutputContract = "json:max_u",
            Adapter = "json-roundtrip",
            DependencyRisk = "low",
        };
        var vm = new SystemMtSutCatalogViewModel(new FakeSutEditor(draft), loc);

        vm.SelectedSut = new SystemMtSutDescriptor("heat_equation", "path", "heat_equation", "heat-equation-1d");

        Assert.Equal("Num", vm.ProfileProgramTypeDisplay);
        Assert.Equal("forward-euler", vm.SolverMethodDisplay);
        Assert.Equal("system", vm.RuntimeKeyDisplay);
        Assert.Equal("json:initial.amplitude", vm.InputContractDisplay);
        Assert.Equal("json:max_u", vm.OutputContractDisplay);
        Assert.Equal("json-roundtrip", vm.AdapterDisplay);
        Assert.Equal("low", vm.DependencyRiskDisplay);
    }

    [WpfFact]
    public void Sut_blank_profile_falls_back_to_localized_unavailable()
    {
        var localization = new AppLocalizationService();
        var loc = new LocalizedTextProvider(localization);
        var unavailable = loc["SystemMt_Explanation_Unavailable"];
        var draft = new SystemMtSutProgramDraft { SutName = "bare", ProgramName = "bare" };
        var vm = new SystemMtSutCatalogViewModel(new FakeSutEditor(draft), loc);

        vm.SelectedSut = new SystemMtSutDescriptor("bare", "path", "bare", "eq");

        Assert.Equal(unavailable, vm.SolverMethodDisplay);
        Assert.Equal(unavailable, vm.InputContractDisplay);
        Assert.Equal(unavailable, vm.DependencyRiskDisplay);
    }

    [WpfFact]
    public void Mr_selected_draft_exposes_explanation_profile_fields()
    {
        var localization = new AppLocalizationService();
        var loc = new LocalizedTextProvider(localization);
        var binding = new MrBindingDefinition
        {
            MrId = "heat-equation-amplitude",
            SutName = "heat_equation",
            DisplayName = "Amplitude scaling",
            TransformationName = "ScaleAmplitude",
            AssertionTypeCode = "greater",
            AssertionName = "greater",
            ValueName = "max_u",
            TransformSteps = new List<MrTransformStepDefinition>
            {
                new() { TransformationName = "ScaleAmplitude", TargetFieldPath = "initial.amplitude" },
            },
            ExplanationProfile = new MrExplanationProfileDefinition
            {
                MetaPatternRationale = "Linearity of the heat equation.",
                TransformationSemantics = "Scale the initial amplitude by factor > 1.",
                ObservableSummary = "Compare peak temperature max_u.",
                PredicateSummary = "max_u(follow-up) > max_u(source).",
                ToleranceSummary = "Ordinal — exact ordering.",
                Applicability = "Applicable when the SUT exposes max_u.",
                FailureMeaning = "Violation indicates non-linear amplitude response.",
            },
        };
        var doc = new SystemMtCatalogDocument
        {
            SutName = "heat_equation",
            Mrs = new List<MrBindingDefinition> { binding },
        };
        var vm = new SystemMtMrCatalogViewModel(new FakeManifestEditor(doc), loc);

        vm.OnNavigatedTo();

        Assert.NotNull(vm.SelectedDraft);
        Assert.Equal("Linearity of the heat equation.", vm.MetaPatternRationaleDisplay);
        Assert.Equal("Scale the initial amplitude by factor > 1.", vm.TransformationSemanticsDisplay);
        Assert.Equal("Compare peak temperature max_u.", vm.ObservablesDisplay);
        Assert.Equal("max_u(follow-up) > max_u(source).", vm.PredicateDisplay);
        Assert.Equal("Ordinal — exact ordering.", vm.ToleranceDisplay);
        Assert.Equal("Applicable when the SUT exposes max_u.", vm.ApplicabilityDisplay);
        Assert.Equal("Violation indicates non-linear amplitude response.", vm.FailureMeaningDisplay);
    }

    [WpfFact]
    public void Mr_blank_profile_falls_back_to_localized_unavailable()
    {
        var localization = new AppLocalizationService();
        var loc = new LocalizedTextProvider(localization);
        var unavailable = loc["SystemMt_Explanation_Unavailable"];
        var binding = new MrBindingDefinition
        {
            MrId = "bare-mr",
            SutName = "heat_equation",
            TransformationName = "ScaleAmplitude",
            AssertionTypeCode = "greater",
            AssertionName = "greater",
            ValueName = "max_u",
            TransformSteps = new List<MrTransformStepDefinition>
            {
                new() { TransformationName = "ScaleAmplitude", TargetFieldPath = "initial.amplitude" },
            },
            ExplanationProfile = new MrExplanationProfileDefinition(),
        };
        var doc = new SystemMtCatalogDocument
        {
            SutName = "heat_equation",
            Mrs = new List<MrBindingDefinition> { binding },
        };
        var vm = new SystemMtMrCatalogViewModel(new FakeManifestEditor(doc), loc);

        vm.OnNavigatedTo();

        Assert.Equal(unavailable, vm.MetaPatternRationaleDisplay);
        Assert.Equal(unavailable, vm.ObservablesDisplay);
        Assert.Equal(unavailable, vm.FailureMeaningDisplay);
    }

    private sealed class FakeEquationEditor : ISystemMtEquationEditor
    {
        public Task<IReadOnlyList<EquationListItem>> ListEquationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EquationListItem>>(new List<EquationListItem>());

        public Task<EquationMetadataDraft?> LoadAsync(string equationKey, CancellationToken cancellationToken = default)
            => Task.FromResult<EquationMetadataDraft?>(null);

        public SystemMtManifestEditResult ValidateDraft(EquationMetadataDraft draft)
            => SystemMtManifestEditResult.Ok();

        public Task<SystemMtManifestEditResult> SaveDraftAsync(EquationMetadataDraft draft, CancellationToken cancellationToken = default)
            => Task.FromResult(SystemMtManifestEditResult.Ok());

        public Task<SystemMtManifestEditResult> DeleteAsync(string equationKey, CancellationToken cancellationToken = default)
            => Task.FromResult(SystemMtManifestEditResult.Ok());
    }

    private sealed class FakeSutEditor : ISystemMtSutEditor
    {
        private readonly SystemMtSutProgramDraft _draft;
        public FakeSutEditor(SystemMtSutProgramDraft draft) => _draft = draft;

        public IReadOnlyList<SystemMtSutDescriptor> ListSuts()
            => new List<SystemMtSutDescriptor>
            {
                new(_draft.SutName, "path", _draft.ProgramName, _draft.EquationKey),
            };

        public SystemMtSutProgramDraft Load(string sutId) => _draft;

        public SystemMtManifestEditResult ValidateDraft(string sutId, SystemMtSutProgramDraft draft)
            => SystemMtManifestEditResult.Ok();

        public SystemMtManifestEditResult SaveDraft(string sutId, SystemMtSutProgramDraft draft)
            => SystemMtManifestEditResult.Ok();
    }

    private sealed class FakeManifestEditor : ISystemMtManifestCatalogEditor
    {
        private readonly SystemMtCatalogDocument _doc;
        public FakeManifestEditor(SystemMtCatalogDocument doc) => _doc = doc;

        public IReadOnlyList<SystemMtManifestDescriptor> ListManifests()
            => new List<SystemMtManifestDescriptor>
            {
                new(_doc.SutName, "path", _doc.SutName),
            };

        public SystemMtCatalogDocument Load(string sutId) => _doc;

        public SystemMtManifestEditResult ValidateDraft(string sutId, SystemMtMrBindingDraft draft)
            => SystemMtManifestEditResult.Ok();

        public SystemMtManifestEditResult SaveDraft(string sutId, SystemMtMrBindingDraft draft)
            => SystemMtManifestEditResult.Ok();
    }
}
