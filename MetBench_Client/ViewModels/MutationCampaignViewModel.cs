using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.Mutation;
using MetBench_Domain;
using MetBench_IDAL;
using MetBench_UI.Localization;

namespace MetBench_Client.ViewModels;

public sealed partial class MutationCampaignViewModel : ObservableObject, INavigationAware
{
    private readonly MutationCampaignService _campaignService;
    private readonly IMutantRepository _mutantRepo;
    private readonly IMRBindingRepository _mrBindingRepo;
    private readonly IMutationResultRepository _resultRepo;
    private CancellationTokenSource? _currentCts;

    [ObservableProperty]
    private ObservableCollection<MutantSelectable> _availableMutants = new();

    [ObservableProperty]
    private ObservableCollection<MRBindingSelectable> _availableBindings = new();

    [ObservableProperty]
    private string _campaignName = "Demo-Campaign";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCampaignCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCampaignCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private int _progressCurrent;

    [ObservableProperty]
    private int _progressTotal = 1;

    [ObservableProperty]
    private MutationCampaignSummary? _lastSummary;

    [ObservableProperty]
    private ObservableCollection<MutationResult> _lastResults = new();

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    public LocalizedTextProvider Localization { get; }

    public MutationCampaignViewModel(
        MutationCampaignService campaignService,
        IMutantRepository mutantRepo,
        IMRBindingRepository mrBindingRepo,
        IMutationResultRepository resultRepo,
        LocalizedTextProvider localization)
    {
        _campaignService = campaignService;
        _mutantRepo = mutantRepo;
        _mrBindingRepo = mrBindingRepo;
        _resultRepo = resultRepo;
        Localization = localization;
    }

    public void OnNavigatedTo()
    {
        try { LoadItems(); }
        catch (Exception ex) { ErrorMessage = string.Format(Localization["Status_Mutation_LoadError_Fmt"], ex.Message); }
    }

    public void OnNavigatedFrom() => _currentCts?.Cancel();

    private void LoadItems()
    {
        AvailableMutants.Clear();
        foreach (var m in _mutantRepo.GetActive())
            AvailableMutants.Add(new MutantSelectable(m, selected: true));

        AvailableBindings.Clear();
        foreach (var b in _mrBindingRepo.GetActive())
            AvailableBindings.Add(new MRBindingSelectable(b, selected: true));
    }

    [RelayCommand]
    private void SeedDemoData()
    {
        try { SeedDemoDataCore(); }
        catch (Exception ex) { ErrorMessage = string.Format(Localization["Status_Mutation_SeedError_Fmt"], ex.Message); }
    }

    private void SeedDemoDataCore()
    {
        var existing = _mutantRepo.GetActive();
        if (existing.Count == 0)
        {
            for (int i = 0; i < 5; i++)
                _mutantRepo.Add(new Mutant
                {
                    OperatorId = 1,
                    AppliedDiff = $"--- a/sut.py +++ b/sut.py\n# demo-mutant-{i + 1}",
                    Status = "active",
                });
        }

        var existingB = _mrBindingRepo.GetActive();
        if (existingB.Count == 0)
        {
            for (int i = 1; i <= 5; i++)
                _mrBindingRepo.Add(new MRBinding
                {
                    MRId = i,
                    ApplicationId = 1,
                    IsActive = true,
                    BoundBy = "seed",
                    DefaultSampleCasePath = string.Empty,
                });
        }

        LoadItems();
        StatusMessage = string.Format(Localization["Status_Mutation_Seeded_Fmt"], _mutantRepo.GetActive().Count, _mrBindingRepo.GetActive().Count);
    }


    private bool CanStart() => !IsBusy;
    private bool CanCancel() => IsBusy;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartCampaignAsync()
    {
        var mutantIds = AvailableMutants.Where(m => m.IsSelected).Select(m => m.Mutant.IdMutant).ToArray();
        var bindingIds = AvailableBindings.Where(b => b.IsSelected).Select(b => b.Binding.IdMRBinding).ToArray();

        if (mutantIds.Length == 0 || bindingIds.Length == 0)
        {
            ErrorMessage = Localization["Status_Mutation_SelectAtLeastOne"];
            return;
        }

        var spec = new MutationCampaignSpec(
            Name: CampaignName,
            MutantIds: mutantIds,
            MRBindingIds: bindingIds,
            SampleCasePaths: Array.Empty<string>(),
            CreatedBy: "wpf-user");

        ProgressCurrent = 0;
        ProgressTotal = mutantIds.Length * bindingIds.Length;
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = Localization["Status_Mutation_Starting"];
        LastSummary = null;
        LastResults.Clear();

        using var cts = new CancellationTokenSource();
        _currentCts = cts;

        var progressReporter = new Progress<MutationCellProgress>(p =>
        {
            ProgressCurrent = p.Total;
            StatusMessage = string.Format(Localization["Status_Mutation_CellProgress_Fmt"], p.Total, ProgressTotal, p.LastMutantId, p.LastBindingId);
        });

        try
        {
            var summary = await _campaignService.RunAsync(
                spec, StubCellRunner, "wpf-user", progressReporter, cts.Token);
            LastSummary = summary;
            StatusMessage = string.Format(Localization["Status_Mutation_Done_Fmt"], summary.TotalCells, summary.Detected, summary.Missed, summary.DetectionRate);
            LoadLastResults(summary.CampaignId);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localization["Status_Mutation_Cancelled"];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = Localization["Status_Mutation_Error"];
        }
        finally
        {
            IsBusy = false;
            _currentCts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelCampaign() => _currentCts?.Cancel();

    // PROTOTYPE — T6 mutation maturity is currently Prototype (see MutationCampaignService XML
    // doc + docs/superpowers/specs/2026-06-06-metbench-maturity-assessment.md Top risk #4).
    // This runner does NOT touch a real SUT or apply any mutant: outcomes are computed by a
    // deterministic hash of (mutantId, bindingId) so the WPF page can exercise UI flows
    // without a backend. Replacing this with a launcher-backed cellRunner that materializes
    // a patched SUT via IMutantApplicator and calls ISystemMtLauncher.RunAsync is documented
    // as deferred work in the remediation plan §5 (requires a per-run SUT-root override on
    // LauncherOptions). Anyone reading kill-rate / detection-rate stats off this runner must
    // treat them as simulated, not measured.
    private static async Task<MutationCellOutcome> StubCellRunner(MutationCellInput input, CancellationToken ct)
    {
        await Task.Delay(80, ct);
        var hash = ((input.MutantId * 17) + (input.MRBindingId * 31)) % 10;
        var outcome = hash < 6 ? "detected" : hash < 8 ? "missed" : hash < 9 ? "error" : "not-affected";
        return new MutationCellOutcome(
            ExecutionId: Guid.NewGuid(),
            Outcome: outcome,
            ObservedDelta: outcome == "detected" ? (double?)(0.01 * input.MutantId) : null,
            ExpectedDelta: outcome == "detected" ? (double?)0.0 : null,
            Notes: "SIMULATED (T6 Prototype StubCellRunner — see XML doc)");
    }

    private void LoadLastResults(Guid campaignId)
    {
        var rows = _resultRepo.GetByCampaign(campaignId);
        LastResults.Clear();
        foreach (var r in rows)
            LastResults.Add(r);
    }
}

public sealed class MutantSelectable : ObservableObject
{
    public Mutant Mutant { get; }
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public string DisplayName => $"Mut-{Mutant.IdMutant}  (Op:{Mutant.OperatorId})";
    public MutantSelectable(Mutant m, bool selected = false) { Mutant = m; _isSelected = selected; }
}

public sealed class MRBindingSelectable : ObservableObject
{
    public MRBinding Binding { get; }
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public string DisplayName => $"Bind-{Binding.IdMRBinding}  (MR:{Binding.MRId}, SUT:{Binding.ApplicationId})";
    public MRBindingSelectable(MRBinding b, bool selected = false) { Binding = b; _isSelected = selected; }
}
