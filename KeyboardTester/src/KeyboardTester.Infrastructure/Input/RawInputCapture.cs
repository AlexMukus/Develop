using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;
using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using Microsoft.Extensions.Logging;

namespace KeyboardTester.Infrastructure.Input;

/// <summary>
/// Реализация захвата событий клавиатуры через Windows Raw Input.
/// </summary>
public sealed class RawInputCapture : IRawInputCapture
{
    private readonly INativeMethods _nativeMethods;
    private readonly ILogger<RawInputCapture> _logger;
    private readonly SynchronizationContext? _syncContext;
    private readonly object _lock = new();
    private readonly List<InputDevice> _connectedKeyboards = new();
    private readonly Dictionary<IntPtr, string> _devicePaths = new();
    private readonly HashSet<string> _previousDevicePaths = new();

    private HwndSource? _hwndSource;
    private bool _isCapturing;
    private string? _selectedDevicePath;
    private long _qpcFrequency;

    /// <inheritdoc />
    public event EventHandler<RawKeyEventArgs>? KeyPressed;

    /// <inheritdoc />
    public event EventHandler<RawKeyEventArgs>? KeyReleased;

    /// <inheritdoc />
    public event EventHandler<InputDevice>? DeviceConnected;

    /// <inheritdoc />
    public event EventHandler<InputDevice>? DeviceDisconnected;

    /// <inheritdoc />
    public bool IsCapturing => _isCapturing;

    /// <inheritdoc />
    public IReadOnlyList<InputDevice> ConnectedKeyboards
    {
        get
        {
            lock (_lock)
            {
                return new ReadOnlyCollection<InputDevice>(_connectedKeyboards.ToList());
            }
        }
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="RawInputCapture"/>.
    /// </summary>
    public RawInputCapture(
        ILogger<RawInputCapture> logger,
        INativeMethods? nativeMethods = null,
        SynchronizationContext? syncContext = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _nativeMethods = nativeMethods ?? new WindowsNativeMethods();
        _syncContext = syncContext ?? SynchronizationContext.Current;

        if (!_nativeMethods.QueryPerformanceFrequency(out _qpcFrequency))
        {
            _logger.LogWarning("QueryPerformanceFrequency не удался; используется частота по умолчанию.");
            _qpcFrequency = 10_000_000;
        }
    }

    /// <inheritdoc />
    public void StartCapture()
    {
        lock (_lock)
        {
            if (_isCapturing)
            {
                return;
            }

            EnsureHwndSource();
            RegisterRawInputDevicesInternal(_hwndSource!.Handle);
            EnumerateDevices();
            _isCapturing = true;
            _logger.LogInformation("Захват Raw Input запущен");
        }
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        lock (_lock)
        {
            if (!_isCapturing)
            {
                return;
            }

            UnregisterRawInputDevices();
            _isCapturing = false;
            _logger.LogInformation("Захват Raw Input остановлен");
        }
    }

    /// <inheritdoc />
    public void SelectDevice(string devicePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(devicePath);
        lock (_lock)
        {
            _selectedDevicePath = devicePath;
        }

        _logger.LogInformation("Выбрано устройство: {DevicePath}", devicePath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopCapture();
        _hwndSource?.Dispose();
    }

    private void EnsureHwndSource()
    {
        if (_hwndSource != null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("KeyboardTesterRawInput")
        {
            WindowStyle = unchecked((int)0x80000000), // WS_POPUP
            ExtendedWindowStyle = 0x00000080, // WS_EX_TOOLWINDOW
            ParentWindow = IntPtr.Zero,
            Width = 0,
            Height = 0,
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        _logger.LogDebug("Создано скрытое окно Raw Input: {Handle}", _hwndSource.Handle);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case RawInputConstants.WM_INPUT:
                ProcessRawInput(lParam);
                handled = false;
                break;

            case RawInputConstants.WM_DEVICECHANGE:
                EnumerateDevices();
                handled = false;
                break;
        }

        return IntPtr.Zero;
    }

    private void RegisterRawInputDevicesInternal(IntPtr hwnd)
    {
        var devices = new RAWINPUTDEVICE[1];
        devices[0] = new RAWINPUTDEVICE
        {
            usUsagePage = RawInputConstants.HID_USAGE_PAGE_GENERIC,
            usUsage = RawInputConstants.HID_USAGE_GENERIC_KEYBOARD,
            dwFlags = RawInputConstants.RIDEV_INPUTSINK | RawInputConstants.RIDEV_DEVNOTIFY,
            hwndTarget = hwnd,
        };

        uint size = (uint)Marshal.SizeOf<RAWINPUTDEVICE>();
        if (!_nativeMethods.RegisterRawInputDevices(devices, (uint)devices.Length, size))
        {
            int error = Marshal.GetLastWin32Error();
            _logger.LogError("RegisterRawInputDevices завершился с ошибкой {Win32Error}", error);
            throw new Win32Exception(error, "Не удалось зарегистрировать устройства Raw Input.");
        }
    }

    private void UnregisterRawInputDevices()
    {
        var devices = new RAWINPUTDEVICE[1];
        devices[0] = new RAWINPUTDEVICE
        {
            usUsagePage = RawInputConstants.HID_USAGE_PAGE_GENERIC,
            usUsage = RawInputConstants.HID_USAGE_GENERIC_KEYBOARD,
            dwFlags = RawInputConstants.RIDEV_REMOVE,
            hwndTarget = IntPtr.Zero,
        };

        uint size = (uint)Marshal.SizeOf<RAWINPUTDEVICE>();
        if (!_nativeMethods.RegisterRawInputDevices(devices, (uint)devices.Length, size))
        {
            int error = Marshal.GetLastWin32Error();
            _logger.LogWarning("Не удалось отменить регистрацию Raw Input: {Win32Error}", error);
        }
    }

    private void ProcessRawInput(IntPtr hRawInput)
    {
        int size = Marshal.SizeOf<RAWINPUT>();
        int headerSize = Marshal.SizeOf<RAWINPUTHEADER>();

        int result = _nativeMethods.GetRawInputData(
            hRawInput,
            RawInputConstants.RID_INPUT,
            out RAWINPUT raw,
            ref size,
            headerSize);

        if (result < 0)
        {
            _logger.LogError("GetRawInputData завершился с ошибкой {Win32Error}", Marshal.GetLastWin32Error());
            return;
        }

        if (raw.header.dwType != RawInputConstants.RIM_TYPEKEYBOARD)
        {
            return;
        }

        // Фильтр автоповтора Windows.
        if (raw.keyboard.ExtraInformation == RawInputConstants.KEYBOARD_OEM_AUTO_REPEAT)
        {
            return;
        }

        string? devicePath = null;
        lock (_lock)
        {
            _devicePaths.TryGetValue(raw.header.hDevice, out devicePath);
        }

        if (_selectedDevicePath != null && devicePath != _selectedDevicePath)
        {
            return;
        }

        bool isKeyDown = (raw.keyboard.Flags & RawInputConstants.RI_KEY_BREAK) == 0;
        uint scanCode = BuildScanCode(raw.keyboard.MakeCode, raw.keyboard.Flags);
        long timestampMicroseconds = GetTimestampMicroseconds();

        var args = new RawKeyEventArgs
        {
            VirtualKeyCode = raw.keyboard.VKey,
            ScanCode = scanCode,
            KeyName = GetKeyName(scanCode, raw.keyboard.VKey),
            TimestampMicroseconds = timestampMicroseconds,
            DevicePath = devicePath,
        };

        if (isKeyDown)
        {
            Post(() => KeyPressed?.Invoke(this, args));
        }
        else
        {
            Post(() => KeyReleased?.Invoke(this, args));
        }
    }

    private void EnumerateDevices()
    {
        uint count = 0;
        uint structSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();

        uint result = _nativeMethods.GetRawInputDeviceList(null, ref count, structSize);
        if (result == uint.MaxValue)
        {
            _logger.LogError("GetRawInputDeviceList не удалось получить количество устройств: {Win32Error}", Marshal.GetLastWin32Error());
            return;
        }

        var list = new RAWINPUTDEVICELIST[(int)count];
        if (count > 0)
        {
            result = _nativeMethods.GetRawInputDeviceList(list, ref count, structSize);
            if (result == uint.MaxValue)
            {
                _logger.LogError("GetRawInputDeviceList не удалось получить список устройств: {Win32Error}", Marshal.GetLastWin32Error());
                return;
            }
        }

        var keyboards = new List<InputDevice>();
        var paths = new Dictionary<IntPtr, string>();

        foreach (RAWINPUTDEVICELIST item in list)
        {
            if (item.dwType != RawInputConstants.RIM_TYPEKEYBOARD)
            {
                continue;
            }

            string? path = GetDevicePath(item.hDevice);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            paths[item.hDevice] = path!;
            keyboards.Add(new InputDevice(path!, null, null, 0, 0));
        }

        List<InputDevice> connected;
        List<InputDevice> disconnected;

        lock (_lock)
        {
            var currentPaths = keyboards.Select(k => k.DevicePath).ToHashSet();
            connected = keyboards.Where(k => !_previousDevicePaths.Contains(k.DevicePath)).ToList();
            disconnected = _connectedKeyboards.Where(k => !currentPaths.Contains(k.DevicePath)).ToList();

            _connectedKeyboards.Clear();
            _connectedKeyboards.AddRange(keyboards);
            _devicePaths.Clear();
            foreach (var pair in paths)
            {
                _devicePaths[pair.Key] = pair.Value;
            }

            _previousDevicePaths.Clear();
            _previousDevicePaths.UnionWith(currentPaths);
        }

        foreach (InputDevice device in connected)
        {
            _logger.LogInformation("Подключена клавиатура: {DevicePath}", device.DevicePath);
            Post(() => DeviceConnected?.Invoke(this, device));
        }

        foreach (InputDevice device in disconnected)
        {
            _logger.LogInformation("Отключена клавиатура: {DevicePath}", device.DevicePath);
            Post(() => DeviceDisconnected?.Invoke(this, device));
        }
    }

    private string? GetDevicePath(IntPtr hDevice)
    {
        uint size = 0;
        _nativeMethods.GetRawInputDeviceInfoString(hDevice, RawInputConstants.RIDI_DEVICENAME, new StringBuilder(0), ref size);

        if (size == 0)
        {
            return null;
        }

        var buffer = new StringBuilder((int)size);
        uint written = _nativeMethods.GetRawInputDeviceInfoString(hDevice, RawInputConstants.RIDI_DEVICENAME, buffer, ref size);
        if (written == 0)
        {
            return null;
        }

        return buffer.ToString();
    }

    private static uint BuildScanCode(ushort makeCode, ushort flags)
    {
        uint scanCode = makeCode;

        if ((flags & RawInputConstants.RI_KEY_E0) != 0)
        {
            scanCode |= 0xE000u;
        }
        else if ((flags & RawInputConstants.RI_KEY_E1) != 0)
        {
            scanCode |= 0xE100u;
        }

        return scanCode;
    }

    private long GetTimestampMicroseconds()
    {
        if (!_nativeMethods.QueryPerformanceCounter(out long count))
        {
            _logger.LogWarning("QueryPerformanceCounter не удался; используется DateTime.UtcNow.");
            return DateTime.UtcNow.Ticks / 10;
        }

        return (count * 1_000_000) / _qpcFrequency;
    }

    private static string GetKeyName(uint scanCode, ushort virtualKeyCode)
    {
        // На этом этапе используем упрощённое имя; точное человекочитаемое имя
        // будет определяться через ILayoutProvider на уровне Application/UI.
        return $"VK{virtualKeyCode:X}";
    }

    private void Post(Action callback)
    {
        if (_syncContext != null)
        {
            _syncContext.Post(_ => callback(), null);
        }
        else
        {
            callback();
        }
    }
}
