using FluentAssertions;
using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Analysis;
using KeyboardTester.Infrastructure.Layouts;
using Moq;
using Xunit;

namespace KeyboardTester.Core.Tests.Analysis;

/// <summary>
/// Тесты движка статистики <see cref="StatisticsEngine"/>.
/// </summary>
public class StatisticsEngineTests
{
    private const uint ScanA = 0x1E;
    private const uint ScanS = 0x1F;

    private readonly Mock<IDebounceAnalyzer> _mockAnalyzer = new();

    private StatisticsEngine CreateEngine(DebounceSettings? settings = null) =>
        new(_mockAnalyzer.Object, settings ?? new DebounceSettings(), new LayoutProvider(), new InlineSyncContext());

    [Fact]
    public void RecordKeyDown_IncrementsPressCount()
    {
        StatisticsEngine engine = CreateEngine();

        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyDown(Down(ScanA, 100_000));

        KeyStatistics statistics = engine.GetStatistics(GetKey(engine, ScanA))!;
        statistics.PressCount.Should().Be(2);
    }

    [Fact]
    public void RecordKeyDown_CalculatesInterval_WithKeyUpBetween()
    {
        // Классический дребезг: down → up → down. Интервал считается между нажатиями.
        StatisticsEngine engine = CreateEngine();

        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyUp(Up(ScanA, 50_000));
        engine.RecordKeyDown(Down(ScanA, 100_000));

        KeyStatistics statistics = engine.GetStatistics(GetKey(engine, ScanA))!;
        statistics.PressIntervalsMs.Should().ContainSingle().Which.Should().Be(100);
    }

    [Fact]
    public void RecordKeyDown_CalculatesInterval_WithoutKeyUpBetween()
    {
        // Повторное нажатие без отпускания (запавший контакт).
        StatisticsEngine engine = CreateEngine();

        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyDown(Down(ScanA, 100_000));

        KeyStatistics statistics = engine.GetStatistics(GetKey(engine, ScanA))!;
        statistics.PressIntervalsMs.Should().ContainSingle().Which.Should().Be(100);
    }

    [Fact]
    public void RecordKeyDown_WithRealAnalyzer_RecordsChatterEvent()
    {
        // Сквозной сценарий: интервал 10 мс → критический дребезг → статус Critical.
        var engine = new StatisticsEngine(
            new DebounceAnalyzer(), new DebounceSettings(), new LayoutProvider(), new InlineSyncContext());

        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyUp(Up(ScanA, 8_000));
        engine.RecordKeyDown(Down(ScanA, 10_000));

        KeyStatistics statistics = engine.GetStatistics(GetKey(engine, ScanA))!;
        statistics.ChatterEvents.Should().ContainSingle().Which.Severity.Should().Be(ChatterSeverity.Critical);
        statistics.Status.Should().Be(KeyStatus.Critical);
        statistics.PressCount.Should().Be(2);
    }

    [Fact]
    public void RecordKeyUp_CalculatesHoldDuration()
    {
        StatisticsEngine engine = CreateEngine();

        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyUp(Up(ScanA, 80_000));

        KeyStatistics statistics = engine.GetStatistics(GetKey(engine, ScanA))!;
        statistics.HoldDurationsMs.Should().ContainSingle().Which.Should().Be(80);
        statistics.TotalHoldTime.Should().Be(TimeSpan.FromMilliseconds(80));
        statistics.AverageHoldDurationMs.Should().Be(80);
    }

    [Fact]
    public void RecordKeyUp_WithoutKeyDown_IsIgnored()
    {
        StatisticsEngine engine = CreateEngine();
        int eventCount = 0;
        engine.StatisticsUpdated += (_, _) => eventCount++;

        engine.RecordKeyUp(Up(ScanA, 0));

        engine.GetAllStatistics().Should().BeEmpty();
        eventCount.Should().Be(0);
    }

    [Fact]
    public void RecordKeyDown_And_KeyUp_PairingWorks()
    {
        StatisticsEngine engine = CreateEngine();

        // Первая пара.
        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyUp(Up(ScanA, 100_000));

        // Лишнее отпускание игнорируется.
        engine.RecordKeyUp(Up(ScanA, 150_000));

        // Вторая пара: интервал считается от первого нажатия.
        engine.RecordKeyDown(Down(ScanA, 200_000));
        engine.RecordKeyUp(Up(ScanA, 260_000));

        KeyStatistics statistics = engine.GetStatistics(GetKey(engine, ScanA))!;
        statistics.PressCount.Should().Be(2);
        statistics.PressIntervalsMs.Should().ContainSingle().Which.Should().Be(200);
        statistics.HoldDurationsMs.Should().HaveCount(2).And.ContainInOrder(new double[] { 100, 60 });
        statistics.TotalHoldTime.Should().Be(TimeSpan.FromMilliseconds(160));
    }

    [Fact]
    public void RecordKeyDown_UnknownScanCode_IsIgnored()
    {
        StatisticsEngine engine = CreateEngine();
        int eventCount = 0;
        engine.StatisticsUpdated += (_, _) => eventCount++;

        engine.RecordKeyDown(Down(0xFEFE, 0));

        engine.GetAllStatistics().Should().BeEmpty();
        eventCount.Should().Be(0);
    }

    [Fact]
    public void Reset_ClearsAllData()
    {
        StatisticsEngine engine = CreateEngine();
        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyDown(Down(ScanS, 1_000));

        engine.Reset();

        engine.GetAllStatistics().Should().BeEmpty();

        // После сброса первое нажатие снова без интервала.
        engine.RecordKeyDown(Down(ScanA, 5_000_000));
        KeyStatistics statistics = engine.GetStatistics(GetKey(engine, ScanA))!;
        statistics.PressCount.Should().Be(1);
        statistics.PressIntervalsMs.Should().BeEmpty();
    }

    [Fact]
    public void ResetKey_RemovesOnlyGivenKey()
    {
        StatisticsEngine engine = CreateEngine();
        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyDown(Down(ScanS, 1_000));

        engine.ResetKey(GetKey(engine, ScanA));

        engine.GetAllStatistics().Should().ContainSingle()
            .Which.Key.ScanCode.Should().Be(ScanS);
    }

    [Fact]
    public void GetStatistics_NonExistentKey_ReturnsNull()
    {
        StatisticsEngine engine = CreateEngine();

        var unknownKey = new PhysicalKey(Guid.NewGuid(), 0, 0xFEFE, "?", "?", 0, 0, 1.0, Array.Empty<KeyboardLayout>());

        engine.GetStatistics(unknownKey).Should().BeNull();
    }

    [Fact]
    public void TrimBuffer_RespectsMaxEvents()
    {
        // Интервалы растут на 1..11 мс; лимит буфера — 5, останутся последние пять: 7..11 мс.
        StatisticsEngine engine = CreateEngine(new DebounceSettings(MaxEventsPerKey: 5));

        long timestamp = 0;
        for (int i = 1; i <= 11; i++)
        {
            timestamp += i * 1_000;
            engine.RecordKeyDown(Down(ScanA, timestamp));
        }

        KeyStatistics statistics = engine.GetStatistics(GetKey(engine, ScanA))!;
        // 11 нажатий: первое без интервала, затем 10 интервалов (2..11 мс).
        statistics.PressCount.Should().Be(11);
        statistics.PressIntervalsMs.Should().HaveCount(5);
        statistics.PressIntervalsMs.Min().Should().Be(7);
        statistics.PressIntervalsMs.Max().Should().Be(11);
        statistics.PressIntervalsMs.Sum().Should().Be(45);
        statistics.AverageIntervalMs.Should().Be(9);
    }

    [Fact]
    public void StatisticsUpdated_RaisedForEachRecordedEvent()
    {
        StatisticsEngine engine = CreateEngine();
        var updates = new List<KeyStatisticsUpdatedEventArgs>();
        engine.StatisticsUpdated += (_, args) => updates.Add(args);

        engine.RecordKeyDown(Down(ScanA, 0));
        engine.RecordKeyUp(Up(ScanA, 10_000));
        engine.RecordKeyDown(Down(ScanA, 200_000));

        updates.Should().HaveCount(3);
        updates.Should().OnlyContain(u => u.Key.ScanCode == ScanA);
        updates[2].Statistics.PressCount.Should().Be(2);
    }

    /// <summary>
    /// Возвращает ключ раскладки по скан-коду, чтобы работать с тем же экземпляром, что и движок.
    /// </summary>
    private static PhysicalKey GetKey(StatisticsEngine engine, uint scanCode)
    {
        return new LayoutProvider().GetKeys(engine.SelectedLayout).Single(k => k.ScanCode == scanCode);
    }

    private static KeyEvent Down(uint scanCode, long timestampMicroseconds) =>
        new(Guid.NewGuid(), 0, scanCode, "K", timestampMicroseconds, true, null);

    private static KeyEvent Up(uint scanCode, long timestampMicroseconds) =>
        new(Guid.NewGuid(), 0, scanCode, "K", timestampMicroseconds, false, null);

    /// <summary>
    /// Контекст с синхронным Post — гарантирует, что события движка приходят
    /// в потоке теста независимо от окружения исполнителя.
    /// </summary>
    private sealed class InlineSyncContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state)
        {
            callback(state);
        }
    }
}
