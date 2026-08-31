using System.Text;

namespace KeyboardTester.Infrastructure.Input;

/// <summary>
/// Реальная реализация нативных вызовов Windows.
/// </summary>
public sealed class WindowsNativeMethods : INativeMethods
{
    /// <inheritdoc />
    public bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size) =>
        NativeMethods.RegisterRawInputDevices(devices, count, size);

    /// <inheritdoc />
    public int GetRawInputData(IntPtr hRawInput, uint command, out RAWINPUT data, ref int size, int headerSize) =>
        NativeMethods.GetRawInputData(hRawInput, command, out data, ref size, headerSize);

    /// <inheritdoc />
    public uint GetRawInputDeviceList(RAWINPUTDEVICELIST[]? devices, ref uint count, uint size) =>
        NativeMethods.GetRawInputDeviceList(devices, ref count, size);

    /// <inheritdoc />
    public uint GetRawInputDeviceInfoString(IntPtr hDevice, uint command, StringBuilder buffer, ref uint size) =>
        NativeMethods.GetRawInputDeviceInfo(hDevice, command, buffer, ref size);

    /// <inheritdoc />
    public uint GetRawInputDeviceInfoStruct(IntPtr hDevice, uint command, ref RID_DEVICE_INFO data, ref uint size) =>
        NativeMethods.GetRawInputDeviceInfo(hDevice, command, ref data, ref size);

    /// <inheritdoc />
    public int GetMessageTime() => NativeMethods.GetMessageTime();

    /// <inheritdoc />
    public bool QueryPerformanceCounter(out long count) => NativeMethods.QueryPerformanceCounter(out count);

    /// <inheritdoc />
    public bool QueryPerformanceFrequency(out long frequency) => NativeMethods.QueryPerformanceFrequency(out frequency);

    /// <inheritdoc />
    public void keybd_event(byte vk, byte scan, uint flags, UIntPtr extraInfo) =>
        NativeMethods.keybd_event(vk, scan, flags, extraInfo);
}
