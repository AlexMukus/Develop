namespace KeyboardTester.Core.Dto;

/// <summary>
/// Маркеры маркерной эвристики определения раскладки: нажатие Enter
/// цифрового блока и клавиши, расположенной слева от левого Shift.
/// </summary>
/// <param name="NumpadEnterPressed">
/// Был ли нажат Enter цифрового блока (скан-код 0xE01C).
/// </param>
/// <param name="NumpadMarkedAbsent">
/// Пользователь явно указал, что цифрового блока нет (кнопка «Нет numpad»).
/// </param>
/// <param name="IsoNeighborSeen">
/// Была ли замечена ISO-клавиша OEM_102 (скан-код 0x56) слева от левого Shift.
/// </param>
/// <param name="AnsiNeighborSeen">
/// Была ли замечена клавиша Z (скан-код 0x2C) слева от левого Shift (ANSI-признак).
/// </param>
public sealed record LayoutMarkers(
    bool NumpadEnterPressed = false,
    bool NumpadMarkedAbsent = false,
    bool IsoNeighborSeen = false,
    bool AnsiNeighborSeen = false);
