using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Dto;

/// <summary>
/// Результат анализа дребезга и залипания клавиши.
/// </summary>
public class DebounceResult
{
    /// <summary>Зарегистрированные события дребезга.</summary>
    public IReadOnlyList<ChatterEvent> ChatterEvents { get; init; } = Array.Empty<ChatterEvent>();

    /// <summary>Признак залипания клавиши.</summary>
    public bool IsStuckKey { get; init; }

    /// <summary>Длительность залипания, если оно обнаружено.</summary>
    public TimeSpan? StuckDuration { get; init; }

    /// <summary>Рекомендуемый статус клавиши.</summary>
    public KeyStatus RecommendedStatus { get; init; }
}
