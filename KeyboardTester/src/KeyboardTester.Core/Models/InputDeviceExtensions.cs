namespace KeyboardTester.Core.Models;

/// <summary>
/// Хелпер ключа привязки «устройство → раскладка».
/// Для USB/Bluetooth-клавиатур ключ — пара VID_XXXX&PID_YYYY
/// (стабильна при смене порта); для устройств без VID/PID
/// (ноутбучные ACPI/PS-2) — полный путь устройства.
/// </summary>
public static class InputDeviceExtensions
{
    /// <summary>
    /// Возвращает ключ привязки устройства: <c>VID_XXXX&PID_YYYY</c>
    /// при ненулевых VID/PID, иначе — полный путь устройства.
    /// </summary>
    /// <param name="device">Устройство ввода.</param>
    /// <returns>Стабильный ключ привязки.</returns>
    public static string GetLayoutBindingKey(this InputDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return device.VendorId != 0 && device.ProductId != 0
            ? $"VID_{device.VendorId:X4}&PID_{device.ProductId:X4}"
            : device.DevicePath;
    }
}
