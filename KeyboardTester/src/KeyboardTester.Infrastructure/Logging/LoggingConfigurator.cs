using Microsoft.Extensions.Logging;
using Serilog;

namespace KeyboardTester.Infrastructure.Logging;

/// <summary>
/// Настройка структурированного логирования через Serilog.
/// </summary>
public static class LoggingConfigurator
{
    /// <summary>
    /// Создаёт фабрику логгеров Microsoft.Extensions.Logging поверх Serilog
    /// с записью в файл в каталоге logs.
    /// </summary>
    public static ILoggerFactory CreateLoggerFactory(string logFilePath = "logs/keyboard-tester.log")
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        return LoggerFactory.Create(builder => builder.AddSerilog(Log.Logger, dispose: true));
    }
}
