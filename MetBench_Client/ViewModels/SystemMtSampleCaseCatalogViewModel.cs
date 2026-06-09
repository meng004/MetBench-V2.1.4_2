using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Catalog.Editing;
using MetBench_UI.Localization;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MetBench_Client.ViewModels
{
    public sealed partial class SystemMtSampleCaseCatalogViewModel : ObservableObject, INavigationAware
    {
        private readonly ISystemMtSampleCaseEditor _sampleEditor;
        private readonly ISystemMtSutEditor _sutEditor;
        private bool _isInitialized;

        public LocalizedTextProvider Localization { get; }

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
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasValidDraft;

        public SystemMtSampleCaseCatalogViewModel(
            ISystemMtSampleCaseEditor sampleEditor,
            ISystemMtSutEditor sutEditor,
            LocalizedTextProvider localization)
        {
            _sampleEditor = sampleEditor;
            _sutEditor = sutEditor;
            Localization = localization;
            StatusMessage = Localization["Status_SampleCatalog_SelectSut"];
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
                    ? string.Format(Localization["Status_SampleCatalog_NoSamplesUnderSut_Fmt"], value.SutId)
                    : string.Format(Localization["Status_SampleCatalog_LoadedSamples_Fmt"], Samples.Count, value.SutId);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_SampleCatalog_ErrorListingSamples_Fmt"], ex.Message);
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
                StatusMessage = string.Format(Localization["Status_SampleCatalog_LoadedSample_Fmt"], value.FileName);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_SampleCatalog_ErrorLoadingSample_Fmt"], value.FileName, ex.Message);
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
            StatusMessage = Localization["Status_SampleCatalog_EditThenValidate"];
        }

        [RelayCommand(CanExecute = nameof(HasDraftReadyForValidate))]
        private void ValidateSample()
        {
            if (SelectedSut is null) return;
            var result = _sampleEditor.ValidateDraft(SelectedSut.SutId, DraftFileName, DraftJsonBody);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? Localization["Status_SampleCatalog_DraftValid"]
                : string.Format(Localization["Status_SampleCatalog_ValidationFailed_Fmt"], string.Join(" | ", result.Errors));
        }

        [RelayCommand(CanExecute = nameof(CanSaveDraft))]
        private void SaveSample()
        {
            if (SelectedSut is null) return;
            var result = _sampleEditor.SaveDraft(SelectedSut.SutId, DraftFileName, DraftJsonBody);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? string.Format(Localization["Status_SampleCatalog_SampleSaved_Fmt"], DraftFileName)
                : string.Format(Localization["Status_SampleCatalog_SaveBlocked_Fmt"], string.Join(" | ", result.Errors));

            if (result.Success)
            {
                ReloadSamples(SelectedSut.SutId, reselectFileName: DraftFileName);
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedSample))]
        private void DeleteSample()
        {
            if (SelectedSut is null || SelectedSample is null) return;

            var confirm = System.Windows.MessageBox.Show(
                string.Format(Localization["Dialog_SampleCatalog_ConfirmDeleteText_Fmt"], SelectedSample.FileName, SelectedSut.SutId),
                Localization["Dialog_SampleCatalog_ConfirmDeleteCaption"],
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.OK) return;

            var result = _sampleEditor.Delete(SelectedSut.SutId, SelectedSample.FileName);
            StatusMessage = result.Success
                ? string.Format(Localization["Status_SampleCatalog_SampleDeleted_Fmt"], SelectedSample.FileName)
                : string.Format(Localization["Status_SampleCatalog_DeleteBlocked_Fmt"], string.Join(" | ", result.Errors));

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
                    ? Localization["Status_SampleCatalog_NoSuts"]
                    : string.Format(Localization["Status_SampleCatalog_LoadedSuts_Fmt"], Suts.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_SampleCatalog_ErrorListingSuts_Fmt"], ex.Message);
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
