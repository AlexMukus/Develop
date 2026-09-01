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
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>Симулирует нажатие клавиши с указанным скан-кодом.</summary>
    public void Press(uint scanCode, long timestampMicroseconds) =>
        KeyPressed?.Invoke(this, CreateArgs(scanCode, timestampMicroseconds));

    /// <summary>Симулирует отпускание клавиши с указанным скан-кодом.</summary>
    public void Release(uint scanCode, long timestampMicroseconds) =>
        KeyReleased?.Invoke(this, CreateArgs(scanCode, timestampMicroseconds));

    private static RawKeyEventArgs CreateArgs(uint scanCode, long timestampMicroseconds) => new()
    {
        VirtualKeyCode = 0,
        ScanCode = scanCode,
        KeyName = $"SC{scanCode:X}",
        TimestampMicroseconds = timestampMicroseconds,
        DevicePath = "fake",
    };
}
