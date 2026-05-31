using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetBench_BLL.SystemMT.Assertions;
using MetBench_BLL.SystemMT.Pipeline;
using MetBench_Client.Services;
using MetBench_Domain;
using MetBench_IDAL;
using MetBench_UI.Localization;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    /// <summary>
    /// v2 P6 配套 — Anomaly Replay 结果对比页 ViewModel。
    /// 入口：AnomalyListPage 设置 ReplayInbox.PendingAnomaly 后 navigate 进来。
    /// </summary>
    /// <remarks>
    /// 两条路径：
    ///   1) <see cref="SimulateReplayAsync"/> — 用 DemoClassification 合成 ReplayResult 仅演示 UI 6 种着色。
    ///   2) <see cref="RunRealReplayAsync"/> — 走 cloud-ship 的 ReplayContextBuilder + ReplayService.ReplayAsync
    ///      真实重跑 Pipeline，比较新旧 outcome 并打分类标签。
    /// </remarks>
    public sealed partial class ReplayResultViewModel : ObservableObject, INavigationAware
    {
        private readonly ReplayInbox _inbox;
        private readonly IAnomalyRepository _anomalies;
        private readonly IResultRepository _results;
        private readonly ReplayContextBuilder _builder;
        private readonly ReplayService _replayService;

        public LocalizedTextProvider Localization { get; }

        public ReplayResultViewModel(
            ReplayInbox inbox,
            IAnomalyRepository anomalies,
            IResultRepository results,
            ReplayContextBuilder builder,
            ReplayService replayService,
            LocalizedTextProvider localization)
        {
            _inbox = inbox;
            _anomalies = anomalies;
            _results = results;
            _builder = builder;
            _replayService = replayService;
            Localization = localization;
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

        // 用于演示 6 种分类着色 — SimulateReplay 路径专用，RunRealReplay 不依赖。
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
                StatusMessage = Localization["Status_Replay_NoAnomalySelected"];
                ResetReplay();
                return;
            }

            OriginalResult = _results.Get(Anomaly.ResultId);
            if (OriginalResult is null)
            {
                StatusMessage = string.Format(Localization["Status_Replay_NoResultRecord_Fmt"], Anomaly.IdAnomaly, Anomaly.ResultId);
                ResetReplay();
                return;
            }

            // Pipeline 元数据（MR/SUT/参数）由 RunRealReplay 调 ReplayContextBuilder 解析后填入。
            // 进页时先显示占位，提示用户用按钮拉真实上下文。
            MrCode = Localization["Hint_Replay_MrCodePlaceholder"];
            SutName = Localization["Hint_Replay_MrCodePlaceholder"];
            TriggerParameters = Localization["Hint_Replay_MrCodePlaceholder"];

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
                StatusMessage = Localization["Status_Replay_PickAnAction"];
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
            ReplaySourceValue = rr.ReplayOutcome.SourceMetrics is { } sm &&
                                sm.TryGetValue("k_eff", out var sv) ? sv : null;
            ReplayFollowupValue = rr.ReplayOutcome.FollowupMetrics is { } fm &&
                                  fm.TryGetValue("k_eff", out var fv) ? fv : null;
            ReplayFinalStatus = rr.ReplayOutcome.FinalStatus;
            ReplayAssertionPassed = rr.ReplayOutcome.AssertionResult?.Passed ?? false;
            ReplayAssertionExpression = rr.ReplayOutcome.AssertionResult?.Expression ?? "—";
        }

        /// <summary>
        /// 真实 Replay — 调 ReplayContextBuilder.Build + ReplayService.ReplayAsync。
        /// 失败时 ErrorMessage 显示原因；UI 不崩。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRunRealReplay))]
        private async Task RunRealReplayAsync()
        {
            if (Anomaly is null || OriginalResult is null) return;
            IsBusy = true;
            ErrorMessage = null;
            try
            {
                var workingDir = Path.Combine(
                    Path.GetTempPath(), "MetBench", "replay", Anomaly.IdAnomaly.ToString("N"));
                Directory.CreateDirectory(workingDir);

                var built = _builder.Build(Anomaly.IdAnomaly, workingDir);
                var original = ReconstructOutcomeFromResult(OriginalResult);
                var replay = await _replayService.ReplayAsync(built.Context, original).ConfigureAwait(true);

                MrCode = string.IsNullOrWhiteSpace(built.MrCode) ? "—" : built.MrCode;
                SutName = string.IsNullOrWhiteSpace(built.SutName) ? "—" : built.SutName;
                TriggerParameters = built.TriggerParameters.Count == 0
                    ? Localization["Hint_Replay_NoTriggerParameters"]
                    : string.Join(", ", built.TriggerParameters.Select(kv => $"{kv.Key}={kv.Value}"));

                ApplyReplayResult(replay);
                StatusMessage = string.Format(Localization["Status_Replay_RealReplayCompleted_Fmt"], replay.Classification);
            }
            catch (Exception ex)
            {
                ErrorMessage = string.Format(Localization["Status_Replay_RealReplayFailed_Fmt"], ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanRunRealReplay() => !IsBusy && Anomaly is not null && OriginalResult is not null;

        /// <summary>
        /// 把持久化的 v2 Result 字段直填回 Pipeline 跑过的 PipelineOutcome 形态，
        /// 供 ReplayService.Classify 跟新 outcome 比较。
        /// 路径信息（ArtifactsDirectory / *InputPath / *OutputPath）原 Result 没存 —— 留空字符串。
        /// </summary>
        private static PipelineOutcome ReconstructOutcomeFromResult(Result r)
        {
            var assertion = new SystemMtAssertionResultV2(
                AssertionTypeCode: "v2-original",
                Passed: r.AssertionPassed,
                SourceValue: r.SourceValue,
                FollowupValue: r.FollowupValue,
                ObservedDelta: r.ObservedDelta,
                ExpectedThreshold: r.ExpectedThreshold,
                Expression: r.AssertionExpression ?? string.Empty,
                FailureReason: r.FailureReason);

            return new PipelineOutcome(
                FinalStatus: r.AssertionPassed ? PipelineStatus.Ok : PipelineStatus.Anomaly,
                ErrorMessage: r.FailureReason,
                StartedAt: DateTime.UtcNow,
                FinishedAt: DateTime.UtcNow,
                ArtifactsDirectory: string.Empty,
                SourceInputPath: string.Empty,
                FollowupInputPath: string.Empty,
                SourceOutputPath: string.Empty,
                FollowupOutputPath: string.Empty,
                SourceMetrics: r.SourceMetrics,
                FollowupMetrics: r.FollowupMetrics,
                AssertionResult: assertion,
                SourceElapsed: r.SourceElapsed,
                FollowupElapsed: r.FollowupElapsed,
                SourceExitCode: r.SourceExitCode,
                FollowupExitCode: r.FollowupExitCode);
        }

        /// <summary>
        /// First-ship 占位 — 真实 Replay 走 RunRealReplayAsync。
        /// 这里仅用所选 DemoClassification 合成一个 ReplayResult 让 UI 6 种着色可演示。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSimulate))]
        private async Task SimulateReplayAsync()
        {
            if (Anomaly is null || OriginalResult is null) return;
            IsBusy = true;
            try
            {
                await Task.Delay(150).ConfigureAwait(true); // 模拟跑一下

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
                ReplayAssertionExpression = string.Format(Localization["Status_Replay_SimulatedExpression_Fmt"], DemoClassification);
                StatusMessage = Localization["Status_Replay_SimulatedOnly"];
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSimulate() => !IsBusy && Anomaly is not null && OriginalResult is not null;

        partial void OnAnomalyChanged(Anomaly? value)
        {
            SimulateReplayCommand.NotifyCanExecuteChanged();
            RunRealReplayCommand.NotifyCanExecuteChanged();
        }
        partial void OnOriginalResultChanged(Result? value)
        {
            SimulateReplayCommand.NotifyCanExecuteChanged();
            RunRealReplayCommand.NotifyCanExecuteChanged();
        }
        partial void OnIsBusyChanged(bool value)
        {
            SimulateReplayCommand.NotifyCanExecuteChanged();
            RunRealReplayCommand.NotifyCanExecuteChanged();
        }
    }
}
