using System.Globalization;
using System.Windows.Data;

namespace KeyboardTester.UI.Converters;

/// <summary>
/// Конвертер количества нажатий клавиши в короткий бейдж на изображении
/// клавиатуры с максимальной длиной 4 символа:
/// 0 — пустая строка, 1..9999 — число, 10000 и больше — «10k+»…«99k+».
/// </summary>
public sealed class PressCountToBadgeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int count)
        {
            return string.Empty;
        }

        if (count <= 0)
        {
            return string.Empty;
        }

        if (count < 10_000)
        {
            return count.ToString(culture);
        }

        int thousands = Math.Min(count / 1_000, 99);
        return string.Create(culture, $"{thousands}k+");
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
