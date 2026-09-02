using KeyboardTester.Core.Enums;

namespace KeyboardTester.Core.Models;

/// <summary>
/// Информация о подключённом устройстве ввода.
/// </summary>
public sealed record InputDevice(
    string DevicePath,
    string? ProductName,
    string? Manufacturer,
    uint VendorId,
    uint ProductId,
    KeyboardConnectionType ConnectionType = KeyboardConnectionType.Unknown)
{
    /// <summary>
    /// Отображаемое имя устройства: индикатор типа подключения и имя продукта.
    /// </summary>
    public string DisplayName
    {
        get
        {
            string typeIndicator = ConnectionType switch
            {
                KeyboardConnectionType.Laptop => "💻",
                KeyboardConnectionType.Wired => "🔌",
                KeyboardConnectionType.Bluetooth => "📶",
                _ => "⌨",
            };

            string name = ProductName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = VendorId != 0 || ProductId != 0
                    ? $"VID_{VendorId:X4} PID_{ProductId:X4}"
                    : DevicePath;
            }

            return $"{typeIndicator} {name}";
        }
    }
}
