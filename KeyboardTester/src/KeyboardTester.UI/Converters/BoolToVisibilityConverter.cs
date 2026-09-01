using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KeyboardTester.UI.Converters;

/// <summary>
/// Конвертер bool → <see cref="Visibility"/> с поддержкой инверсии
/// через ConverterParameter="Invert".
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is bool b && b;
        if (IsInverted(parameter))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is Visibility.Visible;
        if (IsInverted(parameter))
        {
            flag = !flag;
        }

        return flag;
    }

    private static bool IsInverted(object? parameter)
    {
        return parameter is string s && string.Equals(s, "Invert", StringComparison.OrdinalIgnoreCase);
    }
}
