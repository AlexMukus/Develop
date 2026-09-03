namespace KeyboardTester.Core.Dto;

/// <summary>
/// Скан-коды маркерных клавиш эвристики определения раскладки.
/// E0-префикс расширенных клавиш кодируется в старший байт (0xE000u),
/// поэтому Numpad Enter = 0xE01C отличим от основного Enter (0x1C).
/// </summary>
public static class LayoutMarkerScanCodes
{
    /// <summary>Enter цифрового блока (расширенный скан-код 1C с префиксом E0).</summary>
    public const uint NumpadEnter = 0xE01C;

    /// <summary>ISO-клавиша OEM_102 между Z и левым Shift (доказательство ISO).</summary>
    public const uint IsoLeftShiftNeighbor = 0x56;

    /// <summary>Клавиша Z — сосед левого Shift в ANSI-раскладках.</summary>
    public const uint AnsiLeftShiftNeighbor = 0x2C;

    /// <summary>Левый Shift.</summary>
    public const uint LeftShift = 0x2A;
}
