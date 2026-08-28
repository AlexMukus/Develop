using KeyboardTester.Core.Models;
using Microsoft.Extensions.Logging;

namespace KeyboardTester.Application.Services;

/// <summary>
/// Оркестратор тестовой сессии: приём событий клавиш и управление состоянием теста.
/// </summary>
public sealed class TestSessionService
{
    private readonly ILogger<TestSessionService> _logger;

    public TestSessionService(ILogger<TestSessionService> logger)
    {
        _logger = logger;
    }

    /// <summary>Тестовая сессия активна.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Начинает захват событий.</summary>
    public void Start()
    {
        IsRunning = true;
        _logger.LogInformation("Тестовая сессия запущена");
    }

    /// <summary>Останавливает захват событий.</summary>
    public void Stop()
    {
        IsRunning = false;
        _logger.LogInformation("Тестовая сессия остановлена");
    }

    /// <summary>Обрабатывает очередное событие клавиши.</summary>
    public void ProcessEvent(KeyEvent keyEvent)
    {
        if (!IsRunning)
        {
            return;
        }

        _logger.LogTrace(
            "Клавиша {KeyName} (VK {VirtualKeyCode}): {Direction} @ {Timestamp} мкс",
            keyEvent.KeyName, keyEvent.VirtualKeyCode,
            keyEvent.IsKeyDown ? "Down" : "Up", keyEvent.TimestampMicroseconds);
    }
}
