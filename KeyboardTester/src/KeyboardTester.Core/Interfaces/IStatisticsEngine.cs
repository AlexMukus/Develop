using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Сервис учёта статистики нажатий клавиш.
/// </summary>
public interface IStatisticsEngine
{
    /// <summary>Событие обновления статистики по клавише.</summary>
    event EventHandler<KeyStatisticsUpdatedEventArgs>? StatisticsUpdated;

    /// <summary>Выбранная раскладка клавиатуры.</summary>
    KeyboardLayout SelectedLayout { get; set; }

    /// <summary>Зарегистрировать нажатие клавиши.</summary>
    void RecordKeyDown(KeyEvent keyEvent);

    /// <summary>Зарегистрировать отпускание клавиши.</summary>
    void RecordKeyUp(KeyEvent keyEvent);

    /// <summary>
    /// Получить статистику для указанной клавиши.
    /// </summary>
    /// <param name="key">Клавиша.</param>
    /// <returns>Статистика или null, если клавиша не тестировалась.</returns>
    KeyStatistics? GetStatistics(PhysicalKey key);

    /// <summary>Получить статистику по всем клавишам.</summary>
    IReadOnlyDictionary<PhysicalKey, KeyStatistics> GetAllStatistics();

    /// <summary>Сбросить статистику для всех клавиш.</summary>
    void Reset();

    /// <summary>
    /// Сбросить статистику для указанной клавиши.
    /// </summary>
    /// <param name="key">Клавиша.</param>
    void ResetKey(PhysicalKey key);
}
