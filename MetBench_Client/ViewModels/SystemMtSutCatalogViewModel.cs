using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Catalog.Editing;
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

        [ObservableProperty]
        private ObservableCollection<SystemMtSutDescriptor> _suts = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateSutDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveSutDraftCommand))]
        private SystemMtSutDescriptor? _selectedSut;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateSutDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveSutDraftCommand))]
        private SystemMtSutProgramDraft? _draft;

        [ObservableProperty]
        private string _statusMessage = "Select a SUT to edit its program section, or click \"New SUT draft\".";

        [ObservableProperty]
        private bool _hasValidDraft;

        // When true the SaveDraft path treats the draft as a brand-new SUT (writes an empty mrs array);
        // when false it preserves the existing mrs verbatim.
        private bool _isNewSutDraft;

        public SystemMtSutCatalogViewModel(ISystemMtSutEditor editor)
        {
            _editor = editor;
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
                StatusMessage = $"Loaded program section for SUT '{value.SutId}'.";
            }
            catch (Exception ex)
            {
                Draft = null;
                StatusMessage = $"ERROR loading '{value.SutId}': {ex.Message}";
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
            StatusMessage = "New SUT draft. Fill SUT name and program fields, then Validate / Save.";
        }

        [RelayCommand(CanExecute = nameof(HasDraft))]
        private void ValidateSutDraft()
        {
            if (Draft is null) return;

            var sutId = ResolveTargetSutId();
            if (string.IsNullOrWhiteSpace(sutId))
            {
                HasValidDraft = false;
                StatusMessage = "Validation failed: SUT name is required.";
                return;
            }

            var result = _editor.ValidateDraft(sutId, Draft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? "SUT draft is valid."
                : "Validation failed: " + string.Join(" | ", result.Errors);
        }

        [RelayCommand(CanExecute = nameof(CanSaveDraft))]
        private void SaveSutDraft()
        {
            if (Draft is null) return;

            var sutId = ResolveTargetSutId();
            if (string.IsNullOrWhiteSpace(sutId))
            {
                StatusMessage = "Save blocked: SUT name is required.";
                return;
            }

            var result = _editor.SaveDraft(sutId, Draft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? $"SUT draft saved to {sutId}/catalog.json."
                : "Save blocked: " + string.Join(" | ", result.Errors);

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
                    ? "No SUT catalog.json files found."
                    : $"Loaded {Suts.Count} SUT(s).";

                if (!string.IsNullOrWhiteSpace(reselectSutId))
                {
                    SelectedSut = Suts.FirstOrDefault(s =>
                        string.Equals(s.SutId, reselectSutId, StringComparison.Ordinal));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR listing SUTs: {ex.Message}";
            }
        }

        private string ResolveTargetSutId()
        {
            if (_isNewSutDraft)
                return Draft?.SutName ?? string.Empty;
            return SelectedSut?.SutId ?? Draft?.SutName ?? string.Empty;
        }

        private bool HasDraft() => Draft is not null;
        private bool CanSaveDraft() => HasDraft() && HasValidDraft;
    }
}
