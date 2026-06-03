using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Catalog.Editing;
using MetBench_UI.Localization;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public sealed partial class SystemMtSutCatalogViewModel : ObservableObject, INavigationAware
    {
        private readonly ISystemMtSutEditor _editor;
        private bool _isInitialized;

        public LocalizedTextProvider Localization { get; }

        [ObservableProperty]
        private ObservableCollection<SystemMtSutDescriptor> _suts = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateSutDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveSutDraftCommand))]
        private SystemMtSutDescriptor? _selectedSut;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateSutDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveSutDraftCommand))]
        [NotifyPropertyChangedFor(nameof(ProfileProgramTypeDisplay))]
        [NotifyPropertyChangedFor(nameof(SolverMethodDisplay))]
        [NotifyPropertyChangedFor(nameof(RuntimeKeyDisplay))]
        [NotifyPropertyChangedFor(nameof(InputContractDisplay))]
        [NotifyPropertyChangedFor(nameof(OutputContractDisplay))]
        [NotifyPropertyChangedFor(nameof(AdapterDisplay))]
        [NotifyPropertyChangedFor(nameof(DependencyRiskDisplay))]
        private SystemMtSutProgramDraft? _draft;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private bool _hasValidDraft;

        // When true the SaveDraft path treats the draft as a brand-new SUT (writes an empty mrs array);
        // when false it preserves the existing mrs verbatim.
        private bool _isNewSutDraft;

        public SystemMtSutCatalogViewModel(ISystemMtSutEditor editor, LocalizedTextProvider localization)
        {
            _editor = editor;
            Localization = localization;
            _statusMessage = Localization["Status_SutCatalog_InitialHint"];
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
            _isNewSutDraft = false;
            if (value is null)
            {
                Draft = null;
                return;
            }

            try
            {
                Draft = _editor.Load(value.SutId);
                StatusMessage = string.Format(Localization["Status_SutCatalog_DraftLoaded_Fmt"], value.SutId);
            }
            catch (Exception ex)
            {
                Draft = null;
                StatusMessage = string.Format(Localization["Status_SutCatalog_DraftLoadError_Fmt"], value.SutId, ex.Message);
            }
        }

        partial void OnDraftChanged(SystemMtSutProgramDraft? value)
        {
            HasValidDraft = false;
        }

        partial void OnHasValidDraftChanged(bool value)
        {
            SaveSutDraftCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RefreshSuts()
        {
            LoadSuts();
        }

        [RelayCommand]
        private void NewSutDraft()
        {
            _isNewSutDraft = true;
            SelectedSut = null;
            Draft = SystemMtSutProgramDraft.NewForSut(string.Empty);
            HasValidDraft = false;
            StatusMessage = Localization["Status_SutCatalog_NewDraft"];
        }

        [RelayCommand(CanExecute = nameof(HasDraft))]
        private void ValidateSutDraft()
        {
            if (Draft is null) return;

            var sutId = ResolveTargetSutId();
            if (string.IsNullOrWhiteSpace(sutId))
            {
                HasValidDraft = false;
                StatusMessage = Localization["Status_SutCatalog_ValidationFailed_NameRequired"];
                return;
            }

            var result = _editor.ValidateDraft(sutId, Draft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? Localization["Status_SutCatalog_DraftValid"]
                : string.Format(Localization["Status_SutCatalog_ValidationFailed_Fmt"], string.Join(" | ", result.Errors));
        }

        [RelayCommand(CanExecute = nameof(CanSaveDraft))]
        private void SaveSutDraft()
        {
            if (Draft is null) return;

            var sutId = ResolveTargetSutId();
            if (string.IsNullOrWhiteSpace(sutId))
            {
                StatusMessage = Localization["Status_SutCatalog_SaveBlocked_NameRequired"];
                return;
            }

            var result = _editor.SaveDraft(sutId, Draft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? string.Format(Localization["Status_SutCatalog_DraftSaved_Fmt"], sutId)
                : string.Format(Localization["Status_SutCatalog_SaveBlocked_Fmt"], string.Join(" | ", result.Errors));

            if (result.Success)
            {
                _isNewSutDraft = false;
                LoadSuts(reselectSutId: sutId);
            }
        }

        private void LoadSuts(string? reselectSutId = null)
        {
            try
            {
                var suts = _editor.ListSuts();
                Suts = new ObservableCollection<SystemMtSutDescriptor>(suts);
                StatusMessage = Suts.Count == 0
                    ? Localization["Status_SutCatalog_NoCatalogFiles"]
                    : string.Format(Localization["Status_SutCatalog_Loaded_Fmt"], Suts.Count);

                if (!string.IsNullOrWhiteSpace(reselectSutId))
                {
                    SelectedSut = Suts.FirstOrDefault(s =>
                        string.Equals(s.SutId, reselectSutId, StringComparison.Ordinal));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_SutCatalog_ListError_Fmt"], ex.Message);
            }
        }

        private string ResolveTargetSutId()
        {
            if (_isNewSutDraft)
                return Draft?.SutName ?? string.Empty;
            return SelectedSut?.SutId ?? Draft?.SutName ?? string.Empty;
        }

        // PR-5 SUT profile card — read-only projection of the selected SUT's
        // documentation profile (PR-1 fields). Empty fields fall back to the
        // shared localized "unavailable" text.
        public string ProfileProgramTypeDisplay => OrUnavailable(Draft?.ProfileProgramType);
        public string SolverMethodDisplay => OrUnavailable(Draft?.SolverMethod);
        public string RuntimeKeyDisplay => OrUnavailable(Draft?.RuntimeKey);
        public string InputContractDisplay => OrUnavailable(Draft?.InputContract);
        public string OutputContractDisplay => OrUnavailable(Draft?.OutputContract);
        public string AdapterDisplay => OrUnavailable(Draft?.Adapter);
        public string DependencyRiskDisplay => OrUnavailable(Draft?.DependencyRisk);

        private string OrUnavailable(string? value)
            => string.IsNullOrWhiteSpace(value) ? Localization["SystemMt_Explanation_Unavailable"] : value;

        private bool HasDraft() => Draft is not null;
        private bool CanSaveDraft() => HasDraft() && HasValidDraft;
    }
}
