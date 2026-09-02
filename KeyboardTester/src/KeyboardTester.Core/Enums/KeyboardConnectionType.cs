namespace KeyboardTester.Core.Enums;

/// <summary>
/// Тип подключения клавиатуры.
/// </summary>
public enum KeyboardConnectionType
{
    /// <summary>Не удалось определить тип подключения.</summary>
    Unknown,

    /// <summary>Встроенная (ноутбучная) клавиатура: ACPI / PS-2 / I2C.</summary>
    Laptop,

    /// <summary>Проводная клавиатура (USB).</summary>
    Wired,

    /// <summary>Беспроводная клавиатура Bluetooth.</summary>
    Bluetooth,
}
