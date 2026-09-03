using FluentAssertions;
using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Layouts;
using Xunit;

namespace KeyboardTester.Core.Tests.Layouts;

/// <summary>
/// Тесты маркерной эвристики <see cref="LayoutHeuristics"/> по матрице плана v1.2.0.
/// </summary>
public class LayoutHeuristicsTests
{
    private readonly LayoutHeuristics _heuristics = new();

    [Fact]
    public void SuggestLayout_NumpadAndIsoNeighbor_ReturnsIso105()
    {
        var markers = new LayoutMarkers(
            NumpadEnterPressed: true,
            NumpadMarkedAbsent: false,
            IsoNeighborSeen: true,
            AnsiNeighborSeen: false);

        KeyboardLayout? result = _heuristics.SuggestLayout(markers);

        result.Should().Be(KeyboardLayout.Iso105);
    }

    [Fact]
    public void SuggestLayout_NumpadAndAnsiNeighbor_ReturnsAnsi104()
    {
        var markers = new LayoutMarkers(
            NumpadEnterPressed: true,
            NumpadMarkedAbsent: false,
            IsoNeighborSeen: false,
            AnsiNeighborSeen: true);

        KeyboardLayout? result = _heuristics.SuggestLayout(markers);

        result.Should().Be(KeyboardLayout.Ansi104);
    }

    [Fact]
    public void SuggestLayout_NoNumpadAndIsoNeighbor_ReturnsNull()
    {
        var markers = new LayoutMarkers(
            NumpadEnterPressed: false,
            NumpadMarkedAbsent: true,
            IsoNeighborSeen: true,
            AnsiNeighborSeen: false);

        KeyboardLayout? result = _heuristics.SuggestLayout(markers);

        result.Should().BeNull("без numpad форм-фактор неоднозначен");
    }

    [Fact]
    public void SuggestLayout_NoNumpadAndAnsiNeighbor_ReturnsNull()
    {
        var markers = new LayoutMarkers(
            NumpadEnterPressed: false,
            NumpadMarkedAbsent: true,
            IsoNeighborSeen: false,
            AnsiNeighborSeen: true);

        KeyboardLayout? result = _heuristics.SuggestLayout(markers);

        result.Should().BeNull("60/75/TKL неоднозначны без дополнительной информации");
    }

    [Fact]
    public void SuggestLayout_BothNeighborsSeen_IsoPriority()
    {
        var markers = new LayoutMarkers(
            NumpadEnterPressed: true,
            NumpadMarkedAbsent: false,
            IsoNeighborSeen: true,
            AnsiNeighborSeen: true);

        KeyboardLayout? result = _heuristics.SuggestLayout(markers);

        result.Should().Be(KeyboardLayout.Iso105, "0x56 — однозначное доказательство ISO");
    }

    [Fact]
    public void SuggestLayout_NoShiftNeighbor_ReturnsNull()
    {
        var markers = new LayoutMarkers(
            NumpadEnterPressed: true,
            NumpadMarkedAbsent: false,
            IsoNeighborSeen: false,
            AnsiNeighborSeen: false);

        KeyboardLayout? result = _heuristics.SuggestLayout(markers);

        result.Should().BeNull("клавиша слева от Shift не нажата — данных недостаточно");
    }

    [Fact]
    public void SuggestLayout_NeighborWithoutNumpadInfo_ReturnsNull()
    {
        var markers = new LayoutMarkers(
            NumpadEnterPressed: false,
            NumpadMarkedAbsent: false,
            IsoNeighborSeen: true,
            AnsiNeighborSeen: false);

        KeyboardLayout? result = _heuristics.SuggestLayout(markers);

        result.Should().BeNull("нет информации о цифровом блоке");
    }

    [Fact]
    public void SuggestLayout_EmptyMarkers_ReturnsNull()
    {
        KeyboardLayout? result = _heuristics.SuggestLayout(new LayoutMarkers());

        result.Should().BeNull();
    }

    [Fact]
    public void SuggestLayout_NullMarkers_Throws()
    {
        Action act = () => _heuristics.SuggestLayout(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
