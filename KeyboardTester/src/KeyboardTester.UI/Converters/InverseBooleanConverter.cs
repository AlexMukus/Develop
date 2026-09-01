using System.Globalization;
using System.Windows.Data;

namespace KeyboardTester.UI.Converters;

/// <summary>
/// Конвертер логической инверсии (bool ↔ bool).
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value ?? false;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value ?? false;
    }
}
