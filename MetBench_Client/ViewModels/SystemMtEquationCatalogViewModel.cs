using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Metadata.Editing;
using MetBench_UI.Localization;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    public sealed partial class SystemMtEquationCatalogViewModel : ObservableObject, INavigationAware
    {
        private readonly ISystemMtEquationEditor _editor;
        private bool _isInitialized;

        public LocalizedTextProvider Localization { get; }

        [ObservableProperty]
        private ObservableCollection<EquationListItem> _equations = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateEquationDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveEquationDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteEquationCommand))]
        private EquationListItem? _selectedEquation;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateEquationDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveEquationDraftCommand))]
        private EquationMetadataDraft? _draft;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasValidDraft;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateEquationDraftCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveEquationDraftCommand))]
        private bool _isDraftReadOnly = true;

        public SystemMtEquationCatalogViewModel(ISystemMtEquationEditor editor, LocalizedTextProvider localization)
        {
            _editor = editor;
            Localization = localization;
            StatusMessage = Localization["Status_Equation_InitialPrompt"];
        }

        public async void OnNavigatedTo()
        {
            if (_isInitialized) return;
            await LoadEquationsAsync().ConfigureAwait(false);
            _isInitialized = true;
        }

        public void OnNavigatedFrom() { }

        partial void OnSelectedEquationChanged(EquationListItem? value)
        {
            HasValidDraft = false;
            if (value is null)
            {
                Draft = null;
                IsDraftReadOnly = true;
                return;
            }

            // Built-in rows are read-only — populate the form for inspection only.
            if (string.Equals(value.Source, EquationSourceKinds.BuiltIn, StringComparison.Ordinal))
            {
                Draft = new EquationMetadataDraft
                {
                    EquationKey = value.EquationKey,
                    Name = value.Name,
                    CanonicalForm = value.CanonicalForm,
                };
                IsDraftReadOnly = true;
                StatusMessage = string.Format(Localization["Status_Equation_LoadedBuiltInReadOnly_Fmt"], value.EquationKey);
                return;
            }

            _ = LoadUserDraftAsync(value.EquationKey);
        }

        partial void OnDraftChanged(EquationMetadataDraft? value)
        {
            HasValidDraft = false;
        }

        partial void OnHasValidDraftChanged(bool value)
        {
            SaveEquationDraftCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private async Task RefreshEquationsAsync()
        {
            await LoadEquationsAsync().ConfigureAwait(false);
        }

        [RelayCommand]
        private void NewEquationDraft()
        {
            SelectedEquation = null;
            Draft = new EquationMetadataDraft();
            IsDraftReadOnly = false;
            HasValidDraft = false;
            StatusMessage = Localization["Status_Equation_NewDraft"];
        }

        [RelayCommand(CanExecute = nameof(CanValidateDraft))]
        private void ValidateEquationDraft()
        {
            if (Draft is null) return;
            var result = _editor.ValidateDraft(Draft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? Localization["Status_Equation_DraftValid"]
                : string.Format(Localization["Status_Equation_ValidationFailed_Fmt"], string.Join(" | ", result.Errors));
        }

        [RelayCommand(CanExecute = nameof(CanSaveDraft))]
        private async Task SaveEquationDraftAsync()
        {
            if (Draft is null) return;
            var result = await _editor.SaveDraftAsync(Draft).ConfigureAwait(false);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? string.Format(Localization["Status_Equation_Saved_Fmt"], Draft.EquationKey)
                : string.Format(Localization["Status_Equation_SaveBlocked_Fmt"], string.Join(" | ", result.Errors));

            if (result.Success)
                await LoadEquationsAsync(reselectKey: Draft.EquationKey).ConfigureAwait(false);
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
        private async Task DeleteEquationAsync()
        {
            if (SelectedEquation is null) return;
            var key = SelectedEquation.EquationKey;
            var result = await _editor.DeleteAsync(key).ConfigureAwait(false);
            StatusMessage = result.Success
                ? string.Format(Localization["Status_Equation_Deleted_Fmt"], key)
                : string.Format(Localization["Status_Equation_DeleteBlocked_Fmt"], string.Join(" | ", result.Errors));

            if (result.Success)
                await LoadEquationsAsync().ConfigureAwait(false);
        }

        private async Task LoadEquationsAsync(string? reselectKey = null)
        {
            try
            {
                var items = await _editor.ListEquationsAsync().ConfigureAwait(false);
                Equations = new ObservableCollection<EquationListItem>(items);
                StatusMessage = string.Format(Localization["Status_Equation_Loaded_Fmt"], Equations.Count, Equations.Count(e => e.Source == EquationSourceKinds.User));
                if (!string.IsNullOrWhiteSpace(reselectKey))
                {
                    SelectedEquation = Equations.FirstOrDefault(e =>
                        string.Equals(e.EquationKey, reselectKey, StringComparison.Ordinal));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Localization["Status_Equation_ErrorListing_Fmt"], ex.Message);
            }
        }

        private async Task LoadUserDraftAsync(string equationKey)
        {
            try
            {
                var draft = await _editor.LoadAsync(equationKey).ConfigureAwait(false);
                Draft = draft;
                IsDraftReadOnly = draft is null;
                StatusMessage = draft is not null
                    ? string.Format(Localization["Status_Equation_LoadedUser_Fmt"], equationKey)
                    : string.Format(Localization["Status_Equation_UserNotFound_Fmt"], equationKey);
            }
            catch (Exception ex)
            {
                Draft = null;
                IsDraftReadOnly = true;
                StatusMessage = string.Format(Localization["Status_Equation_ErrorLoading_Fmt"], equationKey, ex.Message);
            }
        }

        private bool CanValidateDraft() => Draft is not null && !IsDraftReadOnly;
        private bool CanSaveDraft() => CanValidateDraft() && HasValidDraft;
        private bool CanDeleteSelected() =>
            SelectedEquation is not null
            && string.Equals(SelectedEquation.Source, EquationSourceKinds.User, StringComparison.Ordinal);
    }
}
