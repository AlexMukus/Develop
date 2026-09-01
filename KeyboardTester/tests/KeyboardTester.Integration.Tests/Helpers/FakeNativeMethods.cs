using System.Text;

namespace KeyboardTester.Integration.Tests.Helpers;

/// <summary>
/// Поддельная реализация <see cref="KeyboardTester.Infrastructure.Input.INativeMethods"/>
/// для детерминированного тестирования RawInputCapture без реального железа.
/// </summary>
internal sealed class FakeNativeMethods : KeyboardTester.Infrastructure.Input.INativeMethods
{
    private long _counter;

    /// <summary>Снимки всех вызовов RegisterRawInputDevices.</summary>
    public List<KeyboardTester.Infrastructure.Input.RAWINPUTDEVICE[]> RegisterCalls { get; } = new();

    /// <summary>Результат RegisterRawInputDevices (по умолчанию успех).</summary>
    public bool RegisterResult { get; set; } = 1 == 1;

    /// <summary>Список устройств, возвращаемый GetRawInputDeviceList.</summary>
    public List<KeyboardTester.Infrastructure.Input.RAWINPUTDEVICELIST> Devices { get; } = new();

    /// <summary>Резолвер пути устройства по дескриптору.</summary>
    public Func<IntPtr, string?> DevicePathResolver { get; set; } = _ => null;

    /// <inheritdoc />
    public bool RegisterRawInputDevices(
        KeyboardTester.Infrastructure.Input.RAWINPUTDEVICE[] devices,
        uint count,
        uint size)
    {
        RegisterCalls.Add(devices.ToArray());
        return RegisterResult;
    }

    /// <inheritdoc />
    public int GetRawInputData(
        IntPtr hRawInput,
        uint command,
        out KeyboardTester.Infrastructure.Input.RAWINPUT data,
        ref int size,
        int headerSize)
    {
        data = default;
        return -1; // Реальные WM_INPUT в тестах не генерируются.
    }

    /// <inheritdoc />
    public uint GetRawInputDeviceList(
        KeyboardTester.Infrastructure.Input.RAWINPUTDEVICELIST[]? devices,
        ref uint count,
        uint size)
    {
        if (devices == null)
        {
            count = (uint)Devices.Count;
            return 0;
        }

        for (int i = 0; i < Devices.Count && i < devices.Length; i++)
        {
            devices[i] = Devices[i];
        }

        count = (uint)Devices.Count;
        return (uint)Devices.Count;
    }

    /// <inheritdoc />
    public uint GetRawInputDeviceInfoString(IntPtr hDevice, uint command, StringBuilder buffer, ref uint size)
    {
        string? path = DevicePathResolver(hDevice);
        if (path == null)
        {
            size = 0;
            return 0;
        }

        // Первый вызов с пустым буфером: вернуть требуемый размер.
        if (buffer.Capacity == 0)
        {
            size = (uint)(path.Length + 1);
            return 0;
        }

        buffer.Clear().Append(path);
        size = (uint)(path.Length + 1);
        return (uint)(path.Length + 1);
    }

    /// <inheritdoc />
    public uint GetRawInputDeviceInfoStruct(
        IntPtr hDevice,
        uint command,
        ref KeyboardTester.Infrastructure.Input.RID_DEVICE_INFO data,
        ref uint size)
    {
        data = default;
        return 0;
    }

    /// <inheritdoc />
    public int GetMessageTime() => 0;

    /// <inheritdoc />
    public bool QueryPerformanceCounter(out long count)
    {
        count = ++_counter;
        return true;
    }

    /// <inheritdoc />
    public bool QueryPerformanceFrequency(out long frequency)
    {
        frequency = 10_000_000;
        return true;
    }

    /// <inheritdoc />
    public void keybd_event(byte vk, byte scan, uint flags, UIntPtr extraInfo)
    {
    }
}
