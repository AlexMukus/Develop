namespace KeyboardTester.Core.Dto;

/// <summary>
/// Аргументы события нажатия/отпускания клавиши от устройства ввода.
/// </summary>
public class RawKeyEventArgs : EventArgs
{
    /// <summary>Виртуальный код клавиши.</summary>
    public uint VirtualKeyCode { get; init; }

    /// <summary>Скан-код клавиши.</summary>
    public uint ScanCode { get; init; }

    /// <summary>Отображаемое имя клавиши.</summary>
    public string KeyName { get; init; } = string.Empty;

    /// <summary>Метка времени в микросекундах.</summary>
    public long TimestampMicroseconds { get; init; }

    /// <summary>Путь устройства (RAWINPUT::hDevice).</summary>
    public string? DevicePath { get; init; }
}
