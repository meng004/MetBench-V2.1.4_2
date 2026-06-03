using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using LiveChartsCore.SkiaSharpView.WPF;
using MetBench_Client.ViewModels;

namespace MetBench_Client.Views.Pages;

public partial class SystemMtResultPage : Wpf.Ui.Controls.INavigableView<SystemMtResultViewModel>
{
    public SystemMtResultViewModel ViewModel { get; }

    public SystemMtResultPage(SystemMtResultViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
        BuildLayout();
    }

    private static Binding OneWay(string path) => new(path) { Mode = BindingMode.OneWay };

    private static Binding TwoWay(string path) => new(path) { Mode = BindingMode.TwoWay };

    private void BuildLayout()
    {
        var page = new Grid { Margin = new Thickness(16) };
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        page.Children.Add(BuildToolbar());

        var body = BuildBody();
        Grid.SetRow(body, 1);
        page.Children.Add(body);

        var status = BuildStatusBar();
        Grid.SetRow(status, 2);
        page.Children.Add(status);

        Root.Children.Add(page);
    }

    private FrameworkElement BuildToolbar()
    {
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var refresh = new System.Windows.Controls.Button
        {
            Content = "Refresh",
            Margin = new Thickness(0, 0, 16, 0),
            MinWidth = 88
        };
        AutomationProperties.SetAutomationId(refresh, "Button_RefreshSystemMtResults");
        refresh.SetBinding(System.Windows.Controls.Button.CommandProperty, OneWay("ViewModel.RefreshCommand"));
        toolbar.Children.Add(refresh);

        toolbar.Children.Add(new TextBlock
        {
            Text = "View mode:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });

        var binary = new RadioButton
        {
            Content = "Single run (Binary)",
            GroupName = "SystemMtResultViewMode",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        binary.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, TwoWay("ViewModel.IsBinaryView"));
        toolbar.Children.Add(binary);

        var historical = new RadioButton
        {
            Content = "Historical trend",
            GroupName = "SystemMtResultViewMode",
            VerticalAlignment = VerticalAlignment.Center
        };
        historical.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, TwoWay("ViewModel.IsHistoricalView"));
        historical.SetBinding(IsEnabledProperty, OneWay("ViewModel.CanShowHistoricalView"));
        toolbar.Children.Add(historical);

        return toolbar;
    }

    private FrameworkElement BuildBody()
    {
        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });

        var results = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            Margin = new Thickness(0, 0, 8, 0)
        };
        AutomationProperties.SetAutomationId(results, "DataGrid_SystemMtResults");
        results.SetBinding(ItemsControl.ItemsSourceProperty, OneWay("ViewModel.Records"));
        results.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, TwoWay("ViewModel.SelectedRecord"));
        results.Columns.Add(new DataGridTextColumn { Header = "MR", Binding = OneWay("MrName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        results.Columns.Add(new DataGridTextColumn { Header = "Assertion", Binding = OneWay("AssertionName"), Width = 120 });
        results.Columns.Add(new DataGridCheckBoxColumn { Header = "Passed", Binding = OneWay("Passed"), Width = 60 });
        results.Columns.Add(new DataGridTextColumn { Header = "Source", Binding = new Binding("SourceValue") { StringFormat = "G4" }, Width = 80 });
        results.Columns.Add(new DataGridTextColumn { Header = "Follow-up", Binding = new Binding("FollowUpValue") { StringFormat = "G4" }, Width = 80 });
        results.Columns.Add(new DataGridTextColumn { Header = "Run time", Binding = new Binding("RunAt") { StringFormat = "yyyy-MM-dd HH:mm:ss" }, Width = 140 });
        body.Children.Add(results);

        var chartHost = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8)
        };
        chartHost.SetResourceReference(Border.BorderBrushProperty, "ControlStrokeColorDefaultBrush");
        Grid.SetColumn(chartHost, 1);

        var chartGrid = new Grid();
        var chart = new CartesianChart { LegendPosition = LiveChartsCore.Measure.LegendPosition.Top };
        AutomationProperties.SetAutomationId(chart, "Chart_SystemMtResult");
        chart.SetBinding(CartesianChart.SeriesProperty, OneWay("ViewModel.ChartBinding.Series"));
        chart.SetBinding(CartesianChart.XAxesProperty, OneWay("ViewModel.ChartBinding.XAxes"));
        chart.SetBinding(CartesianChart.YAxesProperty, OneWay("ViewModel.ChartBinding.YAxes"));
        chart.SetBinding(VisibilityProperty, OneWay("ViewModel.ChartVisibility"));
        chartGrid.Children.Add(chart);

        var noData = new TextBlock
        {
            Text = "No data",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetAutomationId(noData, "TextBlock_SystemMtResultNoData");
        noData.SetResourceReference(ForegroundProperty, "TextFillColorDisabledBrush");
        noData.SetBinding(VisibilityProperty, OneWay("ViewModel.EmptyOverlayVisibility"));
        chartGrid.Children.Add(noData);

        chartHost.Child = chartGrid;
        body.Children.Add(chartHost);

        return body;
    }

    private FrameworkElement BuildStatusBar()
    {
        var statusHost = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 12, 0, 0)
        };
        statusHost.SetResourceReference(Border.BackgroundProperty, "SubtleFillColorTertiaryBrush");

        var statusGrid = new Grid();
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var statusText = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        statusText.SetBinding(TextBlock.TextProperty, OneWay("ViewModel.StatusMessage"));
        statusGrid.Children.Add(statusText);

        var busy = new ProgressBar
        {
            IsIndeterminate = true,
            Width = 80,
            Height = 16
        };
        busy.SetBinding(VisibilityProperty, OneWay("ViewModel.BusyVisibility"));
        Grid.SetColumn(busy, 1);
        statusGrid.Children.Add(busy);

        statusHost.Child = statusGrid;
        return statusHost;
    }
}
