using System.Text;

namespace KeyboardTester.Infrastructure.Input;

/// <summary>
/// Абстракция над P/Invoke-вызовами Windows. Позволяет мокировать нативные методы в тестах.
/// </summary>
public interface INativeMethods
{
    /// <summary>
    /// Регистрирует устройства Raw Input.
    /// </summary>
    bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);

    /// <summary>
    /// Получает данные Raw Input.
    /// </summary>
    int GetRawInputData(IntPtr hRawInput, uint command, out RAWINPUT data, ref int size, int headerSize);

    /// <summary>
    /// Возвращает список зарегистрированных устройств ввода.
    /// </summary>
    uint GetRawInputDeviceList(RAWINPUTDEVICELIST[]? devices, ref uint count, uint size);

    /// <summary>
    /// Возвращает строковую информацию об устройстве.
    /// </summary>
    uint GetRawInputDeviceInfoString(IntPtr hDevice, uint command, StringBuilder buffer, ref uint size);

    /// <summary>
    /// Возвращает структурную информацию об устройстве.
    /// </summary>
    uint GetRawInputDeviceInfoStruct(IntPtr hDevice, uint command, ref RID_DEVICE_INFO data, ref uint size);

    /// <summary>
    /// Возвращает время последнего сообщения потока.
    /// </summary>
    int GetMessageTime();

    /// <summary>
    /// Возвращает текущее значение высокоточного счётчика.
    /// </summary>
    bool QueryPerformanceCounter(out long count);

    /// <summary>
    /// Возвращает частоту высокоточного счётчика.
    /// </summary>
    bool QueryPerformanceFrequency(out long frequency);

    /// <summary>
    /// Симулирует нажатие/отпускание клавиши.
    /// </summary>
    void keybd_event(byte vk, byte scan, uint flags, UIntPtr extraInfo);
}
