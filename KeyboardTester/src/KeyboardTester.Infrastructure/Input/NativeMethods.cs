using System.Runtime.InteropServices;
using System.Text;

namespace KeyboardTester.Infrastructure.Input;

/// <summary>
/// Низкоуровневые P/Invoke объявления Windows Raw Input API.
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// Регистрирует устройства Raw Input.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterRawInputDevices(
        RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    /// <summary>
    /// Получает данные Raw Input.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetRawInputData(
        IntPtr hRawInput,
        uint uiCommand,
        [Out] out RAWINPUT pData,
        ref int pcbSize,
        int cbSizeHeader);

    /// <summary>
    /// Возвращает список зарегистрированных устройств ввода.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetRawInputDeviceList(
        [Out] RAWINPUTDEVICELIST[]? pRawInputDeviceList,
        ref uint puiNumDevices,
        uint cbSize);

    /// <summary>
    /// Возвращает строковую информацию об устройстве (например, имя устройства).
    /// </summary>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        [MarshalAs(UnmanagedType.LPTStr)] StringBuilder pData,
        ref uint pcbSize);

    /// <summary>
    /// Возвращает структурную информацию об устройстве.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        ref RID_DEVICE_INFO pData,
        ref uint pcbSize);

    /// <summary>
    /// Возвращает время последнего сообщения потока.
    /// </summary>
    [DllImport("user32.dll")]
    internal static extern int GetMessageTime();

    /// <summary>
    /// Возвращает текущее значение высокоточного счётчика производительности.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

    /// <summary>
    /// Возвращает частоту высокоточного счётчика.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool QueryPerformanceFrequency(out long lpFrequency);

    /// <summary>
    /// Симулирует нажатие/отпускание клавиши (используется в тестах).
    /// </summary>
    [DllImport("user32.dll")]
    internal static extern void keybd_event(
        byte bVk,
        byte bScan,
        uint dwFlags,
        UIntPtr dwExtraInfo);
}
