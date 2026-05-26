using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Catalog.Editing;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public sealed partial class SystemMtMrCatalogViewModel : ObservableObject, INavigationAware
    {
        private readonly ISystemMtManifestCatalogEditor _editor;
        private bool _isInitialized;

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
        private SystemMtMrBindingDraft? _selectedDraft;

        [ObservableProperty]
        private ObservableCollection<string> _availableAssertionTypeCodes = new(AssertionTypeCodes.All);

        [ObservableProperty]
        private string _statusMessage = "Select a System MT manifest.";

        [ObservableProperty]
        private bool _hasValidDraft;

        public SystemMtMrCatalogViewModel(ISystemMtManifestCatalogEditor editor)
        {
            _editor = editor;
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
            StatusMessage = "New MR draft created. Fill fields, validate, then save.";
        }

        [RelayCommand(CanExecute = nameof(HasSelectedDraft))]
        private void ValidateMrDraft()
        {
            if (SelectedManifest is null || SelectedDraft is null) return;

            var result = _editor.ValidateDraft(SelectedManifest.SutId, SelectedDraft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? "MR draft is valid."
                : "Validation failed: " + string.Join(" | ", result.Errors);
        }

        [RelayCommand(CanExecute = nameof(CanSaveDraft))]
        private void SaveMrDraft()
        {
            if (SelectedManifest is null || SelectedDraft is null) return;

            var result = _editor.SaveDraft(SelectedManifest.SutId, SelectedDraft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? "MR draft saved and catalog reloaded."
                : "Save blocked: " + string.Join(" | ", result.Errors);

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
                    ? "No System MT catalog.json files found."
                    : $"Loaded {Manifests.Count} System MT manifest(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: {ex.Message}";
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
                StatusMessage = $"Loaded {MrBindings.Count} MR binding(s) from {SelectedManifest.SutId}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: {ex.Message}";
            }
        }

        private bool CanCreateDraft() => SelectedManifest is not null;

        private bool HasSelectedDraft() => SelectedManifest is not null && SelectedDraft is not null;

        private bool CanSaveDraft() => HasSelectedDraft() && HasValidDraft;
    }
}
