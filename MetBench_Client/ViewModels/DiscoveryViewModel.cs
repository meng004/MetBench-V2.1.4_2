using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL;
using MetBench_BLL.Discovery;
using MetBench_Domain;
using MetBench_IDAL;
using MetBench_UI.Localization;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels;

public sealed partial class DiscoveryViewModel : ObservableObject, INavigationAware
{
    private readonly DiscoveryService _discoveryService;
    private readonly IEnumerable<IMRDiscoverer> _discoverers;
    private readonly ICandidateMRRepository _candidateRepo;
    private readonly ApplicationService _appService;

    [ObservableProperty]
    private ObservableCollection<IMRDiscoverer> _discovererList = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunDiscoveryCommand))]
    private IMRDiscoverer? _selectedDiscoverer;

    [ObservableProperty]
    private ObservableCollection<Application> _applications = new();

    [ObservableProperty]
    private Application? _selectedApplication;

    [ObservableProperty]
    private ObservableCollection<CandidateMR> _candidateItems = new();

    [ObservableProperty]
    private DiscoveryExecutionResult? _lastRun;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunDiscoveryCommand))]
    private bool _isBusy;

    public DiscoveryViewModel(
        DiscoveryService discoveryService,
        IEnumerable<IMRDiscoverer> discoverers,
        ICandidateMRRepository candidateRepo,
        ApplicationService appService,
        LocalizedTextProvider localization)
    {
        _discoveryService = discoveryService;
        _discoverers = discoverers;
        _candidateRepo = candidateRepo;
        _appService = appService;
        Localization = localization;
    }

    public LocalizedTextProvider Localization { get; }

    public void OnNavigatedTo()
    {
        DiscovererList.Clear();
        foreach (var d in _discoverers)
            DiscovererList.Add(d);
        if (DiscovererList.Count > 0 && SelectedDiscoverer is null)
            SelectedDiscoverer = DiscovererList[0];

        Applications.Clear();
        var apps = _appService.GetApplications();
        foreach (var app in apps)
            Applications.Add(app);
    }

    public void OnNavigatedFrom() { }

    private bool CanRunDiscovery() => SelectedDiscoverer is not null && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRunDiscovery))]
    private async Task RunDiscoveryAsync()
    {
        if (SelectedDiscoverer is null) return;
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Running discovery…";
        try
        {
            var sutId = SelectedApplication?.IdApplication;
            var result = await _discoveryService.RunAsync(
                SelectedDiscoverer, methodId: 1,
                targetApplicationId: sutId, actor: "wpf-user");
            LastRun = result;
            StatusMessage = $"Done — status: {result.RawOutcome.Status}, candidates: {result.CreatedCandidateIds.Count}";
            if (!string.IsNullOrEmpty(result.RawOutcome.ErrorMessage))
                ErrorMessage = result.RawOutcome.ErrorMessage;
            LoadCandidates();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Error";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadCandidates()
    {
        if (LastRun is null) return;
        var items = _candidateRepo.GetByDiscoveryRun(LastRun.DiscoveryRunId);
        CandidateItems.Clear();
        foreach (var item in items)
            CandidateItems.Add(item);
    }
}
