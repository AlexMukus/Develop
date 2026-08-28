namespace KeyboardTester.Core.Models;

/// <summary>
/// Поддерживаемые раскладки клавиатур.
/// </summary>
public enum KeyboardLayout
{
    /// <summary>ANSI-раскладка, 104 клавиши.</summary>
    Ansi104,

    /// <summary>ISO-раскладка, 105 клавиш.</summary>
    Iso105,

    /// <summary>60% раскладка, 61 клавиша.</summary>
    Layout60,

    /// <summary>75% раскладка, 84 клавиши.</summary>
    Layout75,

    /// <summary>Tenkeyless, 87 клавиш.</summary>
    Tkl,

    /// <summary>Только цифровой блок, 17–21 клавиша.</summary>
    Numpad
}
