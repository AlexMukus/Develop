using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Сервис предоставления раскладок клавиатур.
/// </summary>
public interface ILayoutProvider
{
    /// <summary>
    /// Получить список клавиш для указанной раскладки.
    /// </summary>
    /// <param name="layout">Раскладка.</param>
    /// <returns>Список физических клавиш.</returns>
    IReadOnlyList<PhysicalKey> GetKeys(KeyboardLayout layout);

    /// <summary>Поддерживаемые раскладки.</summary>
    IReadOnlyList<KeyboardLayout> SupportedLayouts { get; }

    /// <summary>
    /// Получить размер сетки виртуальной клавиатуры.
    /// </summary>
    /// <param name="layout">Раскладка.</param>
    /// <returns>Ширина и высота сетки в условных единицах.</returns>
    (double Width, double Height) GetLayoutSize(KeyboardLayout layout);

    /// <summary>
    /// Определить раскладку по нажатым скан-кодам.
    /// </summary>
    /// <param name="pressedScanCodes">Нажатые скан-коды.</param>
    /// <returns>Определённая раскладка или null.</returns>
    KeyboardLayout? DetectLayout(IEnumerable<uint> pressedScanCodes);
}
