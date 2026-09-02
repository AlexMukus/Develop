using System.IO;
using FluentAssertions;
using KeyboardTester.Application.ViewModels;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Layouts;
using KeyboardTester.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KeyboardTester.Integration.Tests.Session;

/// <summary>
/// Тесты ViewModel сохранённой сессии для панели просмотра истории (v1.1.0, пункт 2):
/// список статистики клавиш, сводные счётчики и корректность конструирования.
/// </summary>
public class TestSessionViewModelTests
{
    private static TestSession CreateSession()
    {
        var layoutProvider = new LayoutProvider();
        PhysicalKey keyA = layoutProvider.GetKeys(KeyboardLayout.Ansi104).Single(k => k.ScanCode == 0x1E);
        PhysicalKey keyS = layoutProvider.GetKeys(KeyboardLayout.Ansi104).Single(k => k.ScanCode == 0x1F);

        var statsA = new KeyStatistics
        {
            Key = keyA,
            PressCount = 7,
            Status = KeyStatus.Warning,
        };
        statsA.PressIntervalsMs.AddRange(new double[] { 60, 80 });
        statsA.HoldDurationsMs.AddRange(new double[] { 50, 40 });

        var statsS = new KeyStatistics
        {
            Key = keyS,
            PressCount = 30,
            Status = KeyStatus.Ok,
        };
        statsS.HoldDurationsMs.Add(20);

        var statistics = new Dictionary<PhysicalKey, KeyStatistics> { [keyA] = statsA, [keyS] = statsS };

        return new TestSession(
            Id: Guid.NewGuid(),
            Name: "Test session",
            StartTime: DateTime.Now.AddMinutes(-5),
            EndTime: DateTime.Now,
            Layout: KeyboardLayout.Ansi104,
            Duration: TimeSpan.FromMinutes(5),
            Statistics: statistics,
            Notes: "заметка");
    }

    [Fact]
    public void StatisticsList_ContainsAllKeysFromSession()
    {
        TestSession session = CreateSession();

        var vm = new TestSessionViewModel(session);

        vm.StatisticsList.Should().HaveCount(2);
        vm.StatisticsList.Should().OnlyContain(s => session.Statistics.Values.Contains(s));
    }

    [Fact]
    public void StatisticsList_SortedByPressCountDescending()
    {
        TestSession session = CreateSession();

        var vm = new TestSessionViewModel(session);

        vm.StatisticsList.Select(s => s.PressCount)
            .Should().BeInDescendingOrder();
    }

    [Fact]
    public void SummaryCounters_MatchSessionData()
    {
        TestSession session = CreateSession();

        var vm = new TestSessionViewModel(session);

        vm.TotalPressCount.Should().Be(37);
        vm.ProblematicKeysCount.Should().Be(1); // только Warning(A)
        vm.DisplayName.Should().Be("Test session");
    }

    [Fact]
    public void Constructor_NullSession_Throws()
    {
        Action act = () => new TestSessionViewModel(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ViewModel_CreatedFromRestoredHistory_KeepsStatisticsList()
    {
        // Сессия проходит полный цикл: сохранение в JSON → загрузка → ViewModel.
        string tempDir = Path.Combine(Path.GetTempPath(), "kt-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            TestSession original = CreateSession();

            TestSession restored;
            using (var history = new SessionHistoryService(tempDir, NullLogger<SessionHistoryService>.Instance))
            {
                history.SaveSession(original);
                restored = history.GetSession(original.Id).Should().NotBeNull().And.Subject.As<TestSession>();
            }

            var vm = new TestSessionViewModel(restored);

            vm.StatisticsList.Should().HaveCount(2);
            vm.TotalPressCount.Should().Be(original.Statistics.Values.Sum(s => s.PressCount));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
