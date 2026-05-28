using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Metadata.Editing;
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
        private string _statusMessage = "Select an equation row or click \"New equation\".";

        [ObservableProperty]
        private bool _hasValidDraft;

        [ObservableProperty]
        private bool _isDraftReadOnly = true;

        public SystemMtEquationCatalogViewModel(ISystemMtEquationEditor editor)
        {
            _editor = editor;
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
                StatusMessage = $"Loaded '{value.EquationKey}' (Built-in, read-only).";
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
            StatusMessage = "New user-defined equation draft. Fill in the fields, then Validate / Save.";
        }

        [RelayCommand(CanExecute = nameof(CanValidateDraft))]
        private void ValidateEquationDraft()
        {
            if (Draft is null) return;
            var result = _editor.ValidateDraft(Draft);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? "Equation draft is valid."
                : "Validation failed: " + string.Join(" | ", result.Errors);
        }

        [RelayCommand(CanExecute = nameof(CanSaveDraft))]
        private async Task SaveEquationDraftAsync()
        {
            if (Draft is null) return;
            var result = await _editor.SaveDraftAsync(Draft).ConfigureAwait(false);
            HasValidDraft = result.Success;
            StatusMessage = result.Success
                ? $"Equation '{Draft.EquationKey}' saved."
                : "Save blocked: " + string.Join(" | ", result.Errors);

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
                ? $"Equation '{key}' deleted."
                : "Delete blocked: " + string.Join(" | ", result.Errors);

            if (result.Success)
                await LoadEquationsAsync().ConfigureAwait(false);
        }

        private async Task LoadEquationsAsync(string? reselectKey = null)
        {
            try
            {
                var items = await _editor.ListEquationsAsync().ConfigureAwait(false);
                Equations = new ObservableCollection<EquationListItem>(items);
                StatusMessage = $"Loaded {Equations.Count} equation(s) ({Equations.Count(e => e.Source == EquationSourceKinds.User)} user-defined).";
                if (!string.IsNullOrWhiteSpace(reselectKey))
                {
                    SelectedEquation = Equations.FirstOrDefault(e =>
                        string.Equals(e.EquationKey, reselectKey, StringComparison.Ordinal));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR listing equations: {ex.Message}";
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
                    ? $"Loaded user equation '{equationKey}'. Edit and Validate / Save."
                    : $"User equation '{equationKey}' not found.";
            }
            catch (Exception ex)
            {
                Draft = null;
                IsDraftReadOnly = true;
                StatusMessage = $"ERROR loading '{equationKey}': {ex.Message}";
            }
        }

        private bool CanValidateDraft() => Draft is not null && !IsDraftReadOnly;
        private bool CanSaveDraft() => CanValidateDraft() && HasValidDraft;
        private bool CanDeleteSelected() =>
            SelectedEquation is not null
            && string.Equals(SelectedEquation.Source, EquationSourceKinds.User, StringComparison.Ordinal);
    }
}
