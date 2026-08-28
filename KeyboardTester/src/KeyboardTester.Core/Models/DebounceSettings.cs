namespace KeyboardTester.Core.Models;

/// <summary>
/// Настройки дебаунса и порогов диагностики.
/// </summary>
public sealed record DebounceSettings(
    /// <summary>
    /// Порог критического дребезга в миллисекундах (меньше = критично).
    /// </summary>
    int CriticalThresholdMs = 20,

    /// <summary>
    /// Порог умеренного дребезга в миллисекундах.
    /// </summary>
    int WarningThresholdMs = 50,

    /// <summary>
    /// Порог лёгкого дребезга в миллисекундах.
    /// </summary>
    int MildThresholdMs = 80,

    /// <summary>
    /// Порог определения залипания клавиши в миллисекундах.
    /// </summary>
    int StuckKeyThresholdMs = 30000,

    /// <summary>
    /// Максимальное количество сохраняемых событий на одну клавишу.
    /// </summary>
    int MaxEventsPerKey = 10000);
