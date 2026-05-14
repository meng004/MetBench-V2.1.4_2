using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Client.Services;
using MetBench_Domain;
using MetBench_IDAL;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    /// <summary>
    /// v2 P6 配套 — Anomaly Replay 结果对比页 ViewModel。
    /// 入口：AnomalyListPage 设置 ReplayInbox.PendingAnomaly 后 navigate 进来。
    /// </summary>
    /// <remarks>
    /// First-ship 范围：纯展示 + 一个 "Run replay" 占位按钮。
    /// 真实 Replay 需要从 Anomaly→Result→Execution→MRBinding 重建 PipelineContext，
    /// 这块跟 ISystemMtScenarioLauncher 集成留作 follow-up（见 §1.5 同期）。
    /// </remarks>
    public sealed partial class ReplayResultViewModel : ObservableObject, INavigationAware
    {
        private readonly ReplayInbox _inbox;
        private readonly IAnomalyRepository _anomalies;
        private readonly IResultRepository _results;

        public ReplayResultViewModel(
            ReplayInbox inbox,
            IAnomalyRepository anomalies,
            IResultRepository results)
        {
            _inbox = inbox;
            _anomalies = anomalies;
            _results = results;
        }

        // === Anomaly / 原 Result 展示字段 ===

        [ObservableProperty] private Anomaly? _anomaly;
        [ObservableProperty] private Result? _originalResult;

        [ObservableProperty] private string _mrCode = "—";
        [ObservableProperty] private string _sutName = "—";
        [ObservableProperty] private string _triggerParameters = "—";

        [ObservableProperty] private double? _originalSourceValue;
        [ObservableProperty] private double? _originalFollowupValue;
        [ObservableProperty] private string _originalFinalStatus = "—";
        [ObservableProperty] private bool _originalAssertionPassed;
        [ObservableProperty] private string _originalAssertionExpression = "—";

        // === Replay 字段 ===

        [ObservableProperty] private double? _replaySourceValue;
        [ObservableProperty] private double? _replayFollowupValue;
        [ObservableProperty] private string _replayFinalStatus = "—";
        [ObservableProperty] private bool _replayAssertionPassed;
        [ObservableProperty] private string _replayAssertionExpression = "—";

        [ObservableProperty] private ReplayClassification? _classification;
        [ObservableProperty] private bool _hasReplayResult;

        // === 状态 ===

        [ObservableProperty] private string? _errorMessage;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _isBusy;

        // 用于演示 6 种分类着色（first ship — 真实 ReplayService 集成是 follow-up）。
        [ObservableProperty] private ReplayClassification _demoClassification = ReplayClassification.Reproduced;

        public IReadOnlyList<ReplayClassification> AvailableClassifications { get; } = new[]
        {
            ReplayClassification.Reproduced,
            ReplayClassification.FixedOrFlaky,
            ReplayClassification.RegressionOnReplay,
            ReplayClassification.StillPassing,
            ReplayClassification.MismatchedFailure,
            ReplayClassification.NotComparable,
        };

        public void OnNavigatedTo()
        {
            try
            {
                LoadFromInbox();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public void OnNavigatedFrom() { }

        private void LoadFromInbox()
        {
            ErrorMessage = null;
            StatusMessage = null;

            Anomaly = _inbox.PendingAnomaly;
            if (Anomaly is null)
            {
                StatusMessage = "No anomaly selected. Open the Anomalies page, pick a row, and click \"Replay this anomaly\".";
                ResetReplay();
                return;
            }

            OriginalResult = _results.Get(Anomaly.ResultId);
            if (OriginalResult is null)
            {
                StatusMessage = $"No Result record found for Anomaly {Anomaly.IdAnomaly} (ResultId={Anomaly.ResultId}).";
                ResetReplay();
                return;
            }

            // Pipeline 元数据（MR/SUT/参数）在当前数据模型里没单独存到 Result/Anomaly，
            // 真实 PipelineContext 重建需要 Execution + MRBinding。这里只显示已有字段。
            MrCode = "—  (needs Execution + MRBinding lookup)";
            SutName = "—  (needs Execution + MRBinding lookup)";
            TriggerParameters = "—  (needs Execution + MRBinding lookup)";

            OriginalSourceValue = OriginalResult.SourceValue;
            OriginalFollowupValue = OriginalResult.FollowupValue;
            OriginalAssertionPassed = OriginalResult.AssertionPassed;
            OriginalAssertionExpression = string.IsNullOrEmpty(OriginalResult.AssertionExpression)
                ? "—" : OriginalResult.AssertionExpression;
            OriginalFinalStatus = OriginalResult.AssertionPassed ? PipelineStatus.Ok : PipelineStatus.Anomaly;

            // 如果 Inbox 里有真实 ReplayResult — 直接渲染
            if (_inbox.LastResult is { } rr)
            {
                ApplyReplayResult(rr);
            }
            else
            {
                ResetReplay();
                StatusMessage = "Replay has not been run yet. Use \"Simulate replay\" to preview the classification UI; real Replay wiring is tracked as a follow-up.";
            }
        }

        private void ResetReplay()
        {
            HasReplayResult = false;
            Classification = null;
            ReplaySourceValue = null;
            ReplayFollowupValue = null;
            ReplayFinalStatus = "—";
            ReplayAssertionPassed = false;
            ReplayAssertionExpression = "—";
        }

        private void ApplyReplayResult(ReplayResult rr)
        {
            HasReplayResult = true;
            Classification = rr.Classification;
            ReplaySourceValue = rr.ReplayOutcome.SourceMetrics is { } sm && OriginalResult is { } &&
                                sm.TryGetValue("k_eff", out var sv) ? sv : null;
            ReplayFollowupValue = rr.ReplayOutcome.FollowupMetrics is { } fm &&
                                  fm.TryGetValue("k_eff", out var fv) ? fv : null;
            ReplayFinalStatus = rr.ReplayOutcome.FinalStatus;
            ReplayAssertionPassed = rr.ReplayOutcome.AssertionResult?.Passed ?? false;
            ReplayAssertionExpression = rr.ReplayOutcome.AssertionResult?.Expression ?? "—";
        }

        /// <summary>
        /// First-ship 占位 — 真实 Replay 需要 PipelineContext 重建。
        /// 现阶段用所选 DemoClassification 合成一个 ReplayResult 仅用于演示 UI 6 种着色。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSimulate))]
        private async Task SimulateReplayAsync()
        {
            if (Anomaly is null || OriginalResult is null) return;
            IsBusy = true;
            try
            {
                await Task.Delay(150).ConfigureAwait(true); // 模拟跑一下

                // 根据 DemoClassification 反推一个 "replay outcome" 让 UI 显示一致
                var (replayStatus, replayPassed, replayFollowup) = DemoClassification switch
                {
                    ReplayClassification.Reproduced =>
                        (PipelineStatus.Anomaly, false, OriginalResult.FollowupValue ?? 0.0),
                    ReplayClassification.FixedOrFlaky =>
                        (PipelineStatus.Ok, true, (OriginalResult.SourceValue ?? 0.0) * 1.0),
                    ReplayClassification.RegressionOnReplay =>
                        (PipelineStatus.Anomaly, false, (OriginalResult.FollowupValue ?? 0.0) * 0.5),
                    ReplayClassification.StillPassing =>
                        (PipelineStatus.Ok, true, OriginalResult.FollowupValue ?? 0.0),
                    ReplayClassification.MismatchedFailure =>
                        (PipelineStatus.Anomaly, false, (OriginalResult.FollowupValue ?? 0.0) * 0.7),
                    ReplayClassification.NotComparable =>
                        (PipelineStatus.Error, false, 0.0),
                    _ => (PipelineStatus.Ok, true, 0.0)
                };

                HasReplayResult = true;
                Classification = DemoClassification;
                ReplaySourceValue = OriginalResult.SourceValue;
                ReplayFollowupValue = replayFollowup;
                ReplayFinalStatus = replayStatus;
                ReplayAssertionPassed = replayPassed;
                ReplayAssertionExpression = $"simulated → {DemoClassification}";
                StatusMessage = "Simulated only — wire ReplayService.ReplayAsync (PipelineContext reconstruction needed) for real replay.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSimulate() => !IsBusy && Anomaly is not null && OriginalResult is not null;

        partial void OnAnomalyChanged(Anomaly? value) => SimulateReplayCommand.NotifyCanExecuteChanged();
        partial void OnOriginalResultChanged(Result? value) => SimulateReplayCommand.NotifyCanExecuteChanged();
        partial void OnIsBusyChanged(bool value) => SimulateReplayCommand.NotifyCanExecuteChanged();
    }
}
