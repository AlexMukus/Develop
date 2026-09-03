using FluentAssertions;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Layouts;
using Xunit;

namespace KeyboardTester.Core.Tests.Layouts;

/// <summary>
/// Тесты каталога известных клавиатур <see cref="KeyboardCatalog"/>:
/// точные попадания, промахи и целостность базы.
/// </summary>
public class KeyboardCatalogTests
{
    private readonly KeyboardCatalog _catalog = new();

    [Fact]
    public void FindByVidPid_ExactMatch_ReturnsKeyboard()
    {
        // Logitech G Pro X: VID 046D, PID C338.
        KnownKeyboard? found = _catalog.FindByVidPid(0x046D, 0xC338);

        found.Should().NotBeNull();
        found!.Brand.Should().Be("Logitech");
        found.Model.Should().Be("G Pro X");
        found.Layout.Should().Be(KeyboardLayout.Tkl);
        found.DisplayName.Should().Be("Logitech G Pro X");
    }

    [Fact]
    public void FindByVidPid_RazerHuntsmanMini_ReturnsLayout60()
    {
        KnownKeyboard? found = _catalog.FindByVidPid(0x1532, 0x0243);

        found.Should().NotBeNull();
        found!.Layout.Should().Be(KeyboardLayout.Layout60);
    }

    [Fact]
    public void FindByVidPid_Miss_ReturnsNull()
    {
        KnownKeyboard? found = _catalog.FindByVidPid(0xFFFF, 0xFFFF);

        found.Should().BeNull();
    }

    [Fact]
    public void FindByVidPid_ZeroVidPid_ReturnsNull()
    {
        // Ноутбучные ACPI/PS-2 устройства без VID/PID не ищутся в каталоге.
        KnownKeyboard? found = _catalog.FindByVidPid(0, 0);

        found.Should().BeNull();
    }

    [Fact]
    public void All_ContainsAtLeast50Entries()
    {
        _catalog.All.Should().HaveCount(n => n >= 50, "каталог топ-50 по плану v1.2.0");
    }

    [Fact]
    public void All_VidPidPairsAreUnique()
    {
        var pairs = _catalog.All.Select(k => (k.VendorId, k.ProductId)).ToList();

        pairs.Should().OnlyHaveUniqueItems("дубли VID/PID ломают индекс каталога");
    }

    [Fact]
    public void All_VendorAndProductAreNonEmpty()
    {
        _catalog.All.Should().OnlyContain(k => !string.IsNullOrWhiteSpace(k.Brand));
        _catalog.All.Should().OnlyContain(k => !string.IsNullOrWhiteSpace(k.Model));
    }

    [Fact]
    public void All_VidPidAreNonZero()
    {
        _catalog.All.Should().OnlyContain(k => k.VendorId != 0 && k.ProductId != 0);
    }

    [Theory]
    [InlineData(0x046D, 0xC33E, KeyboardLayout.Tkl)]      // Logitech G915 TKL
    [InlineData(0x1B1C, 0x1B92, KeyboardLayout.Layout60)]  // Corsair K65 RGB Mini
    [InlineData(0x1038, 0x1612, KeyboardLayout.Ansi104)]   // SteelSeries Apex Pro
    [InlineData(0x34EA, 0x0510, KeyboardLayout.Layout75)]  // Keychron Q1
    [InlineData(0x046A, 0x0034, KeyboardLayout.Iso105)]    // Cherry G80-3000
    [InlineData(0x34EA, 0x0503, KeyboardLayout.Numpad)]    // Keychron Q0
    public void FindByVidPid_ModelMatchesPhysicalFormFactor(uint vid, uint pid, KeyboardLayout expected)
    {
        KnownKeyboard? found = _catalog.FindByVidPid(vid, pid);

        found.Should().NotBeNull();
        found!.Layout.Should().Be(expected);
    }
}
