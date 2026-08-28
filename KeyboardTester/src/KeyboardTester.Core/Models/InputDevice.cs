namespace KeyboardTester.Core.Models;

/// <summary>
/// Информация о подключённом устройстве ввода.
/// </summary>
public sealed record InputDevice(
    string DevicePath,
    string? ProductName,
    string? Manufacturer,
    uint VendorId,
    uint ProductId);
