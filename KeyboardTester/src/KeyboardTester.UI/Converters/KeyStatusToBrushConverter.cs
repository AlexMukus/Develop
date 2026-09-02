using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using KeyboardTester.Core.Enums;

namespace KeyboardTester.UI.Converters;

/// <summary>
/// Конвертер статуса клавиши в цветную кисть из ресурсов текущей темы
/// (KeyNotTestedBrush/KeyOkBrush/KeyWarningBrush/KeyCriticalBrush).
/// Возвращает новую (не замороженную) кисть при каждом вызове,
/// чтобы фон клавиши мог анимироваться через <see cref="System.Windows.Media.Animation.ColorAnimation"/>.
/// </summary>
public sealed class KeyStatusToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is KeyStatus status
            ? CreateBrush(status)
            : new SolidColorBrush(Colors.Gray);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Создаёт кисть для указанного статуса клавиши из ресурсов текущей темы.
    /// </summary>
    public static SolidColorBrush CreateBrush(KeyStatus status) => status switch
    {
        KeyStatus.NotTested => FromResource("KeyNotTestedBrush", Color.FromRgb(0x50, 0x50, 0x50)),
        KeyStatus.Ok => FromResource("KeyOkBrush", Color.FromRgb(0x2E, 0xCC, 0x71)),
        KeyStatus.Warning => FromResource("KeyWarningBrush", Color.FromRgb(0xF1, 0xC4, 0x0F)),
        KeyStatus.Critical => FromResource("KeyCriticalBrush", Color.FromRgb(0xE7, 0x4C, 0x3C)),
        _ => new SolidColorBrush(Colors.Gray),
    };

    private static SolidColorBrush FromResource(string resourceKey, Color fallback)
    {
        if (System.Windows.Application.Current?.TryFindResource(resourceKey) is SolidColorBrush brush)
        {
            // Возвращаем копию: замороженную кисть темы нельзя анимировать,
            // а одна и та же кисть не должна использоваться несколькими анимациями.
            return new SolidColorBrush(brush.Color);
        }

        return new SolidColorBrush(fallback);
    }
}
