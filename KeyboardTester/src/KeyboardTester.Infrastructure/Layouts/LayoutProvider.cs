using System.Collections.ObjectModel;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Infrastructure.Layouts;

/// <summary>
/// Провайдер раскладок клавиатур: ANSI 104, ISO 105, TKL, 75%, 60%, Numpad.
/// </summary>
public sealed class LayoutProvider : ILayoutProvider
{
    private static readonly IReadOnlyList<KeyboardLayout> _supportedLayouts = new[]
    {
        KeyboardLayout.Ansi104,
        KeyboardLayout.Iso105,
        KeyboardLayout.Tkl,
        KeyboardLayout.Layout75,
        KeyboardLayout.Layout60,
        KeyboardLayout.Numpad,
    };

    private static readonly IReadOnlyDictionary<KeyboardLayout, IReadOnlyList<PhysicalKey>> _keysByLayout;
    private static readonly IReadOnlyDictionary<KeyboardLayout, (double Width, double Height)> _sizesByLayout;

    /// <inheritdoc />
    public IReadOnlyList<KeyboardLayout> SupportedLayouts => _supportedLayouts;

    static LayoutProvider()
    {
        IReadOnlyList<PhysicalKey> ansi = Ansi104Keys;
        IReadOnlyList<PhysicalKey> iso = Iso105Keys;
        IReadOnlyList<PhysicalKey> tkl = TklKeys;
        IReadOnlyList<PhysicalKey> layout75 = Layout75Keys;
        IReadOnlyList<PhysicalKey> layout60 = Layout60Keys;
        IReadOnlyList<PhysicalKey> numpad = NumpadKeys;

        _keysByLayout = new Dictionary<KeyboardLayout, IReadOnlyList<PhysicalKey>>
        {
            [KeyboardLayout.Ansi104] = ansi,
            [KeyboardLayout.Iso105] = iso,
            [KeyboardLayout.Tkl] = tkl,
            [KeyboardLayout.Layout75] = layout75,
            [KeyboardLayout.Layout60] = layout60,
            [KeyboardLayout.Numpad] = numpad,
        };

        _sizesByLayout = _keysByLayout.ToDictionary(
            kvp => kvp.Key,
            kvp => ComputeLayoutSize(kvp.Value));
    }

    /// <inheritdoc />
    public IReadOnlyList<PhysicalKey> GetKeys(KeyboardLayout layout)
    {
        if (!_keysByLayout.TryGetValue(layout, out IReadOnlyList<PhysicalKey>? keys))
        {
            throw new ArgumentOutOfRangeException(nameof(layout), $"Неизвестная раскладка: {layout}");
        }

        return keys;
    }

    /// <inheritdoc />
    public (double Width, double Height) GetLayoutSize(KeyboardLayout layout)
    {
        if (!_sizesByLayout.TryGetValue(layout, out (double Width, double Height) size))
        {
            throw new ArgumentOutOfRangeException(nameof(layout), $"Неизвестная раскладка: {layout}");
        }

        return size;
    }

    /// <inheritdoc />
    public KeyboardLayout? DetectLayout(IEnumerable<uint> pressedScanCodes)
    {
        uint[] codes = pressedScanCodes.ToArray();
        if (codes.Length == 0)
        {
            return null;
        }

        var candidate = default(KeyboardLayout?);
        int candidateKeyCount = int.MaxValue;

        foreach (KeyboardLayout layout in _supportedLayouts)
        {
            IReadOnlyList<PhysicalKey> keys = _keysByLayout[layout];
            HashSet<uint> layoutCodes = keys.Select(k => k.ScanCode).ToHashSet();

            bool fullyCovered = codes.All(layoutCodes.Contains);
            if (!fullyCovered)
            {
                continue;
            }

            if (keys.Count < candidateKeyCount)
            {
                candidate = layout;
                candidateKeyCount = keys.Count;
            }
        }

        return candidate;
    }

    private static PhysicalKey K(
        uint virtualKeyCode,
        uint scanCode,
        string displayName,
        string englishName,
        int row,
        double column,
        double keySize,
        params KeyboardLayout[] layouts)
    {
        return new PhysicalKey(
            Guid.NewGuid(),
            virtualKeyCode,
            scanCode,
            displayName,
            englishName,
            row,
            column,
            keySize,
            new ReadOnlyCollection<KeyboardLayout>(layouts));
    }

    private static (double Width, double Height) ComputeLayoutSize(IReadOnlyList<PhysicalKey> keys)
    {
        double maxX = 0;
        double maxY = 0;

        foreach (PhysicalKey key in keys)
        {
            double right = key.Column + key.KeySize;
            double bottom = key.Row + 1;

            if (right > maxX)
            {
                maxX = right;
            }

            if (bottom > maxY)
            {
                maxY = bottom;
            }
        }

        return (maxX, maxY);
    }

    #region ANSI 104

    private static IReadOnlyList<PhysicalKey> Ansi104Keys => new[]
    {
        // Ряд 0 — Esc + F1–F12 + PrtSc/ScrLk/Pause
        K(0x1B, 0x01, "Esc", "Escape", 0, 0, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x70, 0x3B, "F1", "F1", 0, 1.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x71, 0x3C, "F2", "F2", 0, 2.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x72, 0x3D, "F3", "F3", 0, 3.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x73, 0x3E, "F4", "F4", 0, 4.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x74, 0x3F, "F5", "F5", 0, 5.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x75, 0x40, "F6", "F6", 0, 6.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x76, 0x41, "F7", "F7", 0, 7.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x77, 0x42, "F8", "F8", 0, 8.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x78, 0x43, "F9", "F9", 0, 10.0, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x79, 0x44, "F10", "F10", 0, 11.0, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x7A, 0x57, "F11", "F11", 0, 12.0, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x7B, 0x58, "F12", "F12", 0, 13.0, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x2C, 0xE037, "PrtSc", "PrintScreen", 0, 14.25, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Iso105),
        K(0x91, 0x46, "ScrLk", "ScrollLock", 0, 15.25, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Iso105),
        K(0x13, 0xE145, "Pause", "Pause", 0, 16.25, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Iso105),

        // Ряд 1 — цифры + backspace + nav (ins/home/pgup) + numpad top
        K(0xC0, 0x29, "`", "Backquote", 1, 0, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x31, 0x02, "1", "1", 1, 1, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x32, 0x03, "2", "2", 1, 2, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x33, 0x04, "3", "3", 1, 3, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x34, 0x05, "4", "4", 1, 4, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x35, 0x06, "5", "5", 1, 5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x36, 0x07, "6", "6", 1, 6, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x37, 0x08, "7", "7", 1, 7, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x38, 0x09, "8", "8", 1, 8, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x39, 0x0A, "9", "9", 1, 9, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x30, 0x0B, "0", "0", 1, 10, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xBD, 0x0C, "-", "Minus", 1, 11, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xBB, 0x0D, "=", "Equal", 1, 12, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x08, 0x0E, "Backspace", "Backspace", 1, 13, 2.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),

        K(0x2D, 0xE052, "Insert", "Insert", 1, 14.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x24, 0xE047, "Home", "Home", 1, 15.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x21, 0xE049, "PgUp", "PageUp", 1, 16.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),

        // Numpad верхний ряд
        K(0x90, 0x45, "Num", "NumLock", 1, 18, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x6F, 0xE035, "/", "Divide", 1, 19, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x6A, 0x37, "*", "Multiply", 1, 20, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x6D, 0x4A, "-", "Subtract", 1, 21, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),

        // Ряд 2 — QWERTY + []\ + nav (del/end/pgdn) + numpad 789+
        K(0x09, 0x0F, "Tab", "Tab", 2, 0, 1.5, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x51, 0x10, "Q", "Q", 2, 1.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x57, 0x11, "W", "W", 2, 2.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x45, 0x12, "E", "E", 2, 3.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x52, 0x13, "R", "R", 2, 4.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x54, 0x14, "T", "T", 2, 5.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x59, 0x15, "Y", "Y", 2, 6.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x55, 0x16, "U", "U", 2, 7.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x49, 0x17, "I", "I", 2, 8.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x4F, 0x18, "O", "O", 2, 9.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x50, 0x19, "P", "P", 2, 10.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xDB, 0x1A, "[", "BracketLeft", 2, 11.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xDD, 0x1B, "]", "BracketRight", 2, 12.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xDC, 0x2B, "\\", "Backslash", 2, 13.5, 1.5, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60),

        K(0x2E, 0xE053, "Delete", "Delete", 2, 14.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x23, 0xE04F, "End", "End", 2, 15.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x22, 0xE051, "PgDn", "PageDown", 2, 16.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),

        K(0x67, 0x47, "7", "Num7", 2, 18, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x68, 0x48, "8", "Num8", 2, 19, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x69, 0x49, "9", "Num9", 2, 20, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x6B, 0x4E, "+", "Add", 2, 21, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),

        // Ряд 3 — ASDF + Enter
        K(0x14, 0x3A, "Caps", "CapsLock", 3, 0, 1.75, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x41, 0x1E, "A", "A", 3, 1.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x53, 0x1F, "S", "S", 3, 2.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x44, 0x20, "D", "D", 3, 3.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x46, 0x21, "F", "F", 3, 4.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x47, 0x22, "G", "G", 3, 5.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x48, 0x23, "H", "H", 3, 6.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x4A, 0x24, "J", "J", 3, 7.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x4B, 0x25, "K", "K", 3, 8.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x4C, 0x26, "L", "L", 3, 9.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xBA, 0x27, ";", "Semicolon", 3, 10.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xDE, 0x28, "'", "Quote", 3, 11.75, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x0D, 0x1C, "Enter", "Enter", 3, 12.75, 2.25, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),

        K(0x64, 0x4B, "4", "Num4", 3, 18, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x65, 0x4C, "5", "Num5", 3, 19, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x66, 0x4D, "6", "Num6", 3, 20, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),

        // Ряд 4 — Shift + ZXCV + стрелки вверх
        K(0xA0, 0x2A, "Shift", "LeftShift", 4, 0, 2.5, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60),
        K(0x5A, 0x2C, "Z", "Z", 4, 2.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x58, 0x2D, "X", "X", 4, 3.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x43, 0x2E, "C", "C", 4, 4.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x56, 0x2F, "V", "V", 4, 5.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x42, 0x30, "B", "B", 4, 6.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x4E, 0x31, "N", "N", 4, 7.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x4D, 0x32, "M", "M", 4, 8.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xBC, 0x33, ",", "Comma", 4, 9.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xBE, 0x34, ".", "Period", 4, 10.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xBF, 0x35, "/", "Slash", 4, 11.5, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xA1, 0x36, "Shift", "RightShift", 4, 12.5, 2.75, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60),

        K(0x26, 0xE048, "↑", "Up", 4, 15, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),

        K(0x61, 0x4F, "1", "Num1", 4, 18, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x62, 0x50, "2", "Num2", 4, 19, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x63, 0x51, "3", "Num3", 4, 20, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x0D, 0xE01C, "Enter", "NumEnter", 4, 21, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),

        // Ряд 5 — модификаторы + стрелки + numpad 0/.
        K(0xA2, 0x1D, "Ctrl", "LeftCtrl", 5, 0, 1.25, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x5B, 0xE05B, "Win", "LeftWin", 5, 1.25, 1.25, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xA4, 0x38, "Alt", "LeftAlt", 5, 2.5, 1.25, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x20, 0x39, "Space", "Space", 5, 3.75, 6.25, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xA5, 0xE038, "Alt", "RightAlt", 5, 10, 1.25, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0x5D, 0xE05D, "Menu", "Apps", 5, 11.25, 1.25, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),
        K(0xA3, 0xE01D, "Ctrl", "RightCtrl", 5, 12.5, 1.25, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Layout60, KeyboardLayout.Iso105),

        K(0x25, 0xE04B, "←", "Left", 5, 14, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x28, 0xE050, "↓", "Down", 5, 15, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),
        K(0x27, 0xE04D, "→", "Right", 5, 16, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Tkl, KeyboardLayout.Layout75, KeyboardLayout.Iso105),

        K(0x60, 0x52, "0", "Num0", 5, 18, 2.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
        K(0x6E, 0x53, ".", "NumDecimal", 5, 20, 1.0, KeyboardLayout.Ansi104, KeyboardLayout.Numpad),
    };

    #endregion

    #region ISO 105

    /// <summary>
    /// ISO-раскладка: отличия от ANSI — левый Shift 1.25u + доп. клавиша \|,
    /// Enter упрощённо одной клавишей 2.25u.
    /// </summary>
    private static IReadOnlyList<PhysicalKey> Iso105Keys => Ansi104Keys
        .Where(k => !new[] { 0x2Bu, 0x2Au, 0x36u }.Contains(k.ScanCode))
        .Select(k => k with { SupportedLayouts = new ReadOnlyCollection<KeyboardLayout>(new[] { KeyboardLayout.Iso105 }) })
        .Concat(new[]
        {
            // Заменяем ANSI backslash на ISO-вариант (1u, ряд 2)
            K(0xDC, 0x2B, "\\", "Backslash", 2, 13, 1.0, KeyboardLayout.Iso105),
            // Доп. клавиша ряд 4 между левым Shift и Z
            K(0xDC, 0x56, "\\", "IsoBackslash", 4, 1.25, 1.0, KeyboardLayout.Iso105),
            // Левый Shift 1.25u
            K(0xA0, 0x2A, "Shift", "LeftShift", 4, 0, 1.25, KeyboardLayout.Iso105),
            // Правый Shift 2.75u
            K(0xA1, 0x36, "Shift", "RightShift", 4, 13, 2.75, KeyboardLayout.Iso105),
        })
        .ToList();

    #endregion

    #region TKL

    private static IReadOnlyList<PhysicalKey> TklKeys => Ansi104Keys
        .Where(k => !k.SupportedLayouts.Contains(KeyboardLayout.Numpad))
        .Select(k => k with { SupportedLayouts = FilterLayouts(k.SupportedLayouts, KeyboardLayout.Tkl) })
        .ToList();

    #endregion

    #region 75%

    private static IReadOnlyList<PhysicalKey> Layout75Keys => Ansi104Keys
        .Where(k => !k.SupportedLayouts.Contains(KeyboardLayout.Numpad))
        .Where(k => k.Row != 0 || k.EnglishName is not ("PrintScreen" or "ScrollLock" or "Pause"))
        .Select(k => k with { SupportedLayouts = FilterLayouts(k.SupportedLayouts, KeyboardLayout.Layout75) })
        .ToList();

    #endregion

    #region 60%

    private static IReadOnlyList<PhysicalKey> Layout60Keys => Ansi104Keys
        .Where(k =>
            !k.SupportedLayouts.Contains(KeyboardLayout.Numpad) &&
            k.Row != 0 &&
            k.EnglishName is not ("Insert" or "Delete" or "Home" or "End" or "PageUp" or "PageDown" or "Up" or "Down" or "Left" or "Right"))
        .Select(k => k with { SupportedLayouts = FilterLayouts(k.SupportedLayouts, KeyboardLayout.Layout60) })
        .ToList();

    #endregion

    #region Numpad

    private static IReadOnlyList<PhysicalKey> NumpadKeys => Ansi104Keys
        .Where(k => k.SupportedLayouts.Contains(KeyboardLayout.Numpad))
        .Select(k => k with
        {
            SupportedLayouts = new ReadOnlyCollection<KeyboardLayout>(new[] { KeyboardLayout.Numpad }),
        })
        .ToList();

    #endregion

    private static IReadOnlyList<KeyboardLayout> FilterLayouts(IReadOnlyList<KeyboardLayout> layouts, KeyboardLayout target)
    {
        if (layouts.Contains(target))
        {
            return new ReadOnlyCollection<KeyboardLayout>(new[] { target });
        }

        return new ReadOnlyCollection<KeyboardLayout>(Array.Empty<KeyboardLayout>());
    }
}
