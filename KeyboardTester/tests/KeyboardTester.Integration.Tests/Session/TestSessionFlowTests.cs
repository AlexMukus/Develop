using FluentAssertions;
using KeyboardTester.Application.Services;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Analysis;
using KeyboardTester.Infrastructure.Layouts;
using KeyboardTester.Infrastructure.Storage;
using KeyboardTester.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KeyboardTester.Integration.Tests.Session;

/// <summary>
/// Сквозной сценарий: симуляция ввода → статистика → сессия → сохранение/загрузка истории.
/// Также покрывает GhostingTestEngine и JSON-конвертер словаря статистики.
/// </summary>
public class TestSessionFlowTests
{
    private const uint ScanA = 0x1E;
    private const uint ScanS = 0x1F;

    [Fact]
    public void FullSession_FlowTest()
    {
        // Arrange: реальные сервисы, синхронный контекст отсутствует.
        var layoutProvider = new LayoutProvider();
        IStatisticsEngine statisticsEngine = new StatisticsEngine(
            new DebounceAnalyzer(), new DebounceSettings(), layoutProvider);
        using var sessionService = new TestSessionService(statisticsEngine, NullLogger<TestSessionService>.Instance);

        string tempDir = Path.Combine(Path.GetTempPath(), "kt-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            // Act: полный цикл сессии.
            sessionService.Start();
            sessionService.IsRunning.Should().BeTrue();

            // A: down@0, up@50 (удержание 50 мс), down@60 (интервал 60 мс → Mild), up@110.
            sessionService.ProcessEvent(Down(ScanA, 0));
            sessionService.ProcessEvent(Up(ScanA, 50_000));
            sessionService.ProcessEvent(Down(ScanA, 60_000));
            sessionService.ProcessEvent(Up(ScanA, 110_000));

            // S: одиночное нажатие без проблем.
            sessionService.ProcessEvent(Down(ScanS, 5_000));
            sessionService.ProcessEvent(Up(ScanS, 15_000));

            Thread.Sleep(20);
            sessionService.Stop();

            // Assert: сессия сформирована.
            TestSession? session = sessionService.CurrentSession;
            session.Should().NotBeNull();
            session!.Layout.Should().Be(KeyboardLayout.Ansi104);
            session.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(10));
            session.Statistics.Should().HaveCount(2);

            PhysicalKey keyA = layoutProvider.GetKeys(KeyboardLayout.Ansi104).Single(k => k.ScanCode == ScanA);
            KeyStatistics statsA = session.Statistics[keyA];
            statsA.PressCount.Should().Be(2);
            statsA.PressIntervalsMs.Should().ContainSingle().Which.Should().Be(60);
            statsA.HoldDurationsMs.Should().HaveCount(2).And.ContainInOrder(new double[] { 50, 50 });
            statsA.ChatterEvents.Should().ContainSingle().Which.Severity.Should().Be(ChatterSeverity.Mild);
            statsA.Status.Should().Be(KeyStatus.Warning);

            PhysicalKey keyS = layoutProvider.GetKeys(KeyboardLayout.Ansi104).Single(k => k.ScanCode == ScanS);
            KeyStatistics statsS = session.Statistics[keyS];
            statsS.PressCount.Should().Be(1);
            statsS.HoldDurationsMs.Should().ContainSingle().Which.Should().Be(10);
            statsS.ChatterEvents.Should().BeEmpty();
            statsS.Status.Should().Be(KeyStatus.Ok);

            // Act: сохранение и повторная загрузка истории (проверка JSON-конвертера).
            using (var history = new SessionHistoryService(tempDir))
            {
                history.SaveSession(session);
            }

            using (var reloaded = new SessionHistoryService(tempDir))
            {
                IReadOnlyList<TestSession> sessions = reloaded.GetAllSessions();
                sessions.Should().ContainSingle();

                TestSession restored = reloaded.GetSession(session.Id)!;
                restored.Name.Should().Be(session.Name);
                restored.Layout.Should().Be(KeyboardLayout.Ansi104);
                restored.Statistics.Should().HaveCount(2);

                KeyStatistics restoredA = restored.Statistics[keyA];
                restoredA.PressCount.Should().Be(2);
                restoredA.PressIntervalsMs.Should().ContainSingle().Which.Should().Be(60);
                restoredA.HoldDurationsMs.Should().HaveCount(2);
                restoredA.ChatterEvents.Should().ContainSingle().Which.Severity.Should().Be(ChatterSeverity.Mild);
                restoredA.AverageHoldDurationMs.Should().Be(50);
                restoredA.Status.Should().Be(KeyStatus.Warning);

                // Act: удаление.
                reloaded.DeleteSession(session.Id);
                reloaded.GetAllSessions().Should().BeEmpty();
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Временный каталог мог быть уже занят другим процессом — не влияет на результат.
            }
        }
    }

    [Fact]
    public void ProcessEvent_WhenSessionNotRunning_IsIgnored()
    {
        var layoutProvider = new LayoutProvider();
        IStatisticsEngine statisticsEngine = new StatisticsEngine(
            new DebounceAnalyzer(), new DebounceSettings(), layoutProvider);
        using var sessionService = new TestSessionService(statisticsEngine, NullLogger<TestSessionService>.Instance);

        sessionService.ProcessEvent(Down(ScanA, 0));

        statisticsEngine.GetAllStatistics().Should().BeEmpty();
    }

    [Fact]
    public void SessionService_Reset_ClearsStatistics()
    {
        var layoutProvider = new LayoutProvider();
        IStatisticsEngine statisticsEngine = new StatisticsEngine(
            new DebounceAnalyzer(), new DebounceSettings(), layoutProvider);
        using var sessionService = new TestSessionService(statisticsEngine, NullLogger<TestSessionService>.Instance);

        sessionService.Start();
        sessionService.ProcessEvent(Down(ScanA, 0));
        sessionService.Reset();

        sessionService.IsRunning.Should().BeFalse();
        sessionService.CurrentSession.Should().BeNull();
        statisticsEngine.GetAllStatistics().Should().BeEmpty();
    }

    [Fact]
    public void GhostingEngine_DetectsNkro()
    {
        using var capture = new FakeRawInputCapture();
        using var engine = new GhostingTestEngine(capture, new LayoutProvider());
        var results = new List<GhostingTestResult>();
        engine.TestResultUpdated += (_, result) => results.Add(result);

        // Act: последовательно зажимаем 8 клавиш, затем отпускаем.
        engine.StartTest();
        uint[] scanCodes = { 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };

        foreach ((uint scanCode, int index) in scanCodes.Select((sc, i) => (sc, i)))
        {
            capture.Press(scanCode, index * 1_000);
        }

        foreach ((uint scanCode, int index) in scanCodes.Select((sc, i) => (sc, i)))
        {
            capture.Release(scanCode, 100_000 + index * 1_000);
        }

        // Assert: каждое нажатие зафиксировано, NKRO обнаружен (> 6 клавиш).
        results.Should().HaveCount(8);
        GhostingTestResult last = results.Last();
        last.MaxSimultaneousKeys.Should().Be(8);
        last.IsNKeyRollover.Should().BeTrue();
        last.RegisteredKeys.Should().HaveCount(8);
        engine.CurrentlyPressedKeys.Should().BeEmpty();
    }

    [Fact]
    public void GhostingEngine_SixKeys_NotNkro()
    {
        using var capture = new FakeRawInputCapture();
        using var engine = new GhostingTestEngine(capture, new LayoutProvider());
        var results = new List<GhostingTestResult>();
        engine.TestResultUpdated += (_, result) => results.Add(result);

        engine.StartTest();
        uint[] scanCodes = { 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

        foreach ((uint scanCode, int index) in scanCodes.Select((sc, i) => (sc, i)))
        {
            capture.Press(scanCode, index * 1_000);
        }

        results.Last().MaxSimultaneousKeys.Should().Be(6);
        results.Last().IsNKeyRollover.Should().BeFalse();
    }

    [Fact]
    public void GhostingEngine_IgnoresUnknownScanCode()
    {
        using var capture = new FakeRawInputCapture();
        using var engine = new GhostingTestEngine(capture, new LayoutProvider());
        var results = new List<GhostingTestResult>();
        engine.TestResultUpdated += (_, result) => results.Add(result);

        engine.StartTest();
        capture.Press(0xABCD, 0);

        results.Should().BeEmpty();
    }

    private static KeyEvent Down(uint scanCode, long timestampMicroseconds) =>
        new(Guid.NewGuid(), 0, scanCode, "K", timestampMicroseconds, true, null);

    private static KeyEvent Up(uint scanCode, long timestampMicroseconds) =>
        new(Guid.NewGuid(), 0, scanCode, "K", timestampMicroseconds, false, null);
}
