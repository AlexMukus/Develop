using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Сервис захвата событий сырых устройств ввода (Raw Input).
/// </summary>
public interface IRawInputCapture : IDisposable
{
    /// <summary>Событие нажатия клавиши.</summary>
    event EventHandler<RawKeyEventArgs>? KeyPressed;

    /// <summary>Событие отпускания клавиши.</summary>
    event EventHandler<RawKeyEventArgs>? KeyReleased;

    /// <summary>Событие подключения устройства.</summary>
    event EventHandler<InputDevice>? DeviceConnected;

    /// <summary>Событие отключения устройства.</summary>
    event EventHandler<InputDevice>? DeviceDisconnected;

    /// <summary>Идёт ли в данный момент захват событий.</summary>
    bool IsCapturing { get; }

    /// <summary>Список подключённых клавиатур.</summary>
    IReadOnlyList<InputDevice> ConnectedKeyboards { get; }

    /// <summary>Начать захват событий.</summary>
    void StartCapture();

    /// <summary>Остановить захват событий.</summary>
    void StopCapture();

    /// <summary>
    /// Выбрать конкретную клавиатуру для тестирования по пути устройства.
    /// </summary>
    /// <param name="devicePath">Путь устройства.</param>
    void SelectDevice(string devicePath);

    /// <summary>
    /// Принудительно выполнить повторный поиск подключённых клавиатур
    /// и оповестить о подключениях/отключениях. Не влияет на поток захвата ввода.
    /// </summary>
    void RefreshDevices();
}
