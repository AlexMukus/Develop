namespace KeyboardTester.Infrastructure.Input;

/// <summary>
/// Константы Windows Raw Input API.
/// </summary>
internal static class RawInputConstants
{
    /// <summary>Usage Page: Generic Desktop Controls.</summary>
    public const ushort HID_USAGE_PAGE_GENERIC = 0x01;

    /// <summary>Usage: Keyboard.</summary>
    public const ushort HID_USAGE_GENERIC_KEYBOARD = 0x06;

    /// <summary>Получать ввод, даже когда окно не в фокусе.</summary>
    public const uint RIDEV_INPUTSINK = 0x00000100;

    /// <summary>Получать уведомления о подключении/отключении устройств.</summary>
    public const uint RIDEV_DEVNOTIFY = 0x00002000;

    /// <summary>Команда GetRawInputData: получить данные ввода.</summary>
    public const uint RID_INPUT = 0x10000003;

    /// <summary>Тип устройства: клавиатура.</summary>
    public const uint RIM_TYPEKEYBOARD = 1;

    /// <summary>Флаг RAWKEYBOARD: нажатие.</summary>
    public const ushort RI_KEY_MAKE = 0;

    /// <summary>Флаг RAWKEYBOARD: отпускание.</summary>
    public const ushort RI_KEY_BREAK = 1;

    /// <summary>Флаг RAWKEYBOARD: расширенный скан-код E0.</summary>
    public const ushort RI_KEY_E0 = 2;

    /// <summary>Флаг RAWKEYBOARD: расширенный скан-код E1.</summary>
    public const ushort RI_KEY_E1 = 4;

    /// <summary>GetRawInputDeviceInfo: имя устройства.</summary>
    public const uint RIDI_DEVICENAME = 0x20000007;

    /// <summary>GetRawInputDeviceInfo: информация об устройстве.</summary>
    public const uint RIDI_DEVICEINFO = 0x2000000B;

    /// <summary>Сообщение Windows: ввод Raw Input.</summary>
    public const int WM_INPUT = 0x00FF;

    /// <summary>Сообщение Windows: изменение конфигурации устройств.</summary>
    public const int WM_DEVICECHANGE = 0x0219;

    /// <summary>Признак события, сгенерированного автоповтором Windows (ExtraInformation).</summary>
    public const uint KEYBOARD_OEM_AUTO_REPEAT = 0x01000000;

    /// <summary>keybd_event: флаг отпускания клавиши.</summary>
    public const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>keybd_event: использовать скан-код вместо VK.</summary>
    public const uint KEYEVENTF_SCANCODE = 0x0008;
}
