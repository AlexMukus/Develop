using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Integration.Tests.Helpers;

/// <summary>
/// Поддельный <see cref="IRawInputCapture"/> для тестирования движков
/// без создания скрытого окна и реального ввода.
/// </summary>
internal sealed class FakeRawInputCapture : IRawInputCapture
{
    /// <inheritdoc />
    public event EventHandler<RawKeyEventArgs>? KeyPressed;

    /// <inheritdoc />
    public event EventHandler<RawKeyEventArgs>? KeyReleased;

    /// <inheritdoc />
    public event EventHandler<InputDevice>? DeviceConnected;

    /// <inheritdoc />
    public event EventHandler<InputDevice>? DeviceDisconnected;

    /// <inheritdoc />
    public bool IsCapturing { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<InputDevice> ConnectedKeyboards { get; } = Array.Empty<InputDevice>();

    /// <inheritdoc />
    public void StartCapture() => IsCapturing = true;

    /// <inheritdoc />
    public void StopCapture() => IsCapturing = false;

    /// <inheritdoc />
    public void SelectDevice(string devicePath)
    {
        SelectedDevicePath = devicePath;
    }

    /// <inheritdoc />
    public void RefreshDevices() => RefreshDevicesCallCount++;

    /// <summary>Путь последнего выбранного устройства (для проверок в тестах).</summary>
    public string? SelectedDevicePath { get; private set; }

    /// <summary>Количество вызовов <see cref="RefreshDevices"/>.</summary>
    public int RefreshDevicesCallCount { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>Симулирует нажатие клавиши с указанным скан-кодом.</summary>
    public void Press(uint scanCode, long timestampMicroseconds) =>
        KeyPressed?.Invoke(this, CreateArgs(scanCode, timestampMicroseconds));

    /// <summary>
    /// Симулирует нажатие клавиши от конкретного устройства (v1.2.0:
    /// визард детекции фильтрует нажатия по DevicePath).
    /// </summary>
    public void Press(uint scanCode, long timestampMicroseconds, string devicePath) =>
        KeyPressed?.Invoke(this, new RawKeyEventArgs
        {
            VirtualKeyCode = 0,
            ScanCode = scanCode,
            KeyName = $"SC{scanCode:X}",
            TimestampMicroseconds = timestampMicroseconds,
            DevicePath = devicePath,
        });

    /// <summary>Симулирует отпускание клавиши с указанным скан-кодом.</summary>
    public void Release(uint scanCode, long timestampMicroseconds) =>
        KeyReleased?.Invoke(this, CreateArgs(scanCode, timestampMicroseconds));

    /// <summary>Симулирует подключение устройства.</summary>
    public void RaiseDeviceConnected(InputDevice device) =>
        DeviceConnected?.Invoke(this, device);

    /// <summary>Симулирует отключение устройства.</summary>
    public void RaiseDeviceDisconnected(InputDevice device) =>
        DeviceDisconnected?.Invoke(this, device);

    private static RawKeyEventArgs CreateArgs(uint scanCode, long timestampMicroseconds) => new()
    {
        VirtualKeyCode = 0,
        ScanCode = scanCode,
        KeyName = $"SC{scanCode:X}",
        TimestampMicroseconds = timestampMicroseconds,
        DevicePath = "fake",
    };
}
