using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.Discovery;
using MetBench_BLL.Discovery.Validators;
using MetBench_Domain;
using MetBench_IDAL;
using MetBench_UI.Localization;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels;

public sealed partial class CandidateReviewViewModel : ObservableObject, INavigationAware
{
    private readonly ValidationService _validationService;
    private readonly EmpiricalValidator _empirical;
    private readonly TheoreticalLlmValidator _theoreticalLlm;
    private readonly ICandidateMRRepository _candidateRepo;
    private readonly IValidationRunRepository _validationRunRepo;

    [ObservableProperty]
    private ObservableCollection<CandidateMR> _candidates = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateSelectedCommand))]
    private CandidateMR? _selectedCandidate;

    [ObservableProperty]
    private ObservableCollection<ValidationRun> _validationHistory = new();

    [ObservableProperty] private bool _useEmpirical = true;
    [ObservableProperty] private bool _useTheoreticalLlm = true;

    [ObservableProperty]
    private ValidationSummary? _lastSummary;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateSelectedCommand))]
    private bool _isBusy;

    public CandidateReviewViewModel(
        ValidationService validationService,
        EmpiricalValidator empirical,
        TheoreticalLlmValidator theoreticalLlm,
        ICandidateMRRepository candidateRepo,
        IValidationRunRepository validationRunRepo,
        LocalizedTextProvider localization)
    {
        _validationService = validationService;
        _empirical = empirical;
        _theoreticalLlm = theoreticalLlm;
        _candidateRepo = candidateRepo;
        _validationRunRepo = validationRunRepo;
        Localization = localization;
    }

    public LocalizedTextProvider Localization { get; }

    public void OnNavigatedTo() => LoadCandidates();
    public void OnNavigatedFrom() { }

    private void LoadCandidates()
    {
        var page = _candidateRepo.GetPage(0, 200);
        var prevId = SelectedCandidate?.IdCandidate;
        Candidates.Clear();
        foreach (var c in page)
            Candidates.Add(c);
        if (prevId.HasValue)
            SelectedCandidate = Candidates.FirstOrDefault(c => c.IdCandidate == prevId.Value);
    }

    partial void OnSelectedCandidateChanged(CandidateMR? value)
    {
        ValidationHistory.Clear();
        StatusMessage = null;
        ErrorMessage = null;
        LastSummary = null;
        if (value is null) return;
        var runs = _validationRunRepo.GetByCandidate(value.IdCandidate);
        foreach (var r in runs)
            ValidationHistory.Add(r);
    }

    private bool CanValidate() => SelectedCandidate is not null && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanValidate))]
    private async Task ValidateSelectedAsync()
    {
        var validators = new List<IMRValidator>();
        if (UseEmpirical) validators.Add(_empirical);
        if (UseTheoreticalLlm) validators.Add(_theoreticalLlm);
        if (!validators.Any())
        {
            ErrorMessage = Localization["Status_CandidateReview_NoValidatorSelected"];
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = Localization["Status_CandidateReview_Validating"];
        try
        {
            var summary = await _validationService.ValidateAsync(
                SelectedCandidate!.IdCandidate, validators, "wpf-user");
            LastSummary = summary;
            StatusMessage = string.Format(Localization["Status_CandidateReview_ValidatorsPassed_Fmt"], summary.PassedValidators, summary.TotalValidatorsRun) +
                (summary.Promoted ? string.Format(Localization["Status_CandidateReview_PromotedSuffix_Fmt"], summary.PromotedMRId) : string.Empty);
            LoadCandidates();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = Localization["Status_CandidateReview_Error"];
        }
        finally
        {
            IsBusy = false;
        }
    }
}
