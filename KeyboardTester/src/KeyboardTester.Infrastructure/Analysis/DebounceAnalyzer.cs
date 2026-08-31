using System.Diagnostics;
using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Infrastructure.Analysis;

/// <summary>
/// Анализатор дребезга (chatter) и залипания клавиш.
/// </summary>
public sealed class DebounceAnalyzer : IDebounceAnalyzer
{
    /// <inheritdoc />
    public DebounceResult Analyze(IReadOnlyList<KeyEvent> events, DebounceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(settings);

        var chatterEvents = new List<ChatterEvent>();
        List<KeyEvent> keyDownEvents = events
            .Where(e => e.IsKeyDown)
            .OrderBy(e => e.TimestampMicroseconds)
            .ToList();

        for (int i = 1; i < keyDownEvents.Count; i++)
        {
            double intervalMs = (keyDownEvents[i].TimestampMicroseconds - keyDownEvents[i - 1].TimestampMicroseconds) / 1000.0;
            ChatterSeverity severity = DetectChatter(intervalMs, settings);

            if (severity != ChatterSeverity.None)
            {
                chatterEvents.Add(new ChatterEvent(
                    keyDownEvents[i].TimestampMicroseconds,
                    intervalMs,
                    severity));
            }
        }

        KeyEvent? lastKeyDown = keyDownEvents.LastOrDefault();
        bool isStuck = lastKeyDown != null && IsStuckKey(lastKeyDown, DateTime.UtcNow, settings);

        TimeSpan? stuckDuration = null;
        if (isStuck)
        {
            long durationMicroseconds = GetCurrentQpcMicroseconds() - lastKeyDown!.TimestampMicroseconds;
            stuckDuration = TimeSpan.FromMicroseconds(Math.Max(0, durationMicroseconds));
        }

        return new DebounceResult
        {
            ChatterEvents = chatterEvents,
            IsStuckKey = isStuck,
            StuckDuration = stuckDuration,
            RecommendedStatus = CalculateStatusFromChatter(chatterEvents, isStuck),
        };
    }

    /// <inheritdoc />
    public ChatterSeverity DetectChatter(double intervalMs, DebounceSettings settings)
    {
        if (intervalMs < settings.CriticalThresholdMs)
        {
            return ChatterSeverity.Critical;
        }

        if (intervalMs < settings.WarningThresholdMs)
        {
            return ChatterSeverity.Moderate;
        }

        if (intervalMs < settings.MildThresholdMs)
        {
            return ChatterSeverity.Mild;
        }

        return ChatterSeverity.None;
    }

    /// <inheritdoc />
    public bool IsStuckKey(KeyEvent lastKeyDown, DateTime now, DebounceSettings settings)
    {
        _ = now; // Параметр оставлен для совместимости интерфейса; используем QPC.

        if (!lastKeyDown.IsKeyDown)
        {
            return false;
        }

        long currentMicroseconds = GetCurrentQpcMicroseconds();
        long stuckDurationMicroseconds = currentMicroseconds - lastKeyDown.TimestampMicroseconds;
        return stuckDurationMicroseconds >= settings.StuckKeyThresholdMs * 1000L;
    }

    /// <inheritdoc />
    public KeyStatus CalculateStatus(KeyStatistics statistics, DebounceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        _ = settings;

        if (statistics.PressCount == 0)
        {
            return KeyStatus.NotTested;
        }

        return CalculateStatusFromChatter(statistics.ChatterEvents, isStuck: false);
    }

    private static KeyStatus CalculateStatusFromChatter(IReadOnlyList<ChatterEvent> chatterEvents, bool isStuck)
    {
        if (isStuck)
        {
            return KeyStatus.Critical;
        }

        if (chatterEvents.Any(e => e.Severity == ChatterSeverity.Critical))
        {
            return KeyStatus.Critical;
        }

        if (chatterEvents.Any(e => e.Severity is ChatterSeverity.Moderate or ChatterSeverity.Mild))
        {
            return KeyStatus.Warning;
        }

        return KeyStatus.Ok;
    }

    private static long GetCurrentQpcMicroseconds()
    {
        long frequency = Stopwatch.Frequency;
        long timestamp = Stopwatch.GetTimestamp();
        return (timestamp * 1_000_000) / frequency;
    }
}
