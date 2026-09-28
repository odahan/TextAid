using System.Globalization;
using System.Windows.Data;

namespace TextAid.App.Converters;

/// <summary>Inverts a Boolean binding for controls that must be disabled while work is active.</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool flag && !flag;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool flag && !flag;
}
