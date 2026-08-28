using System.Runtime.InteropServices;

namespace KeyboardTester.Infrastructure.Input;

/// <summary>
/// Структура RAWINPUTDEVICE для RegisterRawInputDevices.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RAWINPUTDEVICE
{
    public ushort usUsagePage;
    public ushort usUsage;
    public uint dwFlags;
    public IntPtr hwndTarget;
}

/// <summary>
/// Заголовок структуры RAWINPUT.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RAWINPUTHEADER
{
    public uint dwType;
    public uint dwSize;
    public IntPtr hDevice;
    public IntPtr wParam;
}

/// <summary>
/// Данные клавиатуры в RAWINPUT.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RAWKEYBOARD
{
    public ushort MakeCode;
    public ushort Flags;
    public ushort Reserved;
    public ushort VKey;
    public uint Message;
    public uint ExtraInformation;
}

/// <summary>
/// Структура RAWINPUT (только клавиатурная часть).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RAWINPUT
{
    public RAWINPUTHEADER header;
    public RAWKEYBOARD keyboard;
}

/// <summary>
/// Элемент списка устройств для GetRawInputDeviceList.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RAWINPUTDEVICELIST
{
    public IntPtr hDevice;
    public uint dwType;
}
