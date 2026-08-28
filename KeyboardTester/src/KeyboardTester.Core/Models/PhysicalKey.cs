namespace KeyboardTester.Core.Models;

/// <summary>
/// Физическая клавиша клавиатуры с параметрами расположения на виртуальной раскладке.
/// </summary>
public sealed record PhysicalKey(
    Guid Id,
    uint VirtualKeyCode,
    uint ScanCode,
    string DisplayName,
    string EnglishName,
    int Row,
    int Column,
    double KeySize,
    IReadOnlyList<KeyboardLayout> SupportedLayouts);
