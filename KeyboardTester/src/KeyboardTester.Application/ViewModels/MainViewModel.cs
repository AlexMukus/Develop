using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyboardTester.Application.Services;
using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using Microsoft.Extensions.Logging;

namespace KeyboardTester.Application.ViewModels;

/// <summary>
/// Главный ViewModel приложения: оркестрация захвата, статистики, истории и ghosting-теста.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private const int MaxChartPoints = 5000;
    private const int MaxGhostingResults = 1000;

    private readonly IRawInputCapture _rawInputCapture;
    private readonly IStatisticsEngine _statisticsEngine;
    private readonly IGhostingTestEngine _ghostingTestEngine;
    private readonly ISessionHistoryService _sessionHistoryService;
    private readonly ILayoutProvider _layoutProvider;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly TestSessionService _testSessionService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly SynchronizationContext? _syncContext;

    private readonly Dictionary<PhysicalKey, int> _intervalCounts = new();
    private readonly Dictionary<PhysicalKey, int> _durationCounts = new();
    private readonly Dictionary<PhysicalKey, KeyViewModel> _keyViewModels = new();

    /// <summary>
    /// Создаёт главный ViewModel.
    /// </summary>
    public MainViewModel(
        IRawInputCapture rawInputCapture,
        IStatisticsEngine statisticsEngine,
        IGhostingTestEngine ghostingTestEngine,
        ISessionHistoryService sessionHistoryService,
        ILayoutProvider layoutProvider,
        IThemeService themeService,
        ILocalizationService localizationService,
        TestSessionService testSessionService,
        ILogger<MainViewModel> logger,
        SynchronizationContext? syncContext = null)
    {
        _rawInputCapture = rawInputCapture ?? throw new ArgumentNullException(nameof(rawInputCapture));
        _statisticsEngine = statisticsEngine ?? throw new ArgumentNullException(nameof(statisticsEngine));
        _ghostingTestEngine = ghostingTestEngine ?? throw new ArgumentNullException(nameof(ghostingTestEngine));
        _sessionHistoryService = sessionHistoryService ?? throw new ArgumentNullException(nameof(sessionHistoryService));
        _layoutProvider = layoutProvider ?? throw new ArgumentNullException(nameof(layoutProvider));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _testSessionService = testSessionService ?? throw new ArgumentNullException(nameof(testSessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncContext = syncContext ?? SynchronizationContext.Current;

        _statisticsEngine.SelectedLayout = SelectedLayout;
        _currentTheme = _themeService.CurrentTheme;

        SubscribeEvents();
        RefreshLayout();
        RefreshHistory();
    }

    #region Observable properties

    /// <summary>
    /// Идёт ли основная тестовая сессия.
    /// </summary>
    [ObservableProperty]
    private bool _isSessionRunning;

    /// <summary>
    /// Выбранная раскладка клавиатуры.
    /// </summary>
    [ObservableProperty]
    private KeyboardLayout _selectedLayout = KeyboardLayout.Ansi104;

    /// <summary>
    /// Выбранная клавиша для детального просмотра.
    /// </summary>
    [ObservableProperty]
    private KeyViewModel? _selectedKey;

    /// <summary>
    /// Длительность текущей сессии.
    /// </summary>
    [ObservableProperty]
    private TimeSpan _sessionDuration;

    /// <summary>
    /// Общее количество нажатий по всем клавишам.
    /// </summary>
    [ObservableProperty]
    private int _totalPressCount;

    /// <summary>
    /// Количество проблемных клавиш (Warning / Critical).
    /// </summary>
    [ObservableProperty]
    private int _problematicKeysCount;

    /// <summary>
    /// Активен ли ghosting-тест.
    /// </summary>
    [ObservableProperty]
    private bool _isGhostingTestActive;

    /// <summary>
    /// Текущая тема оформления.
    /// </summary>
    [ObservableProperty]
    private AppTheme _currentTheme;

    /// <summary>
    /// Клавиши текущей раскладки.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<KeyViewModel> _keys = new();

    /// <summary>
    /// Точки графика интервалов между нажатиями.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<KeyDataPoint> _pressIntervalPoints = new();

    /// <summary>
    /// Точки графика времени удержания.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<KeyDataPoint> _holdDurationPoints = new();

    /// <summary>
    /// Результаты ghosting-теста.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GhostingTestResult> _ghostingResults = new();

    /// <summary>
    /// История сохранённых сессий.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TestSessionViewModel> _sessionHistory = new();

    /// <summary>
    /// Выбранная сессия в истории.
    /// </summary>
    [ObservableProperty]
    private TestSessionViewModel? _selectedSession;

    /// <summary>
    /// Последнее сравнение двух сессий.
    /// </summary>
    [ObservableProperty]
    private SessionComparisonResult? _lastComparison;

    #endregion

    #region Commands

    /// <summary>
    /// Начать основную тестовую сессию.
    /// </summary>
    [RelayCommand]
    private void StartTest()
    {
        _testSessionService.Start();
    }

    /// <summary>
    /// Остановить основную тестовую сессию.
    /// </summary>
    [RelayCommand]
    private void StopTest()
    {
        _testSessionService.Stop();
    }

    /// <summary>
    /// Сбросить текущую сессию, статистику и ghosting-тест.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        _testSessionService.Reset();
        _ghostingTestEngine.Reset();
        PressIntervalPoints.Clear();
        HoldDurationPoints.Clear();
        GhostingResults.Clear();
        _intervalCounts.Clear();
        _durationCounts.Clear();
        SessionDuration = TimeSpan.Zero;
        TotalPressCount = 0;
        ProblematicKeysCount = 0;

        foreach (KeyViewModel vm in Keys)
        {
            vm.Status = KeyStatus.NotTested;
            vm.IsPressed = false;
            vm.PressCount = 0;
        }

        _logger.LogInformation("Состояние приложения сброшено");
    }

    /// <summary>
    /// Начать ghosting-тест.
    /// </summary>
    [RelayCommand]
    private void StartGhostingTest()
    {
        _ghostingTestEngine.StartTest();
        IsGhostingTestActive = true;
        EnsureCaptureStarted();
    }

    /// <summary>
    /// Остановить ghosting-тест.
    /// </summary>
    [RelayCommand]
    private void StopGhostingTest()
    {
        _ghostingTestEngine.StopTest();
        IsGhostingTestActive = false;
        MaybeStopCapture();
    }

    /// <summary>
    /// Сохранить текущую сессию в историю.
    /// </summary>
    [RelayCommand]
    private void SaveSession()
    {
        TestSession session = _testSessionService.BuildCurrentSession();
        _sessionHistoryService.SaveSession(session);
        _logger.LogInformation("Сессия сохранена: {SessionName}", session.Name);
    }

    /// <summary>
    /// Удалить выбранную сессию из истории.
    /// </summary>
    [RelayCommand]
    private void DeleteSession()
    {
        if (SelectedSession == null)
        {
            return;
        }

        _sessionHistoryService.DeleteSession(SelectedSession.Session.Id);
        _logger.LogInformation("Сессия удалена: {SessionName}", SelectedSession.Session.Name);
    }

    /// <summary>
    /// Сравнить две последние сохранённые сессии.
    /// </summary>
    [RelayCommand]
    private void CompareSessions()
    {
        IReadOnlyList<TestSessionViewModel> history = SessionHistory;
        if (history.Count < 2)
        {
            _logger.LogWarning("Для сравнения нужно как минимум две сессии в истории.");
            return;
        }

        TestSession first = history[^2].Session;
        TestSession second = history[^1].Session;
        LastComparison = SessionComparison.Compare(first, second);
    }

    /// <summary>
    /// Открыть диалог настроек.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Открыть диалог "О программе".
    /// </summary>
    [RelayCommand]
    private void OpenAbout()
    {
        OpenAboutRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events

    /// <summary>
    /// Запрос на открытие диалога настроек.
    /// </summary>
    public event EventHandler? OpenSettingsRequested;

    /// <summary>
    /// Запрос на открытие диалога "О программе".
    /// </summary>
    public event EventHandler? OpenAboutRequested;

    #endregion

    #region Event handlers

    partial void OnSelectedLayoutChanged(KeyboardLayout value)
    {
        _statisticsEngine.SelectedLayout = value;
        RefreshLayout();
    }

    partial void OnCurrentThemeChanged(AppTheme value)
    {
        _themeService.SetTheme(value);
    }

    private void SubscribeEvents()
    {
        _rawInputCapture.KeyPressed += OnKeyPressed;
        _rawInputCapture.KeyReleased += OnKeyReleased;
        _statisticsEngine.StatisticsUpdated += OnStatisticsUpdated;
        _ghostingTestEngine.TestResultUpdated += OnGhostingResultUpdated;
        _sessionHistoryService.SessionsChanged += OnSessionsChanged;
        _testSessionService.SessionStarted += OnSessionStarted;
        _testSessionService.SessionStopped += OnSessionStopped;
        _testSessionService.DurationChanged += OnDurationChanged;
        _themeService.ThemeChanged += OnThemeChanged;
    }

    private void UnsubscribeEvents()
    {
        _rawInputCapture.KeyPressed -= OnKeyPressed;
        _rawInputCapture.KeyReleased -= OnKeyReleased;
        _statisticsEngine.StatisticsUpdated -= OnStatisticsUpdated;
        _ghostingTestEngine.TestResultUpdated -= OnGhostingResultUpdated;
        _sessionHistoryService.SessionsChanged -= OnSessionsChanged;
        _testSessionService.SessionStarted -= OnSessionStarted;
        _testSessionService.SessionStopped -= OnSessionStopped;
        _testSessionService.DurationChanged -= OnDurationChanged;
        _themeService.ThemeChanged -= OnThemeChanged;
    }

    private void OnKeyPressed(object? sender, RawKeyEventArgs e)
    {
        var keyEvent = new KeyEvent(
            Guid.NewGuid(),
            e.VirtualKeyCode,
            e.ScanCode,
            e.KeyName,
            e.TimestampMicroseconds,
            true,
            e.DevicePath);

        _testSessionService.ProcessEvent(keyEvent);

        KeyViewModel? vm = ResolveKeyViewModel(e.ScanCode);
        if (vm != null)
        {
            vm.IsPressed = true;
        }
    }

    private void OnKeyReleased(object? sender, RawKeyEventArgs e)
    {
        var keyEvent = new KeyEvent(
            Guid.NewGuid(),
            e.VirtualKeyCode,
            e.ScanCode,
            e.KeyName,
            e.TimestampMicroseconds,
            false,
            e.DevicePath);

        _testSessionService.ProcessEvent(keyEvent);

        KeyViewModel? vm = ResolveKeyViewModel(e.ScanCode);
        if (vm != null)
        {
            vm.IsPressed = false;
        }
    }

    private void OnStatisticsUpdated(object? sender, KeyStatisticsUpdatedEventArgs e)
    {
        Post(() =>
        {
            KeyViewModel? vm = ResolveKeyViewModel(e.Key.ScanCode);
            if (vm != null)
            {
                vm.Status = e.Statistics.Status;
                vm.PressCount = e.Statistics.PressCount;
            }

            UpdateChartPoints(e.Key, e.Statistics);
            RecalculateTotals();
        });
    }

    private void OnGhostingResultUpdated(object? sender, GhostingTestResult e)
    {
        Post(() =>
        {
            GhostingResults.Add(e);
            TrimCollection(GhostingResults, MaxGhostingResults);
        });
    }

    private void OnSessionsChanged(object? sender, EventArgs e)
    {
        Post(RefreshHistory);
    }

    private void OnSessionStarted(object? sender, EventArgs e)
    {
        Post(() =>
        {
            IsSessionRunning = true;
            EnsureCaptureStarted();
        });
    }

    private void OnSessionStopped(object? sender, EventArgs e)
    {
        Post(() =>
        {
            IsSessionRunning = false;
            MaybeStopCapture();
        });
    }

    private void OnDurationChanged(object? sender, EventArgs e)
    {
        Post(() => SessionDuration = _testSessionService.SessionDuration);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Post(() => CurrentTheme = _themeService.CurrentTheme);
    }

    #endregion

    #region Helpers

    private void RefreshLayout()
    {
        Keys.Clear();
        _keyViewModels.Clear();

        foreach (PhysicalKey key in _layoutProvider.GetKeys(SelectedLayout))
        {
            var vm = new KeyViewModel(key);
            Keys.Add(vm);
            _keyViewModels[key] = vm;
        }

        foreach (KeyStatistics stats in _statisticsEngine.GetAllStatistics().Values)
        {
            if (_keyViewModels.TryGetValue(stats.Key, out KeyViewModel? vm))
            {
                vm.Status = stats.Status;
                vm.PressCount = stats.PressCount;
            }
        }
    }

    private void RefreshHistory()
    {
        SessionHistory.Clear();
        foreach (TestSession session in _sessionHistoryService.GetAllSessions().OrderByDescending(s => s.StartTime))
        {
            SessionHistory.Add(new TestSessionViewModel(session));
        }
    }

    private void UpdateChartPoints(PhysicalKey key, KeyStatistics stats)
    {
        DateTime sessionStart = _testSessionService.StartTime ?? DateTime.Now;
        TimeSpan relativeTime = DateTime.Now - sessionStart;

        if (_intervalCounts.TryGetValue(key, out int lastIntervalCount))
        {
            for (int i = lastIntervalCount; i < stats.PressIntervalsMs.Count; i++)
            {
                PressIntervalPoints.Add(new KeyDataPoint(relativeTime, stats.PressIntervalsMs[i], key, stats.Status));
            }
        }
        else
        {
            foreach (double interval in stats.PressIntervalsMs)
            {
                PressIntervalPoints.Add(new KeyDataPoint(relativeTime, interval, key, stats.Status));
            }
        }

        _intervalCounts[key] = stats.PressIntervalsMs.Count;

        if (_durationCounts.TryGetValue(key, out int lastDurationCount))
        {
            for (int i = lastDurationCount; i < stats.HoldDurationsMs.Count; i++)
            {
                HoldDurationPoints.Add(new KeyDataPoint(relativeTime, stats.HoldDurationsMs[i], key, stats.Status));
            }
        }
        else
        {
            foreach (double duration in stats.HoldDurationsMs)
            {
                HoldDurationPoints.Add(new KeyDataPoint(relativeTime, duration, key, stats.Status));
            }
        }

        _durationCounts[key] = stats.HoldDurationsMs.Count;

        TrimCollection(PressIntervalPoints, MaxChartPoints);
        TrimCollection(HoldDurationPoints, MaxChartPoints);
    }

    private void RecalculateTotals()
    {
        int total = 0;
        int problematic = 0;

        foreach (KeyStatistics stats in _statisticsEngine.GetAllStatistics().Values)
        {
            total += stats.PressCount;
            if (stats.Status is KeyStatus.Warning or KeyStatus.Critical)
            {
                problematic++;
            }
        }

        TotalPressCount = total;
        ProblematicKeysCount = problematic;
    }

    private KeyViewModel? ResolveKeyViewModel(uint scanCode)
    {
        return _keyViewModels.Values.FirstOrDefault(vm => vm.PhysicalKey.ScanCode == scanCode);
    }

    private void EnsureCaptureStarted()
    {
        if (!_rawInputCapture.IsCapturing)
        {
            _rawInputCapture.StartCapture();
        }
    }

    private void MaybeStopCapture()
    {
        if (!IsSessionRunning && !IsGhostingTestActive && _rawInputCapture.IsCapturing)
        {
            _rawInputCapture.StopCapture();
        }
    }

    private static void TrimCollection<T>(ObservableCollection<T> collection, int maxCount)
    {
        while (collection.Count > maxCount)
        {
            collection.RemoveAt(0);
        }
    }

    private void Post(Action action)
    {
        if (_syncContext != null)
        {
            _syncContext.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    /// <summary>
    /// Отписывается от событий сервисов.
    /// </summary>
    public void Dispose()
    {
        UnsubscribeEvents();
    }

    #endregion
}
