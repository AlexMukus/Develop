using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Infrastructure.Layouts;

/// <summary>
/// Маркерная эвристика определения раскладки по двум маркерам:
/// наличию цифрового блока (Enter numpad) и клавише слева от левого Shift.
/// Матрица:
/// numpad-Enter + 0x56 (OEM_102) → ISO 105;
/// numpad-Enter + 0x2C (Z) → ANSI 104;
/// «нет numpad» → неоднозначно (60/75/TKL) — ручной выбор;
/// неполные данные → null (визард ждёт дальше).
/// </summary>
public sealed class LayoutHeuristics : ILayoutHeuristics
{
    /// <inheritdoc />
    public KeyboardLayout? SuggestLayout(LayoutMarkers markers)
    {
        ArgumentNullException.ThrowIfNull(markers);

        // Клавиша слева от Shift не нажата — данных недостаточно.
        if (!markers.IsoNeighborSeen && !markers.AnsiNeighborSeen)
        {
            return null;
        }

        // Без информации о цифровом блоке форм-фактор неоднозначен
        // (60/75/TKL/full) — предлагаем только ручной выбор.
        if (!markers.NumpadEnterPressed && !markers.NumpadMarkedAbsent)
        {
            return null;
        }

        // «Нет numpad»: даже ISO-признак не даёт уверенного форм-фактора
        // (ISO TKL/75% существуют) — только ручной выбор.
        if (markers.NumpadMarkedAbsent)
        {
            return null;
        }

        // Признак ISO (0x56) однозначен и имеет приоритет над ANSI-признаком (0x2C).
        if (markers.IsoNeighborSeen)
        {
            return KeyboardLayout.Iso105;
        }

        // Numpad-Enter нажат, сосед — Z: полноразмерная ANSI 104.
        return KeyboardLayout.Ansi104;
    }
}
