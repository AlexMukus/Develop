using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Сервис анализа дребезга (chatter) и залипания клавиш.
/// </summary>
public interface IDebounceAnalyzer
{
    /// <summary>
    /// Проанализировать последовательность событий клавиши.
    /// </summary>
    /// <param name="events">События клавиши.</param>
    /// <param name="settings">Пороговые настройки.</param>
    /// <returns>Результат анализа.</returns>
    DebounceResult Analyze(IReadOnlyList<KeyEvent> events, DebounceSettings settings);

    /// <summary>
    /// Определить степень дребезга по интервалу между событиями.
    /// </summary>
    /// <param name="intervalMs">Интервал в миллисекундах.</param>
    /// <param name="settings">Пороговые настройки.</param>
    /// <returns>Степень дребезга.</returns>
    ChatterSeverity DetectChatter(double intervalMs, DebounceSettings settings);

    /// <summary>
    /// Проверить, является ли клавиша залипшей.
    /// </summary>
    /// <param name="lastKeyDown">Последнее событие нажатия.</param>
    /// <param name="now">Текущее время.</param>
    /// <param name="settings">Пороговые настройки.</param>
    /// <returns>true, если клавиша считается залипшей.</returns>
    bool IsStuckKey(KeyEvent lastKeyDown, DateTime now, DebounceSettings settings);

    /// <summary>
    /// Рассчитать итоговый статус клавиши на основе накопленной статистики.
    /// </summary>
    /// <param name="statistics">Статистика клавиши.</param>
    /// <param name="settings">Пороговые настройки.</param>
    /// <returns>Статус клавиши.</returns>
    KeyStatus CalculateStatus(KeyStatistics statistics, DebounceSettings settings);
}
