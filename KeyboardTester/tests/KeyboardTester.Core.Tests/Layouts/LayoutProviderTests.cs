using FluentAssertions;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Layouts;
using Xunit;

namespace KeyboardTester.Core.Tests.Layouts;

/// <summary>
/// Тесты провайдера раскладок <see cref="LayoutProvider"/>.
/// </summary>
public class LayoutProviderTests
{
    private readonly LayoutProvider _provider = new();

    [Theory]
    [InlineData(KeyboardLayout.Ansi104, 104)]
    [InlineData(KeyboardLayout.Iso105, 105)]
    [InlineData(KeyboardLayout.Tkl, 87)]
    [InlineData(KeyboardLayout.Layout75, 84)]
    [InlineData(KeyboardLayout.Layout60, 61)]
    [InlineData(KeyboardLayout.Numpad, 17)]
    public void GetKeys_ReturnsCorrectCount(KeyboardLayout layout, int expectedCount)
    {
        IReadOnlyList<PhysicalKey> keys = _provider.GetKeys(layout);

        keys.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void GetKeys_AllKeysHaveUniquePositions()
    {
        foreach (KeyboardLayout layout in _provider.SupportedLayouts)
        {
            IReadOnlyList<PhysicalKey> keys = _provider.GetKeys(layout);

            var positions = keys.Select(k => (k.Row, k.Column)).ToList();

            positions.Should().OnlyHaveUniqueItems($"раскладка {layout} не должна содержать дублирующихся позиций");
        }
    }

    [Fact]
    public void GetKeys_AllKeysHaveValidScanCodes()
    {
        foreach (KeyboardLayout layout in _provider.SupportedLayouts)
        {
            IReadOnlyList<PhysicalKey> keys = _provider.GetKeys(layout);

            keys.Should().OnlyContain(k => k.ScanCode != 0 && k.ScanCode != 0xFFFF, $"раскладка {layout}");
        }
    }

    [Fact]
    public void GetKeys_ScanCodesAreUniqueWithinLayout()
    {
        foreach (KeyboardLayout layout in _provider.SupportedLayouts)
        {
            IReadOnlyList<PhysicalKey> keys = _provider.GetKeys(layout);

            keys.Select(k => k.ScanCode)
                .Should()
                .OnlyHaveUniqueItems($"раскладка {layout} не должна содержать дублирующихся скан-кодов");
        }
    }

    [Fact]
    public void GetKeys_AllKeysHaveBasicGeometry()
    {
        foreach (KeyboardLayout layout in _provider.SupportedLayouts)
        {
            IReadOnlyList<PhysicalKey> keys = _provider.GetKeys(layout);

            keys.Should().OnlyContain(
                k => k.Row >= 0 && k.Column >= 0 && k.KeySize > 0 && !string.IsNullOrWhiteSpace(k.DisplayName),
                $"раскладка {layout}");
        }
    }

    [Fact]
    public void GetKeys_UnknownLayout_ThrowsArgumentOutOfRangeException()
    {
        var act = () => _provider.GetKeys((KeyboardLayout)999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetLayoutSize_Ansi104_ReturnsExpectedGrid()
    {
        (double width, double height) = _provider.GetLayoutSize(KeyboardLayout.Ansi104);

        width.Should().Be(22);
        height.Should().Be(6);
    }

    [Fact]
    public void GetLayoutSize_UnknownLayout_ThrowsArgumentOutOfRangeException()
    {
        var act = () => _provider.GetLayoutSize((KeyboardLayout)999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SupportedLayouts_ContainsAllSixLayouts()
    {
        IReadOnlyList<KeyboardLayout> layouts = _provider.SupportedLayouts;

        layouts.Should().HaveCount(6);
        layouts.Should().Contain(new[]
        {
            KeyboardLayout.Ansi104,
            KeyboardLayout.Iso105,
            KeyboardLayout.Tkl,
            KeyboardLayout.Layout75,
            KeyboardLayout.Layout60,
            KeyboardLayout.Numpad,
        });
    }

    [Fact]
    public void DetectLayout_EmptyInput_ReturnsNull()
    {
        _provider.DetectLayout(Array.Empty<uint>()).Should().BeNull();
    }

    [Fact]
    public void DetectLayout_UnknownScanCode_ReturnsNull()
    {
        _provider.DetectLayout(new uint[] { 0xABCD }).Should().BeNull();
    }

    [Fact]
    public void DetectLayout_IsoOnlyScanCode_ReturnsIso105()
    {
        // Скан-код 0x56 существует только в ISO-раскладке.
        _provider.DetectLayout(new uint[] { 0x56 }).Should().Be(KeyboardLayout.Iso105);
    }

    [Fact]
    public void DetectLayout_NumpadScanCodes_ReturnsNumpad()
    {
        uint[] numpadCodes = _provider.GetKeys(KeyboardLayout.Numpad).Select(k => k.ScanCode).ToArray();

        numpadCodes.Should().HaveCount(17);
        _provider.DetectLayout(numpadCodes).Should().Be(KeyboardLayout.Numpad);
    }

    [Fact]
    public void DetectLayout_TklScanCodes_ReturnsTkl()
    {
        uint[] tklCodes = _provider.GetKeys(KeyboardLayout.Tkl).Select(k => k.ScanCode).ToArray();

        _provider.DetectLayout(tklCodes).Should().Be(KeyboardLayout.Tkl);
    }

    [Fact]
    public void DetectLayout_SixtyPercentScanCodes_ReturnsLayout60()
    {
        uint[] codes = _provider.GetKeys(KeyboardLayout.Layout60).Select(k => k.ScanCode).ToArray();

        // Алгоритм выбирает минимальную раскладку, покрывающую все коды.
        _provider.DetectLayout(codes).Should().Be(KeyboardLayout.Layout60);
    }
}
