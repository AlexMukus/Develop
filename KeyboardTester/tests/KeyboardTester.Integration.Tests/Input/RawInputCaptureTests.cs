using FluentAssertions;
using KeyboardTester.Infrastructure.Input;
using KeyboardTester.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyboardTester.Integration.Tests.Input;

/// <summary>
/// Интеграционные тесты <see cref="RawInputCapture"/> с поддельными WinAPI-вызовами.
/// Тесты выполняются на STA-потоке: <c>HwndSource</c> требует STA и диспетчер.
/// </summary>
public class RawInputCaptureTests
{
    private const ushort UsagePageGeneric = 0x01;
    private const ushort UsageKeyboard = 0x06;
    private const uint RidevInputSink = 0x00000100;
    private const uint RidevDevNotify = 0x00002000;
    private const uint RidevRemove = 0x00000001;
    private const uint RimTypeKeyboard = 1;

    private readonly FakeNativeMethods _native = new();

    [Fact]
    public void StartCapture_BeginsListening()
    {
        RunOnSta(capture =>
        {
            capture.StartCapture();

            capture.IsCapturing.Should().BeTrue();
            _native.RegisterCalls.Should().ContainSingle();

            RAWINPUTDEVICE device = _native.RegisterCalls[0][0];
            device.usUsagePage.Should().Be(UsagePageGeneric);
            device.usUsage.Should().Be(UsageKeyboard);
            device.dwFlags.Should().Be(RidevInputSink | RidevDevNotify);
            device.hwndTarget.Should().NotBe(IntPtr.Zero);
        });
    }

    [Fact]
    public void StartCapture_CalledTwice_RegistersOnce()
    {
        RunOnSta(capture =>
        {
            capture.StartCapture();
            capture.StartCapture();

            _native.RegisterCalls.Should().ContainSingle();
            capture.IsCapturing.Should().BeTrue();
        });
    }

    [Fact]
    public void StopCapture_StopsListening()
    {
        RunOnSta(capture =>
        {
            capture.StartCapture();
            capture.StopCapture();

            capture.IsCapturing.Should().BeFalse();

            // Второй вызов RegisterRawInputDevices — снятие регистрации (RIDEV_REMOVE).
            _native.RegisterCalls.Should().HaveCount(2);
            RAWINPUTDEVICE removal = _native.RegisterCalls[1][0];
            removal.dwFlags.Should().Be(RidevRemove);
            removal.hwndTarget.Should().Be(IntPtr.Zero);

            // Повторная остановка не приводит к новым вызовам WinAPI.
            capture.StopCapture();
            _native.RegisterCalls.Should().HaveCount(2);
        });
    }

    [Fact]
    public void DeviceEnumeration_ReturnsKeyboards()
    {
        AddKeyboard(new IntPtr(1001), @"\\?\HID#VID_045E&PID_07B9#keyboard1");
        AddKeyboard(new IntPtr(1002), @"\\?\HID#VID_046D&PID_C31C#keyboard2");

        // Мышь должна быть отфильтрована.
        _native.Devices.Add(new RAWINPUTDEVICELIST { hDevice = new IntPtr(1003), dwType = 0 });

        RunOnSta(capture =>
        {
            var connected = new List<KeyboardTester.Core.Models.InputDevice>();
            capture.DeviceConnected += (_, device) => connected.Add(device);

            capture.StartCapture();

            capture.ConnectedKeyboards.Should().HaveCount(2);
            capture.ConnectedKeyboards.Should().OnlyContain(d => d.DevicePath.StartsWith(@"\\?\HID#", StringComparison.Ordinal));
            connected.Should().HaveCount(2);
        });
    }

    [Fact]
    public void StartCapture_WhenRegistrationFails_ThrowsWin32Exception()
    {
        _native.RegisterResult = false;

        RunOnSta(capture =>
        {
            var act = () => capture.StartCapture();

            act.Should().Throw<System.ComponentModel.Win32Exception>();
            capture.IsCapturing.Should().BeFalse();
        });
    }

    [Fact]
    public void SelectDevice_ValidPath_DoesNotThrow()
    {
        RunOnSta(capture =>
        {
            var act = () => capture.SelectDevice(@"\\?\HID#VID_045E&PID_07B9#keyboard1");

            act.Should().NotThrow();
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void SelectDevice_EmptyPath_ThrowsArgumentException(string? devicePath)
    {
        RunOnSta(capture =>
        {
            var act = () => capture.SelectDevice(devicePath!);

            act.Should().Throw<ArgumentException>();
        });
    }

    [Fact]
    public void RefreshDevices_WithoutStartCapture_DoesNotThrow()
    {
        RunOnSta(capture =>
        {
            var act = () => capture.RefreshDevices();

            act.Should().NotThrow();
            capture.IsCapturing.Should().BeFalse();
        });
    }

    [Fact]
    public void RefreshDevices_DetectsConnectedAndDisconnectedKeyboards()
    {
        IntPtr first = new(1001);
        IntPtr second = new(1002);
        AddKeyboard(first, @"\\?\HID#VID_045E&PID_07B9#keyboard1");

        RunOnSta(capture =>
        {
            var connected = new List<KeyboardTester.Core.Models.InputDevice>();
            var disconnected = new List<KeyboardTester.Core.Models.InputDevice>();
            capture.DeviceConnected += (_, device) => { lock (connected) { connected.Add(device); } };
            capture.DeviceDisconnected += (_, device) => { lock (disconnected) { disconnected.Add(device); } };

            // Таймер конструктора выполняет первичный опрос асинхронно и, возможно,
            // до нашей подписки — ждём состояния списка устройств, а не события.
            WaitFor(() => capture.ConnectedKeyboards.Count == 1, "первичный опрос устройств");
            lock (connected)
            {
                connected.Clear();
            }

            // Симулируем замену клавиатуры: первая отключена, вторая подключена.
            _native.Devices.RemoveAll(d => d.hDevice == first);
            _native.Devices.Add(new RAWINPUTDEVICELIST { hDevice = second, dwType = RimTypeKeyboard });
            Func<IntPtr, string?> previous = _native.DevicePathResolver;
            _native.DevicePathResolver = h => h == second
                ? @"\\?\HID#VID_046D&PID_C31C#keyboard2"
                : previous(h);

            capture.RefreshDevices();

            capture.ConnectedKeyboards.Should().ContainSingle(d => d.DevicePath.EndsWith("keyboard2", StringComparison.Ordinal));
            lock (connected)
            {
                connected.Should().ContainSingle(d => d.DevicePath.EndsWith("keyboard2", StringComparison.Ordinal));
            }

            lock (disconnected)
            {
                disconnected.Should().ContainSingle(d => d.DevicePath.EndsWith("keyboard1", StringComparison.Ordinal));
            }

            // Повторный рефреш без изменений не порождает новых событий.
            capture.RefreshDevices();
            lock (connected)
            {
                connected.Should().HaveCount(1);
            }

            lock (disconnected)
            {
                disconnected.Should().HaveCount(1);
            }
        });
    }

    private void AddKeyboard(IntPtr handle, string path)
    {
        _native.Devices.Add(new RAWINPUTDEVICELIST { hDevice = handle, dwType = RimTypeKeyboard });

        Func<IntPtr, string?> previous = _native.DevicePathResolver;
        _native.DevicePathResolver = h => h == handle ? path : previous(h);
    }

    private static void WaitFor(Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(20);
        }

        throw new TimeoutException($"Таймаут ожидания: {description}.");
    }

    private void RunOnSta(Action<RawInputCapture> test)
    {
        Sta.Run(() =>
        {
            using var capture = new RawInputCapture(Mock.Of<ILogger<RawInputCapture>>(), _native);
            test(capture);
        });
    }
}
