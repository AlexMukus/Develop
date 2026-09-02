using System.Text.Json.Serialization;
using KeyboardTester.Core.Enums;

namespace KeyboardTester.Core.Models;

/// <summary>
/// Статистика нажатий и диагностики для конкретной физической клавиши.
/// </summary>
public sealed class KeyStatistics
{
    /// <summary>
    /// Клавиша, к которой относится статистика.
    /// </summary>
    public required PhysicalKey Key { get; init; }

    /// <summary>
    /// Общее количество нажатий.
    /// </summary>
    public int PressCount { get; set; }

    /// <summary>
    /// Общее время удержания клавиши.
    /// </summary>
    public TimeSpan TotalHoldTime { get; set; }

    /// <summary>
    /// Интервалы между нажатиями в миллисекундах.
    /// Populate обязателен: без сеттера System.Text.Json пропускает свойство
    /// при десериализации, и история сессий теряет интервалы.
    /// </summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public List<double> PressIntervalsMs { get; } = [];

    /// <summary>
    /// Время удержания каждого нажатия в миллисекундах.
    /// <see cref="JsonObjectCreationHandlingAttribute"/> — см. <see cref="PressIntervalsMs"/>.
    /// </summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public List<double> HoldDurationsMs { get; } = [];

    /// <summary>
    /// Зарегистрированные события дребезга.
    /// <see cref="JsonObjectCreationHandlingAttribute"/> — см. <see cref="PressIntervalsMs"/>.
    /// </summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public List<ChatterEvent> ChatterEvents { get; } = [];

    /// <summary>
    /// Текущий статус клавиши.
    /// </summary>
    public KeyStatus Status { get; set; } = KeyStatus.NotTested;

    /// <summary>
    /// Время последнего обновления статистики.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Средний интервал между нажатиями в миллисекундах.
    /// </summary>
    public double AverageIntervalMs => PressIntervalsMs.Count > 0 ? PressIntervalsMs.Average() : 0;

    /// <summary>
    /// Среднее время удержания в миллисекундах.
    /// </summary>
    public double AverageHoldDurationMs => HoldDurationsMs.Count > 0 ? HoldDurationsMs.Average() : 0;

    /// <summary>
    /// Минимальный интервал между нажатиями в миллисекундах.
    /// </summary>
    public double MinIntervalMs => PressIntervalsMs.Count > 0 ? PressIntervalsMs.Min() : double.MaxValue;
}
