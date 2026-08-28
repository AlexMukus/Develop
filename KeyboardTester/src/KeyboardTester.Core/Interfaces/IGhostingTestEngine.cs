using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Сервис тестирования на ghosting и NKRO.
/// </summary>
public interface IGhostingTestEngine
{
    /// <summary>Событие обновления результата тестирования.</summary>
    event EventHandler<GhostingTestResult>? TestResultUpdated;

    /// <summary>Идёт ли в данный момент тест.</summary>
    bool IsRunning { get; }

    /// <summary>Список клавиш, удерживаемых в текущий момент.</summary>
    IReadOnlyList<PhysicalKey> CurrentlyPressedKeys { get; }

    /// <summary>Начать тестирование.</summary>
    void StartTest();

    /// <summary>Остановить тестирование.</summary>
    void StopTest();

    /// <summary>Сбросить состояние теста.</summary>
    void Reset();
}
