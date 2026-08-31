using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Application.ViewModels;

/// <summary>
/// Результат сравнения двух тестовых сессий.
/// </summary>
public sealed record SessionComparisonResult(
    TestSession First,
    TestSession Second,
    int PressCountDelta,
    int ProblematicKeysDelta,
    IReadOnlyList<KeyStatusChange> KeyStatusChanges);

/// <summary>
/// Изменение статуса конкретной клавиши между двумя сессиями.
/// </summary>
public sealed record KeyStatusChange(
    PhysicalKey Key,
    KeyStatus OldStatus,
    KeyStatus NewStatus);

/// <summary>
/// Статический помощник для сравнения сессий.
/// </summary>
public static class SessionComparison
{
    /// <summary>
    /// Сравнивает две сессии: считает дельты и изменения статусов клавиш.
    /// </summary>
    public static SessionComparisonResult Compare(TestSession first, TestSession second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        int firstPressCount = first.Statistics.Values.Sum(s => s.PressCount);
        int secondPressCount = second.Statistics.Values.Sum(s => s.PressCount);

        int firstProblematic = first.Statistics.Values.Count(s => s.Status is KeyStatus.Warning or KeyStatus.Critical);
        int secondProblematic = second.Statistics.Values.Count(s => s.Status is KeyStatus.Warning or KeyStatus.Critical);

        var changes = new List<KeyStatusChange>();
        foreach ((PhysicalKey key, KeyStatistics secondStats) in second.Statistics)
        {
            if (first.Statistics.TryGetValue(key, out KeyStatistics? firstStats) && firstStats.Status != secondStats.Status)
            {
                changes.Add(new KeyStatusChange(key, firstStats.Status, secondStats.Status));
            }
        }

        return new SessionComparisonResult(
            first,
            second,
            secondPressCount - firstPressCount,
            secondProblematic - firstProblematic,
            changes);
    }
}
