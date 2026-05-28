using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Catalog.Editing;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public sealed partial class SystemMtSampleCaseCatalogViewModel : ObservableObject, INavigationAware
    {
        private readonly ISystemMtSampleCaseEditor _sampleEditor;
        private readonly ISystemMtSutEditor _sutEditor;
        private bool _isInitialized;

        [ObservableProperty]
        private ObservableCollection<SystemMtSutDescriptor> _suts = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NewSampleDraftCommand))]
        private SystemMtSutDescriptor? _selectedSut;

        [ObservableProperty]
        private ObservableCollection<SystemMtSampleCaseDescriptor> _samples = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateSampleCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveSampleCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteSampleCommand))]
        private SystemMtSampleCaseDescriptor? _selectedSample;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateSampleCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveSampleCommand))]
        private string _draftFileName = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateSampleCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveSampleCommand))]
        private string _draftJsonBody = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Select a SUT to browse its sample files.";

        [ObservableProperty]
        private bool _hasValidDraft;

        public SystemMtSampleCaseCatalogViewModel(
            ISystemMtSampleCaseEditor sampleEditor,
            ISystemMtSutEditor sutEditor)
        {
            _sampleEditor = sampleEditor;
            _sutEditor = sutEditor;
        }

        public void OnNavigatedTo()
        {
            if (_isInitialized) return;
            LoadSuts();
            _isInitialized = true;
        }

        public void OnNavigatedFrom() { }

        partial void OnSelectedSutChanged(SystemMtSutDescriptor? value)
        {
            HasValidDraft = false;
            Samples.Clear();
            SelectedSample = null;
            DraftFileName = string.Empty;
            DraftJsonBody = string.Empty;
            if (value is null) return;

            try
            {
                foreach (var s in _sampleEditor.ListSamples(value.SutId))
                    Samples.Add(s);
                StatusMessage = Samples.Count == 0
                    ? $"No samples under SUT '{value.SutId}'."
                    : $"Loaded {Samples.Count} sample(s) for SUT '{value.SutId}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR listing samples: {ex.Message}";
            }
        }

        partial void OnSelectedSampleChanged(SystemMtSampleCaseDescriptor? value)
        {
            HasValidDraft = false;
            if (value is null)
            {
                DraftFileName = string.Empty;
                DraftJsonBody = string.Empty;
                return;
            }

            try
            {
                DraftFileName = value.FileName;
                DraftJsonBody = _sampleEditor.LoadSample(value.SutId, value.FileName);
                StatusMessage = $"Loaded sample '{value.FileName}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR loading '{value.FileName}': {ex.Message}";
            }
        }

        partial void OnHasValidDraftChanged(bool value)
        {
            SaveSampleCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void Refresh()
        {
            LoadSuts(reselectSutId: SelectedSut?.SutId);
        }

        [RelayCommand(CanExecute = nameof(CanCreateDraft))]
        private void NewSampleDraft()
        {
            SelectedSample = null;
            DraftFileName = "new-sample.json";
            DraftJsonBody = "{ }\n";
            HasValidDraft = false;
            StatusMessage = "Edit the file name + JSON body, then Validate / Save.";
        }

        [RelayCommand(CanExecute = nameof(HasDraftReadyForValidate))]
        private void ValidateSample()
        {
            if (SelectedSut is null) return;
            var result = _sampleEditor.ValidateDraft(SelectedSut.SutId, DraftFileName, DraftJsonBody);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? "Sample draft is valid."
                : "Validation failed: " + string.Join(" | ", result.Errors);
        }

        [RelayCommand(CanExecute = nameof(CanSaveDraft))]
        private void SaveSample()
        {
            if (SelectedSut is null) return;
            var result = _sampleEditor.SaveDraft(SelectedSut.SutId, DraftFileName, DraftJsonBody);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? $"Sample '{DraftFileName}' saved."
                : "Save blocked: " + string.Join(" | ", result.Errors);

            if (result.Success)
            {
                ReloadSamples(SelectedSut.SutId, reselectFileName: DraftFileName);
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedSample))]
        private void DeleteSample()
        {
            if (SelectedSut is null || SelectedSample is null) return;

            var confirm = MessageBox.Show(
                $"Delete sample '{SelectedSample.FileName}' from SUT '{SelectedSut.SutId}'? Files referenced by an MR will be blocked.",
                "Confirm delete",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.OK) return;

            var result = _sampleEditor.Delete(SelectedSut.SutId, SelectedSample.FileName);
            StatusMessage = result.Success
                ? $"Sample '{SelectedSample.FileName}' deleted."
                : "Delete blocked: " + string.Join(" | ", result.Errors);

            if (result.Success)
                ReloadSamples(SelectedSut.SutId);
        }

        private void LoadSuts(string? reselectSutId = null)
        {
            try
            {
                Suts = new ObservableCollection<SystemMtSutDescriptor>(_sutEditor.ListSuts());
                if (!string.IsNullOrWhiteSpace(reselectSutId))
                    SelectedSut = Suts.FirstOrDefault(s =>
                        string.Equals(s.SutId, reselectSutId, StringComparison.Ordinal));
                StatusMessage = Suts.Count == 0
                    ? "No SUTs found."
                    : $"Loaded {Suts.Count} SUT(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR listing SUTs: {ex.Message}";
            }
        }

        private void ReloadSamples(string sutId, string? reselectFileName = null)
        {
            Samples.Clear();
            foreach (var s in _sampleEditor.ListSamples(sutId))
                Samples.Add(s);
            if (!string.IsNullOrWhiteSpace(reselectFileName))
                SelectedSample = Samples.FirstOrDefault(s =>
                    string.Equals(s.FileName, reselectFileName, StringComparison.Ordinal));
        }

        private bool CanCreateDraft() => SelectedSut is not null;
        private bool HasDraftReadyForValidate() =>
            SelectedSut is not null
            && !string.IsNullOrWhiteSpace(DraftFileName)
            && !string.IsNullOrWhiteSpace(DraftJsonBody);
        private bool CanSaveDraft() => HasDraftReadyForValidate() && HasValidDraft;
        private bool HasSelectedSample() => SelectedSut is not null && SelectedSample is not null;
    }
}
