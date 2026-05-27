using System;
using System.Globalization;
using System.Windows.Data;

namespace MetBench_Client.ViewModels;

/// <summary>
/// One-shot inverse-boolean converter used by SystemMtResultPage's "Binary" radio
/// to bind to the inverse of <c>ViewModel.IsHistoricalView</c>. Reusing the
/// inverse keeps the ViewModel surface single-source-of-truth (the bool toggles
/// between two view modes; we don't expose two redundant properties).
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : value;
}
