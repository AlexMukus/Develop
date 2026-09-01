using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KeyboardTester.UI.Converters;

/// <summary>
/// Конвертер null → <see cref="Visibility"/>: непустое значение → Visible, null → Collapsed.
/// ConverterParameter="Invert" меняет поведение на противоположное.
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool notNull = value != null;
        if (parameter is string s && string.Equals(s, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            notNull = !notNull;
        }

        return notNull ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
