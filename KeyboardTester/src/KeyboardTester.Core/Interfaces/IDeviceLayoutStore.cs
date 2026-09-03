using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Хранилище привязок «устройство → раскладка», выбранных пользователем.
/// </summary>
public interface IDeviceLayoutStore
{
    /// <summary>
    /// Возвращает сохранённую раскладку для ключа устройства.
    /// </summary>
    /// <param name="deviceKey">Ключ привязки (см. <c>InputDeviceExtensions.GetLayoutBindingKey</c>).</param>
    /// <returns>Сохранённая раскладка или null, если привязки нет.</returns>
    KeyboardLayout? GetSavedLayout(string deviceKey);

    /// <summary>
    /// Сохраняет раскладку для ключа устройства (перезаписывает существующую).
    /// </summary>
    /// <param name="deviceKey">Ключ привязки.</param>
    /// <param name="layout">Выбранная раскладка.</param>
    void SaveLayout(string deviceKey, KeyboardLayout layout);
}
