using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Editing;
using MetBench_UI.Localization;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MetBench_Client.ViewModels
{
    public sealed partial class SystemMtMrCatalogViewModel : ObservableObject, INavigationAware
    {
        private readonly ISystemMtManifestCatalogEditor _editor;
        private bool _isInitialized;

        public LocalizedTextProvider Localization { get; }

        [ObservableProperty]
        private ObservableCollection<SystemMtManifestDescriptor> _manifests = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NewMrDraftCommand))]
        private SystemMtManifestDescriptor? _selectedManifest;

        [ObservableProperty]
        private ObservableCollection<SystemMtMrBindingDraft> _mrBindings = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateMrDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveMrDraftCommand))]
        [NotifyPropertyChangedFor(nameof(MetaPatternRationaleDisplay))]
        [NotifyPropertyChangedFor(nameof(TransformationSemanticsDisplay))]
        [NotifyPropertyChangedFor(nameof(ObservablesDisplay))]
        [NotifyPropertyChangedFor(nameof(PredicateDisplay))]
        [NotifyPropertyChangedFor(nameof(ToleranceDisplay))]
        [NotifyPropertyChangedFor(nameof(ApplicabilityDisplay))]
        [NotifyPropertyChangedFor(nameof(FailureMeaningDisplay))]
        private SystemMtMrBindingDraft? _selectedDraft;

        [ObservableProperty]
        private ObservableCollection<string> _availableAssertionTypeCodes = new(AssertionTypeCodes.All);

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasValidDraft;

        public SystemMtMrCatalogViewModel(ISystemMtManifestCatalogEditor editor, LocalizedTextProvider localization)
        {
            _editor = editor;
            Localization = localization;
            StatusMessage = Localization["Status_Catalog_SelectManifest"];
        }

        public void OnNavigatedTo()
        {
            if (_isInitialized) return;
            LoadManifests();
            _isInitialized = true;
        }

        public void OnNavigatedFrom() { }

        partial void OnSelectedManifestChanged(SystemMtManifestDescriptor? value)
        {
            LoadSelectedManifest();
            NewMrDraftCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedDraftChanged(SystemMtMrBindingDraft? value)
        {
            HasValidDraft = false;
            ValidateMrDraftCommand.NotifyCanExecuteChanged();
            SaveMrDraftCommand.NotifyCanExecuteChanged();
        }

        partial void OnHasValidDraftChanged(bool value)
        {
            SaveMrDraftCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RefreshManifests()
        {
            LoadManifests();
        }

        [RelayCommand(CanExecute = nameof(CanCreateDraft))]
        private void NewMrDraft()
        {
            if (SelectedManifest is null) return;

            var draft = SystemMtMrBindingDraft.NewForSut(SelectedManifest.SutId) with
            {
                AssertionTypeCode = "greater",
                AssertionName = "greater",
                TransformationName = "ScaleAmplitude",
                TransformStepName = "ScaleAmplitude",
                TransformTargetFieldPath = "initial.amplitude",
                ValueName = "value",
                WorkRootName = "system-mt-draft",
            };
            MrBindings.Add(draft);
            SelectedDraft = draft;
            StatusMessage = Localization["Status_Catalog_DraftCreated"];
        }

        [RelayCommand(CanExecute = nameof(HasSelectedDraft))]
        private void ValidateMrDraft()
        {
            if (SelectedManifest is null || SelectedDraft is null) return;

            var result = _editor.ValidateDraft(SelectedManifest.SutId, SelectedDraft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? Localization["Status_Catalog_DraftValid"]
                : string.Format(Localization["Status_Catalog_ValidationFailed_Fmt"], string.Join(" | ", result.Errors));
        }

        [RelayCommand(CanExecute = nameof(CanSaveDraft))]
        private void SaveMrDraft()
        {
            if (SelectedManifest is null || SelectedDraft is null) return;

            var result = _editor.SaveDraft(SelectedManifest.SutId, SelectedDraft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? Localization["Status_Catalog_DraftSaved"]
                : string.Format(Localization["Status_Catalog_SaveBlocked_Fmt"], string.Join(" | ", result.Errors));

            if (result.Success)
                LoadSelectedManifest(SelectedDraft.MrId);
        }

        private void LoadManifests()
        {
            try
            {
                var manifests = _editor.ListManifests();
                Manifests = new ObservableCollection<SystemMtManifestDescriptor>(manifests);
                SelectedManifest ??= Manifests.FirstOrDefault();
                StatusMessage = Manifests.Count == 0
                    ? Localization["Status_Catalog_NoManifests"]
                    : string.Format(Localization["Status_Catalog_ManifestsLoaded_Fmt"], Manifests.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_Catalog_LoadManifestsError_Fmt"], ex.Message);
            }
        }

        private void LoadSelectedManifest(string? selectMrId = null)
        {
            HasValidDraft = false;
            MrBindings.Clear();

            if (SelectedManifest is null)
                return;

            try
            {
                var document = _editor.Load(SelectedManifest.SutId);
                var drafts = document.Mrs.Select(SystemMtMrBindingDraft.FromBinding).ToList();
                MrBindings = new ObservableCollection<SystemMtMrBindingDraft>(drafts);
                SelectedDraft = string.IsNullOrWhiteSpace(selectMrId)
                    ? MrBindings.FirstOrDefault()
                    : MrBindings.FirstOrDefault(mr => string.Equals(mr.MrId, selectMrId, StringComparison.Ordinal))
                        ?? MrBindings.FirstOrDefault();
                StatusMessage = string.Format(Localization["Status_Catalog_BindingsLoaded_Fmt"], MrBindings.Count, SelectedManifest.SutId);
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_Catalog_LoadManifestsError_Fmt"], ex.Message);
            }
        }

        // PR-5 MR explanation card — read-only projection of the selected MR
        // binding's explanation profile (PR-2 fields). Empty fields fall back to
        // the shared localized "unavailable" text.
        public string MetaPatternRationaleDisplay => OrUnavailable(SelectedDraft?.MetaPatternRationale);
        public string TransformationSemanticsDisplay => OrUnavailable(SelectedDraft?.TransformationSemantics);
        public string ObservablesDisplay => OrUnavailable(SelectedDraft?.ObservableSummary);
        public string PredicateDisplay => OrUnavailable(SelectedDraft?.PredicateSummary);
        public string ToleranceDisplay => OrUnavailable(SelectedDraft?.ToleranceSummary);
        public string ApplicabilityDisplay => OrUnavailable(SelectedDraft?.Applicability);
        public string FailureMeaningDisplay => OrUnavailable(SelectedDraft?.FailureMeaning);

        private string OrUnavailable(string? value)
            => string.IsNullOrWhiteSpace(value) ? Localization["SystemMt_Explanation_Unavailable"] : value;

        private bool CanCreateDraft() => SelectedManifest is not null;

        private bool HasSelectedDraft() => SelectedManifest is not null && SelectedDraft is not null;

        private bool CanSaveDraft() => HasSelectedDraft() && HasValidDraft;
    }
}
