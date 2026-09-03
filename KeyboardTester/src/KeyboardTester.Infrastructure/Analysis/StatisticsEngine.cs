using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Infrastructure.Analysis;

/// <summary>
/// Сервис учёта статистики нажатий клавиш.
/// </summary>
public sealed class StatisticsEngine : IStatisticsEngine
{
    private readonly ConcurrentDictionary<PhysicalKey, KeyStatistics> _statistics = new();
    private readonly ConcurrentDictionary<PhysicalKey, KeyEvent> _pendingKeyDowns = new();
    private readonly ConcurrentDictionary<PhysicalKey, long> _lastKeyDownTimestamps = new();
    private readonly IDebounceAnalyzer _debounceAnalyzer;

    /// <summary>Текущие пороги диагностики; заменяются через <see cref="UpdateSettings"/>.</summary>
    private DebounceSettings _settings;
    private readonly ILayoutProvider _layoutProvider;
    private readonly SynchronizationContext? _syncContext;

    /// <inheritdoc />
    public event EventHandler<KeyStatisticsUpdatedEventArgs>? StatisticsUpdated;

    /// <summary>
    /// Выбранная раскладка. Используется для сопоставления scan-code → <see cref="PhysicalKey"/>.
    /// </summary>
    public KeyboardLayout SelectedLayout { get; set; } = KeyboardLayout.Ansi104;

    /// <summary>
    /// Создаёт экземпляр <see cref="StatisticsEngine"/>.
    /// </summary>
    public StatisticsEngine(
        IDebounceAnalyzer debounceAnalyzer,
        DebounceSettings settings,
        ILayoutProvider layoutProvider,
        SynchronizationContext? syncContext = null)
    {
        _debounceAnalyzer = debounceAnalyzer ?? throw new ArgumentNullException(nameof(debounceAnalyzer));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _layoutProvider = layoutProvider ?? throw new ArgumentNullException(nameof(layoutProvider));
        _syncContext = syncContext ?? SynchronizationContext.Current;
    }

    /// <inheritdoc />
    public void RecordKeyDown(KeyEvent keyEvent)
    {
        ArgumentNullException.ThrowIfNull(keyEvent);

        PhysicalKey? physicalKey = ResolveKey(keyEvent.ScanCode);
        if (physicalKey == null)
        {
            return;
        }

        KeyStatistics stats = _statistics.GetOrAdd(
            physicalKey,
            static (k, _) => new KeyStatistics { Key = k },
            (object?)null);

        lock (stats)
        {
            stats.PressCount++;
            stats.LastUpdated = DateTime.Now;

            // Интервал считаем от предыдущего нажатия независимо от того, было ли
            // между ними отпускание: классический дребезг — это down→up→down.
            if (stats.PressCount > 1 && _lastKeyDownTimestamps.TryGetValue(physicalKey, out long lastDownMicroseconds))
            {
                double intervalMs = (keyEvent.TimestampMicroseconds - lastDownMicroseconds) / 1000.0;
                stats.PressIntervalsMs.Add(intervalMs);

                ChatterSeverity severity = _debounceAnalyzer.DetectChatter(intervalMs, _settings);
                if (severity != ChatterSeverity.None)
                {
                    stats.ChatterEvents.Add(new ChatterEvent(
                        keyEvent.TimestampMicroseconds,
                        intervalMs,
                        severity));
                }
            }

            _lastKeyDownTimestamps[physicalKey] = keyEvent.TimestampMicroseconds;
            _pendingKeyDowns[physicalKey] = keyEvent;

            TrimBuffer(stats.PressIntervalsMs);
            TrimBuffer(stats.HoldDurationsMs);
            TrimBuffer(stats.ChatterEvents);

            stats.Status = _debounceAnalyzer.CalculateStatus(stats, _settings);
        }

        RaiseStatisticsUpdated(physicalKey, stats);
    }

    /// <inheritdoc />
    public void RecordKeyUp(KeyEvent keyEvent)
    {
        ArgumentNullException.ThrowIfNull(keyEvent);

        PhysicalKey? physicalKey = ResolveKey(keyEvent.ScanCode);
        if (physicalKey == null)
        {
            return;
        }

        if (!_pendingKeyDowns.TryRemove(physicalKey, out KeyEvent? downEvent))
        {
            return;
        }

        KeyStatistics stats = _statistics.GetOrAdd(
            physicalKey,
            static (k, _) => new KeyStatistics { Key = k },
            (object?)null);

        lock (stats)
        {
            long durationMicroseconds = keyEvent.TimestampMicroseconds - downEvent.TimestampMicroseconds;
            double durationMs = durationMicroseconds / 1000.0;

            stats.TotalHoldTime += TimeSpan.FromMicroseconds(Math.Max(0, durationMicroseconds));
            stats.HoldDurationsMs.Add(durationMs);
            stats.LastUpdated = DateTime.Now;

            TrimBuffer(stats.HoldDurationsMs);
            stats.Status = _debounceAnalyzer.CalculateStatus(stats, _settings);
        }

        RaiseStatisticsUpdated(physicalKey, stats);
    }

    /// <inheritdoc />
    public KeyStatistics? GetStatistics(PhysicalKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _statistics.TryGetValue(key, out KeyStatistics? stats);
        return stats;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<PhysicalKey, KeyStatistics> GetAllStatistics()
    {
        return new ReadOnlyDictionary<PhysicalKey, KeyStatistics>(_statistics);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _statistics.Clear();
        _pendingKeyDowns.Clear();
        _lastKeyDownTimestamps.Clear();
    }

    /// <inheritdoc />
    public void ResetKey(PhysicalKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _statistics.TryRemove(key, out _);
        _pendingKeyDowns.TryRemove(key, out _);
        _lastKeyDownTimestamps.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public void UpdateSettings(DebounceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;

        // Буферы уже накопленных событий подрезаются под новый лимит,
        // чтобы окно наблюдения соответствовало актуальным настройкам.
        foreach (KeyStatistics stats in _statistics.Values)
        {
            lock (stats)
            {
                TrimBuffer(stats.PressIntervalsMs);
                TrimBuffer(stats.HoldDurationsMs);
                TrimBuffer(stats.ChatterEvents);
            }
        }
    }

    private PhysicalKey? ResolveKey(uint scanCode)
    {
        PhysicalKey? key = _layoutProvider.GetKeys(SelectedLayout).FirstOrDefault(k => k.ScanCode == scanCode);
        if (key != null)
        {
            return key;
        }

        foreach (KeyboardLayout layout in _layoutProvider.SupportedLayouts)
        {
            key = _layoutProvider.GetKeys(layout).FirstOrDefault(k => k.ScanCode == scanCode);
            if (key != null)
            {
                return key;
            }
        }

        return null;
    }

    private void TrimBuffer<T>(List<T> buffer)
    {
        int max = _settings.MaxEventsPerKey;
        while (buffer.Count > max)
        {
            buffer.RemoveAt(0);
        }
    }

    private void RaiseStatisticsUpdated(PhysicalKey key, KeyStatistics stats)
    {
        var args = new KeyStatisticsUpdatedEventArgs { Key = key, Statistics = stats };

        if (_syncContext != null)
        {
            _syncContext.Post(_ => StatisticsUpdated?.Invoke(this, args), null);
        }
        else
        {
            StatisticsUpdated?.Invoke(this, args);
        }
    }
}
