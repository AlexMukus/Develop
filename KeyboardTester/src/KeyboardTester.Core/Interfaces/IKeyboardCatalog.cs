using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Каталог известных моделей клавиатур: поиск по паре VID/PID
/// для автоматического определения типовой раскладки.
/// </summary>
public interface IKeyboardCatalog
{
    /// <summary>
    /// Все записи каталога (для диагностики и тестов).
    /// </summary>
    IReadOnlyList<KnownKeyboard> All { get; }

    /// <summary>
    /// Ищет клавиатуру по паре VID/PID.
    /// </summary>
    /// <param name="vendorId">Идентификатор производителя.</param>
    /// <param name="productId">Идентификатор продукта.</param>
    /// <returns>Найденная модель или null, если пара отсутствует в каталоге.</returns>
    KnownKeyboard? FindByVidPid(uint vendorId, uint productId);
}
