using System.Collections.ObjectModel;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Application.ViewModels;

/// <summary>
/// ViewModel сохранённой тестовой сессии для отображения в списке истории
/// и в панели просмотра деталей сессии (v1.1.0).
/// </summary>
public sealed class TestSessionViewModel
{
    /// <summary>
    /// Создаёт ViewModel на основе доменной сессии.
    /// </summary>
    public TestSessionViewModel(TestSession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));

        foreach (KeyStatistics statistics in session.Statistics.Values.OrderByDescending(s => s.PressCount))
        {
            StatisticsList.Add(statistics);
        }
    }

    /// <summary>
    /// Исходная доменная сессия.
    /// </summary>
    public TestSession Session { get; }

    /// <summary>
    /// Отображаемое имя сессии.
    /// </summary>
    public string DisplayName => Session.Name;

    /// <summary>
    /// Дата начала в кратком формате.
    /// </summary>
    public string FormattedDate => Session.StartTime.ToString("g");

    /// <summary>
    /// Общее количество нажатий по всем клавишам.
    /// </summary>
    public int TotalPressCount => Session.Statistics.Values.Sum(s => s.PressCount);

    /// <summary>
    /// Количество клавиш со статусом Warning или Critical.
    /// </summary>
    public int ProblematicKeysCount => Session.Statistics.Values.Count(s => s.Status is KeyStatus.Warning or KeyStatus.Critical);

    /// <summary>
    /// Статистика клавиш сессии, отсортированная по количеству нажатий,
    /// для таблицы в панели просмотра истории.
    /// </summary>
    public ObservableCollection<KeyStatistics> StatisticsList { get; } = [];
}
