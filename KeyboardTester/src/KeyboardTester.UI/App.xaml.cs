using System.IO;
using System.Windows;
using System.Windows.Threading;
using KeyboardTester.Application;
using KeyboardTester.Application.ViewModels;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Analysis;
using KeyboardTester.Infrastructure.Input;
using KeyboardTester.Infrastructure.Layouts;
using KeyboardTester.Infrastructure.Logging;
using KeyboardTester.Infrastructure.Storage;
using KeyboardTester.UI.Services;
using KeyboardTester.UI.Themes;
using KeyboardTester.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace KeyboardTester.UI;

/// <summary>
/// Точка входа приложения KeyboardTester: настройка DI-контейнера,
/// темы оформления и запуск главного окна.
/// </summary>
public partial class App : System.Windows.Application
{
    private static readonly string AppDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeyboardTester");

    private ServiceProvider? _serviceProvider;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var settingsService = new SettingsService(AppDataDirectory);
        string logPath = Path.Combine(AppDataDirectory, "logs", "keyboard-tester.log");
        ILoggerFactory loggerFactory = LoggingConfigurator.CreateLoggerFactory(logPath);

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddLogging();
        services.AddSingleton(settingsService);
        services.AddSingleton(settingsService.Current.Debounce);

        // Infrastructure Layer. RawInputCapture и StatisticsEngine должны создаваться
        // на UI-потоке (HwndSource, SynchronizationContext) — граф зависимостей
        // вытягивается в OnStartup до показа окна.
        services.AddSingleton<INativeMethods, WindowsNativeMethods>();
        services.AddSingleton<IRawInputCapture>(sp => new RawInputCapture(
            sp.GetRequiredService<ILogger<RawInputCapture>>(),
            sp.GetRequiredService<INativeMethods>(),
            SynchronizationContext.Current));
        services.AddSingleton<IDebounceAnalyzer, DebounceAnalyzer>();
        services.AddSingleton<ILayoutProvider, LayoutProvider>();
        services.AddSingleton<IStatisticsEngine>(sp => new StatisticsEngine(
            sp.GetRequiredService<IDebounceAnalyzer>(),
            sp.GetRequiredService<DebounceSettings>(),
            sp.GetRequiredService<ILayoutProvider>(),
            SynchronizationContext.Current));
        services.AddSingleton<IGhostingTestEngine, GhostingTestEngine>();
        services.AddSingleton<ISessionHistoryService>(sp => new SessionHistoryService(
            logger: sp.GetRequiredService<ILogger<SessionHistoryService>>()));

        // Presentation-сервисы.
        services.AddSingleton<ThemeManager>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeManager>());
        services.AddSingleton<ILocalizationService, LocalizationServiceStub>();

        // Application Layer (TestSessionService, MainViewModel).
        services.AddKeyboardTesterApplication();

        _serviceProvider = services.BuildServiceProvider();

        // Разрешение MainViewModel вытягивает весь граф зависимостей на UI-потоке.
        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        // Тема применяется до показа окна; MainViewModel узнает о смене через событие.
        var themeManager = _serviceProvider.GetRequiredService<ThemeManager>();
        themeManager.SetTheme(settingsService.Current.Theme);

        var mainWindow = new MainWindow(viewModel, settingsService, themeManager);
        MainWindow = mainWindow;
        mainWindow.Show();

        ILogger<App> logger = loggerFactory.CreateLogger<App>();
        logger.LogInformation("Приложение запущено. Лог: {LogPath}", logPath);
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        _serviceProvider?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _serviceProvider?.GetService<ILogger<App>>()
            ?.LogError(e.Exception, "Необработанное исключение в UI-потоке");

        MessageBox.Show(
            $"Произошла непредвиденная ошибка:\n{e.Exception.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
