using System;
using System.Windows;
using System.Windows.Controls;

namespace MetBench_Client.Controls;

public partial class SimplePagination : UserControl
{
    public static readonly DependencyProperty PageIndexProperty =
        DependencyProperty.Register(
            nameof(PageIndex), typeof(int), typeof(SimplePagination),
            new FrameworkPropertyMetadata(1,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPagingChanged));

    public static readonly DependencyProperty MaxPageCountProperty =
        DependencyProperty.Register(
            nameof(MaxPageCount), typeof(int), typeof(SimplePagination),
            new PropertyMetadata(1, OnPagingChanged));

    public static readonly RoutedEvent PageUpdatedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(PageUpdated), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(SimplePagination));

    public int PageIndex
    {
        get => (int)GetValue(PageIndexProperty);
        set => SetValue(PageIndexProperty, value);
    }

    public int MaxPageCount
    {
        get => (int)GetValue(MaxPageCountProperty);
        set => SetValue(MaxPageCountProperty, value);
    }

    public event RoutedEventHandler PageUpdated
    {
        add => AddHandler(PageUpdatedEvent, value);
        remove => RemoveHandler(PageUpdatedEvent, value);
    }

    public SimplePagination() => InitializeComponent();

    private static void OnPagingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SimplePagination)d).Refresh();

    private void Refresh()
    {
        if (PageLabel is null) return;
        int cur = Math.Max(1, PageIndex);
        int max = Math.Max(1, MaxPageCount);
        PageLabel.Text = $"{cur} / {max}";
        PrevBtn.IsEnabled = cur > 1;
        NextBtn.IsEnabled = cur < max;
    }

    private void PrevBtn_Click(object sender, RoutedEventArgs e)
    {
        if (PageIndex > 1)
        {
            PageIndex--;
            RaiseEvent(new RoutedEventArgs(PageUpdatedEvent, this));
        }
    }

    private void NextBtn_Click(object sender, RoutedEventArgs e)
    {
        if (PageIndex < MaxPageCount)
        {
            PageIndex++;
            RaiseEvent(new RoutedEventArgs(PageUpdatedEvent, this));
        }
    }
}
