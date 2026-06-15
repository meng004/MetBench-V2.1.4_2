using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MetBench_BLL.Coverage;
using MetBench_UI.Localization;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels
{
    /// <summary>
    /// 主页 Dashboard：注入只读的 CoverageService，导航进入时拉一次真实计数，
    /// 渲染 6 张 summary card（SUT / MR / MetaPattern / SUT×MR 绑定 / 已知缺陷 / 变异杀死率）。
    /// 只读、不写库（接 CoverageService 的 "dashboard 读、不写" 约定）。
    /// </summary>
    public sealed partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        private readonly CoverageService _coverage;

        public LocalizedTextProvider Localization { get; }

        [ObservableProperty]
        private ObservableCollection<DashboardMetric> _metrics = new();

        [ObservableProperty]
        private string? _errorMessage;

        public DashboardViewModel(CoverageService coverage, LocalizedTextProvider localization)
        {
            _coverage = coverage;
            Localization = localization;
        }

        // INavigationAware：导航进入时刷新（接 CLAUDE.md §7，Compute() 同步，无需 async void）。
        public void OnNavigatedTo()
        {
            try
            {
                Refresh();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Load error: {ex.Message}";
            }
        }

        public void OnNavigatedFrom()
        {
        }

        private void Refresh()
        {
            var r = _coverage.Compute();
            Metrics = new ObservableCollection<DashboardMetric>
            {
                new(Localization["Dashboard_Suts"], r.SutMr.TotalSuts.ToString(), "registered SUTs / applications"),
                new(Localization["Dashboard_Mrs"], r.SutMr.TotalMRs.ToString(), "metamorphic relations in catalog"),
                new(Localization["Dashboard_MetaPatterns"], $"{r.MetaPattern.CoveredMetaPatterns} / {r.MetaPattern.TotalMetaPatterns}", $"meta-patterns covered ({r.MetaPattern.Ratio:P0})"),
                new(Localization["Dashboard_Bindings"], $"{r.SutMr.BoundCells} / {r.SutMr.PossibleCells}", $"SUT×MR bindings ({r.SutMr.Density:P0} density)"),
                new(Localization["Dashboard_Bugs"], $"{r.Bug.ReproducedBugs} / {r.Bug.TotalKnownBugs}", $"known bugs reproduced ({r.Bug.Ratio:P0})"),
                new(Localization["Dashboard_Mutation"], $"{r.Mutation.DetectedMutants} / {r.Mutation.TotalMutants}", $"mutants killed ({r.Mutation.DetectionRate:P0})"),
            };
            ErrorMessage = null;
        }
    }

    /// <summary>Dashboard 单张 summary card 的只读视图模型（标题 + 大数值 + 说明）。</summary>
    public sealed class DashboardMetric
    {
        public DashboardMetric(string title, string value, string caption)
        {
            Title = title;
            Value = value;
            Caption = caption;
        }

        public string Title { get; }
        public string Value { get; }
        public string Caption { get; }
    }
}
