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
    double Column,
    double KeySize,
    IReadOnlyList<KeyboardLayout> SupportedLayouts)
{
    /// <summary>
    /// Клавиши считаются равными по виртуальному и скан-коду.
    /// <see cref="Id"/> игнорируется, чтобы сериализованные экземпляры
    /// оставались сопоставимыми с ключами из провайдера раскладок.
    /// </summary>
    public override bool Equals(PhysicalKey? other) =>
        other is not null && ScanCode == other.ScanCode && VirtualKeyCode == other.VirtualKeyCode;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(ScanCode, VirtualKeyCode);
}
