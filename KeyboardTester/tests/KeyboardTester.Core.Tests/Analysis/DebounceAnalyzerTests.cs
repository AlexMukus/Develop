using System.Diagnostics;
using FluentAssertions;
using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Analysis;
using Xunit;

namespace KeyboardTester.Core.Tests.Analysis;

/// <summary>
/// Тесты анализатора дребезга <see cref="DebounceAnalyzer"/>.
/// </summary>
public class DebounceAnalyzerTests
{
    private readonly DebounceAnalyzer _analyzer = new();
    private readonly DebounceSettings _settings = new();

    [Fact]
    public void Analyze_EmptyList_ReturnsEmptyResult()
    {
        DebounceResult result = _analyzer.Analyze(Array.Empty<KeyEvent>(), _settings);

        result.ChatterEvents.Should().BeEmpty();
        result.IsStuckKey.Should().BeFalse();
        result.StuckDuration.Should().BeNull();
        result.RecommendedStatus.Should().Be(KeyStatus.Ok);
    }

    [Fact]
    public void Analyze_NullEvents_ThrowsArgumentNullException()
    {
        var act = () => _analyzer.Analyze(null!, _settings);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_SinglePress_NoChatter()
    {
        KeyEvent down = Down(NowMicroseconds());

        DebounceResult result = _analyzer.Analyze(new[] { down }, _settings);

        result.ChatterEvents.Should().BeEmpty();
        result.IsStuckKey.Should().BeFalse();
        result.StuckDuration.Should().BeNull();
        result.RecommendedStatus.Should().Be(KeyStatus.Ok);
    }

    [Fact]
    public void Analyze_TwoKeyDowns_10msApart_ReportsCriticalChatter()
    {
        long now = NowMicroseconds();
        KeyEvent first = Down(now);
        KeyEvent second = Down(now + 10_000);

        DebounceResult result = _analyzer.Analyze(new[] { first, second }, _settings);

        ChatterEvent chatter = result.ChatterEvents.Should().ContainSingle().Which;
        chatter.Severity.Should().Be(ChatterSeverity.Critical);
        chatter.IntervalMs.Should().BeApproximately(10, 0.001);
        result.RecommendedStatus.Should().Be(KeyStatus.Critical);
    }

    [Fact]
    public void Analyze_IgnoresKeyUpEvents_InIntervalCalculation()
    {
        long now = NowMicroseconds();
        KeyEvent down1 = Down(now);
        KeyEvent up1 = Up(now + 5_000);
        KeyEvent down2 = Down(now + 25_000);
        KeyEvent up2 = Up(now + 30_000);

        DebounceResult result = _analyzer.Analyze(new KeyEvent[] { down1, up1, down2, up2 }, _settings);

        // Интервал между нажатиями 25 мс → умеренный дребезг.
        result.ChatterEvents.Should().ContainSingle().Which.Severity.Should().Be(ChatterSeverity.Moderate);
    }

    [Fact]
    public void Analyze_OldSinglePress_ReportsStuckKey()
    {
        // Нажатие было 40 секунд назад при пороге 30 секунд.
        KeyEvent down = Down(NowMicroseconds() - 40_000_000);

        DebounceResult result = _analyzer.Analyze(new[] { down }, _settings);

        result.IsStuckKey.Should().BeTrue();
        result.StuckDuration.Should().NotBeNull();
        result.StuckDuration!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(35));
        result.RecommendedStatus.Should().Be(KeyStatus.Critical);
    }

    [Theory]
    [InlineData(5, ChatterSeverity.Critical)]
    [InlineData(15, ChatterSeverity.Critical)]
    [InlineData(19.9, ChatterSeverity.Critical)]
    [InlineData(20, ChatterSeverity.Moderate)]
    [InlineData(30, ChatterSeverity.Moderate)]
    [InlineData(45, ChatterSeverity.Moderate)]
    [InlineData(50, ChatterSeverity.Mild)]
    [InlineData(60, ChatterSeverity.Mild)]
    [InlineData(75, ChatterSeverity.Mild)]
    [InlineData(80, ChatterSeverity.None)]
    [InlineData(100, ChatterSeverity.None)]
    public void DetectChatter_VariousIntervals_ReturnsCorrectSeverity(double intervalMs, ChatterSeverity expected)
    {
        ChatterSeverity severity = _analyzer.DetectChatter(intervalMs, _settings);

        severity.Should().Be(expected);
    }

    [Fact]
    public void IsStuckKey_AfterThreshold_ReturnsTrue()
    {
        KeyEvent down = Down(NowMicroseconds() - 31_000_000);

        bool isStuck = _analyzer.IsStuckKey(down, DateTime.UtcNow, _settings);

        isStuck.Should().BeTrue();
    }

    [Fact]
    public void IsStuckKey_BeforeThreshold_ReturnsFalse()
    {
        KeyEvent down = Down(NowMicroseconds() - 29_000_000);

        bool isStuck = _analyzer.IsStuckKey(down, DateTime.UtcNow, _settings);

        isStuck.Should().BeFalse();
    }

    [Fact]
    public void IsStuckKey_KeyUpEvent_ReturnsFalse()
    {
        KeyEvent up = Up(NowMicroseconds() - 40_000_000);

        bool isStuck = _analyzer.IsStuckKey(up, DateTime.UtcNow, _settings);

        isStuck.Should().BeFalse();
    }

    [Fact]
    public void CalculateStatus_WithoutPresses_ReturnsNotTested()
    {
        KeyStatistics statistics = CreateStatistics();

        KeyStatus status = _analyzer.CalculateStatus(statistics, _settings);

        status.Should().Be(KeyStatus.NotTested);
    }

    [Fact]
    public void CalculateStatus_WithCriticalChatter_ReturnsCritical()
    {
        KeyStatistics statistics = CreateStatistics(new ChatterEvent(NowMicroseconds(), 5, ChatterSeverity.Critical));
        statistics.PressCount = 3;

        KeyStatus status = _analyzer.CalculateStatus(statistics, _settings);

        status.Should().Be(KeyStatus.Critical);
    }

    [Theory]
    [InlineData(ChatterSeverity.Moderate)]
    [InlineData(ChatterSeverity.Mild)]
    public void CalculateStatus_WithNonCriticalChatter_ReturnsWarning(ChatterSeverity severity)
    {
        KeyStatistics statistics = CreateStatistics(new ChatterEvent(NowMicroseconds(), 30, severity));
        statistics.PressCount = 2;

        KeyStatus status = _analyzer.CalculateStatus(statistics, _settings);

        status.Should().Be(KeyStatus.Warning);
    }

    [Fact]
    public void CalculateStatus_NoIssues_ReturnsOk()
    {
        KeyStatistics statistics = CreateStatistics();
        statistics.PressCount = 10;

        KeyStatus status = _analyzer.CalculateStatus(statistics, _settings);

        status.Should().Be(KeyStatus.Ok);
    }

    private static KeyEvent Down(long timestampMicroseconds) =>
        new(Guid.NewGuid(), 0x41, 0x1E, "A", timestampMicroseconds, true, null);

    private static KeyEvent Up(long timestampMicroseconds) =>
        new(Guid.NewGuid(), 0x41, 0x1E, "A", timestampMicroseconds, false, null);

    /// <summary>
    /// Текущее время в микросекундах QPC — тот же базис, что использует анализатор.
    /// </summary>
    private static long NowMicroseconds() =>
        Stopwatch.GetTimestamp() * 1_000_000 / Stopwatch.Frequency;

    private static KeyStatistics CreateStatistics(params ChatterEvent[] chatterEvents)
    {
        var statistics = new KeyStatistics
        {
            Key = new PhysicalKey(Guid.NewGuid(), 0x41, 0x1E, "A", "A", 3, 1.75, 1.0, Array.Empty<KeyboardLayout>()),
        };

        foreach (ChatterEvent chatter in chatterEvents)
        {
            statistics.ChatterEvents.Add(chatter);
        }

        return statistics;
    }
}
