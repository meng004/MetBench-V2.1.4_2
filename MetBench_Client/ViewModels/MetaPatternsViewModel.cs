using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.Discovery;
using MetBench_BLL.Paging;
using MetBench_Domain;
using MetBench_IDAL;
using MetBench_Client.Services;
using MetBench_UI.Localization;

namespace MetBench_Client.ViewModels
{
    /// <summary>
    /// F21 — MetaPatterns CRUD ViewModel。
    /// 8 个 NOETHER MetaPattern 默认通过 <see cref="MetaPatternSeed.EnsureSeeded"/> 注入。
    /// </summary>
    /// <remarks>
    /// 操作四件套：
    ///   • Add — 新 MetaPattern（用户输入 Code 唯一）
    ///   • Edit — 改字段（不许改 Code，避免破坏 MetamorphicRelation 弱外键引用）
    ///   • ToggleStatus — active ↔ deprecated（experimental / out-of-scope 不参与切换）
    ///   • Delete (soft) — Status 直接置为 "deprecated"（绝不调 repo.Remove，避免 MR 引用悬挂）
    ///
    /// 所有写动作都落 AuditLog (Actor="wpf-user")。
    /// </remarks>
    public sealed partial class MetaPatternsViewModel
        : PagingViewModel<MetaPattern>, INavigationAware
    {
        private readonly IMetaPatternRepository _repo;
        private readonly IAuditLogRepository _audit;

        [ObservableProperty] private MetaPattern? _selectedPattern;
        [ObservableProperty] private MetaPattern? _editingPattern;
        [ObservableProperty] private bool _isAdding;
        [ObservableProperty] private string? _errorMessage;
        [ObservableProperty] private string? _statusMessage;

        public LocalizedTextProvider Localization { get; }

        public MetaPatternsViewModel(IMetaPatternRepository repo, IAuditLogRepository audit, LocalizedTextProvider localization)
        {
            _repo = repo;
            _audit = audit;
            Localization = localization;
            PageSize = 5;  // 8 seeded rows → 2 pages, makes PagingBar non-trivial
        }

        public async void OnNavigatedTo()
        {
            try
            {
                var inserted = MetaPatternSeed.EnsureSeeded(_repo);
                if (inserted > 0)
                {
                    StatusMessage = string.Format(Localization["Status_MetaPattern_Seeded_Fmt"], inserted);
                }
                await LoadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public void OnNavigatedFrom() { }

        protected override Task<PagedResult<MetaPattern>> LoadPageAsync(
            PageRequest req, CancellationToken ct)
        {
            try
            {
                ErrorMessage = null;
                var all = _repo.GetAll()
                    .OrderBy(mp => mp.Block, StringComparer.Ordinal)
                    .ThenBy(mp => mp.Code, StringComparer.Ordinal)
                    .ToList();
                var slice = all.Skip(req.Skip).Take(req.PageSize).ToList();
                return Task.FromResult(new PagedResult<MetaPattern>(
                    slice, all.Count, req.PageIndex, req.PageSize));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Task.FromResult(PagedResult<MetaPattern>.Empty(req.PageIndex, req.PageSize));
            }
        }

        // ===== 命令 =====

        [RelayCommand]
        private void BeginAdd()
        {
            EditingPattern = new MetaPattern
            {
                Code = string.Empty,
                Name = string.Empty,
                Block = "B1-symmetry",
                DefaultAssertionTypeCode = "approx-invariant",
                DefaultToleranceRel = 0.0001,
                NoiseAware = false,
                HypothesisTemplate = string.Empty,
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "wpf-user",
            };
            IsAdding = true;
            ErrorMessage = null;
            StatusMessage = Localization["Status_MetaPattern_BeginAdd"];
        }

        [RelayCommand(CanExecute = nameof(CanBeginEdit))]
        private void BeginEdit()
        {
            if (SelectedPattern is null) return;
            EditingPattern = CloneFor(SelectedPattern);
            IsAdding = false;
            ErrorMessage = null;
            StatusMessage = string.Format(Localization["Status_MetaPattern_BeginEdit_Fmt"], SelectedPattern.Code);
        }

        private bool CanBeginEdit() => SelectedPattern is not null && EditingPattern is null;

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            if (EditingPattern is null) return;
            try
            {
                if (IsAdding)
                {
                    if (string.IsNullOrWhiteSpace(EditingPattern.Code))
                    {
                        ErrorMessage = Localization["Error_MetaPattern_CodeRequired"];
                        return;
                    }
                    if (_repo.Get(EditingPattern.Code) is not null)
                    {
                        ErrorMessage = string.Format(Localization["Error_MetaPattern_AlreadyExists_Fmt"], EditingPattern.Code);
                        return;
                    }
                    _repo.Add(EditingPattern);
                    WriteAudit("catalog.update", EditingPattern.Code,
                        op: "add", reason: "wpf-user-add");
                    StatusMessage = string.Format(Localization["Status_MetaPattern_Added_Fmt"], EditingPattern.Code);
                }
                else
                {
                    var ok = _repo.Modify(EditingPattern);
                    if (!ok)
                    {
                        ErrorMessage = string.Format(Localization["Error_MetaPattern_ModifyReturnedFalse_Fmt"], EditingPattern.Code);
                        return;
                    }
                    WriteAudit("catalog.update", EditingPattern.Code,
                        op: "edit", reason: "wpf-user-edit");
                    StatusMessage = string.Format(Localization["Status_MetaPattern_Saved_Fmt"], EditingPattern.Code);
                }

                EditingPattern = null;
                IsAdding = false;
                ErrorMessage = null;
                await LoadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private bool CanSave() => EditingPattern is not null && !IsLoading;

        [RelayCommand]
        private void Cancel()
        {
            EditingPattern = null;
            IsAdding = false;
            ErrorMessage = null;
            StatusMessage = null;
        }

        [RelayCommand(CanExecute = nameof(CanToggleOrDelete))]
        private async Task ToggleStatusAsync()
        {
            if (SelectedPattern is null) return;
            try
            {
                var pattern = _repo.Get(SelectedPattern.Code);
                if (pattern is null)
                {
                    ErrorMessage = string.Format(Localization["Error_MetaPattern_NotFound_Fmt"], SelectedPattern.Code);
                    return;
                }

                var oldStatus = pattern.Status;
                pattern.Status = pattern.Status == "active" ? "deprecated" : "active";
                _repo.Modify(pattern);
                WriteAudit("config.change", pattern.Code,
                    op: "toggle-status", reason: $"{oldStatus}→{pattern.Status}");
                StatusMessage = string.Format(Localization["Status_MetaPattern_StatusToggled_Fmt"], pattern.Code, oldStatus, pattern.Status);
                ErrorMessage = null;
                await LoadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        [RelayCommand(CanExecute = nameof(CanToggleOrDelete))]
        private async Task SoftDeleteAsync()
        {
            if (SelectedPattern is null) return;
            try
            {
                var pattern = _repo.Get(SelectedPattern.Code);
                if (pattern is null)
                {
                    ErrorMessage = string.Format(Localization["Error_MetaPattern_NotFound_Fmt"], SelectedPattern.Code);
                    return;
                }

                if (pattern.Status == "deprecated")
                {
                    StatusMessage = string.Format(Localization["Status_MetaPattern_AlreadyDeprecated_Fmt"], pattern.Code);
                    return;
                }

                var oldStatus = pattern.Status;
                pattern.Status = "deprecated";
                _repo.Modify(pattern);
                WriteAudit("catalog.update", pattern.Code,
                    op: "soft-delete", reason: $"{oldStatus}→deprecated");
                StatusMessage = string.Format(Localization["Status_MetaPattern_SoftDeleted_Fmt"], pattern.Code, oldStatus);
                ErrorMessage = null;
                await LoadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private bool CanToggleOrDelete() =>
            !IsLoading && SelectedPattern is not null && EditingPattern is null;

        // ===== helpers =====

        private static MetaPattern CloneFor(MetaPattern source) => new()
        {
            Code = source.Code,
            Name = source.Name,
            Block = source.Block,
            DefaultAssertionTypeCode = source.DefaultAssertionTypeCode,
            DefaultToleranceRel = source.DefaultToleranceRel,
            NoiseAware = source.NoiseAware,
            HypothesisTemplate = source.HypothesisTemplate,
            ExampleParamHints = source.ExampleParamHints.ToList(),
            Status = source.Status,
            OutOfScopeReason = source.OutOfScopeReason,
            CreatedAt = source.CreatedAt,
            CreatedBy = source.CreatedBy,
        };

        private void WriteAudit(string action, string code, string op, string reason)
        {
            try
            {
                var details = JsonSerializer.Serialize(new { op, reason, code });
                _audit.Add(new AuditLog
                {
                    IdLog = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Actor = "wpf-user",
                    Action = action,
                    TargetEntityType = "MetaPattern",
                    TargetEntityId = code,
                    DetailsJson = details,
                });
            }
            catch
            {
                // Audit failure must not block the user-visible CRUD outcome.
            }
        }

        partial void OnSelectedPatternChanged(MetaPattern? value)
        {
            BeginEditCommand.NotifyCanExecuteChanged();
            ToggleStatusCommand.NotifyCanExecuteChanged();
            SoftDeleteCommand.NotifyCanExecuteChanged();
        }

        partial void OnEditingPatternChanged(MetaPattern? value)
        {
            BeginEditCommand.NotifyCanExecuteChanged();
            SaveCommand.NotifyCanExecuteChanged();
            ToggleStatusCommand.NotifyCanExecuteChanged();
            SoftDeleteCommand.NotifyCanExecuteChanged();
        }
    }
}
