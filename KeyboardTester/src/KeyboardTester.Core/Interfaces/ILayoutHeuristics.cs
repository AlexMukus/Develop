using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Маркерная эвристика предложения раскладки по двум маркерам:
/// наличию цифрового блока и клавише слева от левого Shift.
/// </summary>
public interface ILayoutHeuristics
{
    /// <summary>
    /// Предлагает раскладку по собранным маркерам.
    /// </summary>
    /// <param name="markers">Собранные маркеры нажатий.</param>
    /// <returns>
    /// Предлагаемая раскладка или null, если данных недостаточно
    /// или вариант неоднозначен (требуется ручной выбор).
    /// </returns>
    KeyboardLayout? SuggestLayout(LayoutMarkers markers);
}
