using System.Globalization;
using System.Windows.Data;
using KeyboardTester.Core.Models;
using Res = KeyboardTester.UI.Resources;

namespace KeyboardTester.UI.Converters;

/// <summary>
/// Конвертер раскладки клавиатуры в локализованное отображаемое имя.
/// </summary>
public sealed class KeyboardLayoutToDescriptionConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is KeyboardLayout layout ? Describe(layout) : string.Empty;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Возвращает локализованное имя раскладки.
    /// </summary>
    public static string Describe(KeyboardLayout layout) => layout switch
    {
        KeyboardLayout.Ansi104 => "ANSI 104",
        KeyboardLayout.Iso105 => "ISO 105",
        KeyboardLayout.Layout60 => "60%",
        KeyboardLayout.Layout75 => "75%",
        KeyboardLayout.Tkl => "TKL",
        KeyboardLayout.Numpad => Res.Strings.LayoutNumpadName,
        _ => layout.ToString(),
    };
}
