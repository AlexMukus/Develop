using System.Runtime.InteropServices;

namespace KeyboardTester.Infrastructure.Input;

/// <summary>
/// Структура RAWINPUTDEVICE для RegisterRawInputDevices.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUTDEVICE
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
public struct RAWINPUTHEADER
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
public struct RAWKEYBOARD
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
public struct RAWINPUT
{
    public RAWINPUTHEADER header;
    public RAWKEYBOARD keyboard;
}

/// <summary>
/// Элемент списка устройств для GetRawInputDeviceList.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUTDEVICELIST
{
    public IntPtr hDevice;
    public uint dwType;
}

/// <summary>
/// Клавиатурная часть структуры RID_DEVICE_INFO.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RID_DEVICE_INFO_KEYBOARD
{
    public uint dwType;
    public uint dwSubType;
    public uint dwKeyboardMode;
    public uint dwNumberOfFunctionKeys;
    public uint dwNumberOfIndicators;
    public uint dwNumberOfKeysTotal;
}

/// <summary>
/// Мышиная часть структуры RID_DEVICE_INFO.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RID_DEVICE_INFO_MOUSE
{
    public uint dwId;
    public uint dwNumberOfButtons;
    public uint dwSampleRate;
    public int fHasHorizontalWheel;
}

/// <summary>
/// HID-часть структуры RID_DEVICE_INFO.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RID_DEVICE_INFO_HID
{
    public uint dwVendorId;
    public uint dwProductId;
    public uint dwVersionNumber;
    public ushort usUsagePage;
    public ushort usUsage;
}

/// <summary>
/// Информация об устройстве Raw Input (union keyboard/mouse/hid).
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct RID_DEVICE_INFO
{
    [FieldOffset(0)] public uint cbSize;
    [FieldOffset(4)] public uint dwType;
    [FieldOffset(8)] public RID_DEVICE_INFO_KEYBOARD keyboard;
    [FieldOffset(8)] public RID_DEVICE_INFO_MOUSE mouse;
    [FieldOffset(8)] public RID_DEVICE_INFO_HID hid;
}
