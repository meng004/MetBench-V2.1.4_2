using System;
using System.Globalization;
using System.Windows.Data;
using MetBench_Domain;

namespace MetBench_Client.Converters;

/// <summary>DataGrid 显示 AnomalyStatus 为 kebab 字符串（enum→kebab）。</summary>
public sealed class AnomalyStatusKebabConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is AnomalyStatus s ? s.ToKebab() : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string k && AnomalyStatuses.TryParseKebab(k, out var s) ? s : AnomalyStatus.Unspecified;
}
