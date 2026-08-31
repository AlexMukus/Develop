using System.Diagnostics;
using System.Timers;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using Microsoft.Extensions.Logging;

namespace KeyboardTester.Application.Services;

/// <summary>
/// Оркестратор тестовой сессии: приём событий клавиш, управление состоянием и длительностью.
/// </summary>
public sealed class TestSessionService : IDisposable
{
    private readonly IStatisticsEngine _statisticsEngine;
    private readonly ILogger<TestSessionService> _logger;
    private readonly Stopwatch _stopwatch = new();
    private readonly System.Timers.Timer _durationTimer;

    private DateTime? _startTime;
    private KeyboardLayout _layout = KeyboardLayout.Ansi104;
    private TestSession? _currentSession;

    /// <summary>
    /// Создаёт экземпляр <see cref="TestSessionService"/>.
    /// </summary>
    public TestSessionService(
        IStatisticsEngine statisticsEngine,
        ILogger<TestSessionService> logger)
    {
        _statisticsEngine = statisticsEngine ?? throw new ArgumentNullException(nameof(statisticsEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _durationTimer = new System.Timers.Timer(100);
        _durationTimer.Elapsed += OnDurationTimerElapsed;
        _durationTimer.AutoReset = true;
    }

    /// <summary>
    /// Тестовая сессия активна.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Текущая длительность активной сессии.
    /// </summary>
    public TimeSpan SessionDuration => _stopwatch.Elapsed;

    /// <summary>
    /// Время начала текущей/последней сессии.
    /// </summary>
    public DateTime? StartTime => _startTime;

    /// <summary>
    /// Последняя завершённая сессия.
    /// </summary>
    public TestSession? CurrentSession => _currentSession;

    /// <summary>
    /// Событие запуска сессии.
    /// </summary>
    public event EventHandler? SessionStarted;

    /// <summary>
    /// Событие остановки сессии.
    /// </summary>
    public event EventHandler? SessionStopped;

    /// <summary>
    /// Событие изменения длительности сессии (примерно каждые 100 мс).
    /// </summary>
    public event EventHandler? DurationChanged;

    /// <summary>
    /// Начинает новую тестовую сессию.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _layout = _statisticsEngine.SelectedLayout;
        _startTime = DateTime.Now;
        _currentSession = null;
        _stopwatch.Restart();
        _durationTimer.Start();
        IsRunning = true;

        _logger.LogInformation("Тестовая сессия запущена");
        SessionStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Останавливает текущую тестовую сессию.
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        _stopwatch.Stop();
        _durationTimer.Stop();
        _currentSession = BuildSession();

        _logger.LogInformation("Тестовая сессия остановлена. Длительность: {Duration}", _stopwatch.Elapsed);
        SessionStopped?.Invoke(this, EventArgs.Empty);
        DurationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Сбрасывает текущую сессию и статистику.
    /// </summary>
    public void Reset()
    {
        if (IsRunning)
        {
            Stop();
        }

        _statisticsEngine.Reset();
        _currentSession = null;
        _startTime = null;
        _stopwatch.Reset();

        _logger.LogInformation("Тестовая сессия сброшена");
    }

    /// <summary>
    /// Обрабатывает очередное событие клавиши, если сессия активна.
    /// </summary>
    public void ProcessEvent(KeyEvent keyEvent)
    {
        ArgumentNullException.ThrowIfNull(keyEvent);

        if (!IsRunning)
        {
            return;
        }

        if (keyEvent.IsKeyDown)
        {
            _statisticsEngine.RecordKeyDown(keyEvent);
        }
        else
        {
            _statisticsEngine.RecordKeyUp(keyEvent);
        }

        _logger.LogTrace(
            "Клавиша {KeyName} (VK {VirtualKeyCode}): {Direction} @ {Timestamp} мкс",
            keyEvent.KeyName,
            keyEvent.VirtualKeyCode,
            keyEvent.IsKeyDown ? "Down" : "Up",
            keyEvent.TimestampMicroseconds);
    }

    /// <summary>
    /// Формирует сессию из текущего состояния без остановки теста.
    /// </summary>
    public TestSession BuildCurrentSession(string? name = null, string? notes = null)
    {
        DateTime start = _startTime ?? DateTime.Now;
        return new TestSession(
            Guid.NewGuid(),
            name ?? $"Сессия {start:yyyy-MM-dd HH:mm:ss}",
            start,
            DateTime.Now,
            _layout,
            _stopwatch.Elapsed,
            _statisticsEngine.GetAllStatistics(),
            notes);
    }

    private TestSession BuildSession()
    {
        return BuildCurrentSession();
    }

    private void OnDurationTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        DurationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _durationTimer.Stop();
        _durationTimer.Elapsed -= OnDurationTimerElapsed;
        _durationTimer.Dispose();
    }
}
