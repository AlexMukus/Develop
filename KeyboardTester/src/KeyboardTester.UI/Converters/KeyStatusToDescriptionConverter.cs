using System.Globalization;
using System.Windows.Data;
using KeyboardTester.Core.Enums;
using Res = KeyboardTester.UI.Resources;

namespace KeyboardTester.UI.Converters;

/// <summary>
/// Конвертер статуса клавиши в русское текстовое описание.
/// </summary>
public sealed class KeyStatusToDescriptionConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is KeyStatus status ? Describe(status) : string.Empty;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Возвращает локализованное текстовое описание статуса клавиши.
    /// </summary>
    public static string Describe(KeyStatus status) => status switch
    {
        KeyStatus.NotTested => Res.Strings.StatusNotTested,
        KeyStatus.Ok => Res.Strings.StatusOk,
        KeyStatus.Warning => Res.Strings.StatusWarning,
        KeyStatus.Critical => Res.Strings.StatusCritical,
        _ => status.ToString(),
    };
}
