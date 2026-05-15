using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MetBench_BLL.Coverage;
using SkiaSharp;
using Wpf.Ui.Controls;

namespace MetBench_Client.ViewModels;

public sealed partial class CoverageDashboardViewModel : ObservableObject, INavigationAware
{
    private readonly CoverageService _service;

    [ObservableProperty]
    private IEnumerable<ISeries> _metaPatternSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private IEnumerable<ISeries> _sutMrSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private IEnumerable<ISeries> _bugSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private IEnumerable<ISeries> _mutationSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private string _metaPatternLabel = string.Empty;

    [ObservableProperty]
    private string _sutMrLabel = string.Empty;

    [ObservableProperty]
    private string _bugLabel = string.Empty;

    [ObservableProperty]
    private string _mutationLabel = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public CoverageDashboardViewModel(CoverageService service)
    {
        _service = service;
    }

    public void OnNavigatedTo()
    {
        try { Refresh(); }
        catch (Exception ex) { ErrorMessage = $"Load error: {ex.Message}"; }
    }

    public void OnNavigatedFrom() { }

    private void Refresh()
    {
        var report = _service.Compute();
        BuildMetaPattern(report.MetaPattern);
        BuildSutMr(report.SutMr);
        BuildBug(report.Bug);
        BuildMutation(report.Mutation);
    }

    private void BuildMetaPattern(MetaPatternCoverage cov)
    {
        var covered = Math.Max(cov.CoveredMetaPatterns, 0);
        var uncovered = Math.Max(cov.TotalMetaPatterns - cov.CoveredMetaPatterns, 0);
        MetaPatternSeries = MakePie(covered, uncovered);
        MetaPatternLabel = $"{covered} / {cov.TotalMetaPatterns} patterns  ({cov.Ratio:P0})";
    }

    private void BuildSutMr(SutMrCoverage cov)
    {
        var bound = Math.Max(cov.BoundCells, 0);
        var unbound = Math.Max(cov.PossibleCells - cov.BoundCells, 0);
        SutMrSeries = MakePie(bound, unbound);
        SutMrLabel = $"{bound} / {cov.PossibleCells} cells  ({cov.Density:P0})";
    }

    private void BuildBug(BugCoverage cov)
    {
        var repro = Math.Max(cov.ReproducedBugs, 0);
        var notRepro = Math.Max(cov.TotalKnownBugs - cov.ReproducedBugs, 0);
        BugSeries = MakePie(repro, notRepro);
        BugLabel = $"{repro} / {cov.TotalKnownBugs} bugs reproduced  ({cov.Ratio:P0})";
    }

    private void BuildMutation(MutationCoverage cov)
    {
        var detected = Math.Max(cov.DetectedMutants, 0);
        var missed = Math.Max(cov.MissedMutants, 0);
        MutationSeries = MakePie(detected, missed);
        MutationLabel = $"{detected} / {cov.TotalMutants} mutants detected  ({cov.DetectionRate:P0})";
    }

    private static IEnumerable<ISeries> MakePie(int covered, int uncovered)
    {
        var series = new List<ISeries>();

        if (covered > 0)
        {
            series.Add(new PieSeries<int>
            {
                Values = new[] { covered },
                Name = "Covered",
                Fill = new SolidColorPaint(new SKColor(39, 174, 96)),
            });
        }

        if (uncovered > 0)
        {
            series.Add(new PieSeries<int>
            {
                Values = new[] { uncovered },
                Name = "Uncovered",
                Fill = new SolidColorPaint(new SKColor(231, 76, 60)),
            });
        }

        if (covered == 0 && uncovered == 0)
        {
            series.Add(new PieSeries<int>
            {
                Values = new[] { 1 },
                Name = "No data",
                Fill = new SolidColorPaint(SKColors.LightGray),
            });
        }

        return series;
    }
}
