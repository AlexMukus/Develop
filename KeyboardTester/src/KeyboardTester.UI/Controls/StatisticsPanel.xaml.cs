using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using KeyboardTester.Application.ViewModels;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.UI.Controls;

/// <summary>
/// Панель статистики по клавишам: таблица с фильтром и сортировкой.
/// Двойной клик по строке выбирает клавишу для детального просмотра.
/// </summary>
public partial class StatisticsPanel : UserControl
{
    private int _filterMode; // 0 — все, 1 — только проблемные, 2 — только не тестированные

    /// <summary>
    /// Создаёт панель статистики.
    /// </summary>
    public StatisticsPanel()
    {
        InitializeComponent();
    }

    private void StatisticsView_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not KeyStatistics statistics)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = _filterMode switch
        {
            1 => statistics.Status is KeyStatus.Warning or KeyStatus.Critical,
            2 => statistics.Status == KeyStatus.NotTested,
            _ => true,
        };
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _filterMode = FilterCombo.SelectedIndex;
        if (Resources["StatisticsView"] is CollectionViewSource source)
        {
            source.View?.Refresh();
        }
    }

    private void StatisticsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (StatisticsGrid.SelectedItem is not KeyStatistics statistics ||
            DataContext is not MainViewModel vm)
        {
            return;
        }

        KeyViewModel? key = vm.Keys.FirstOrDefault(k => k.PhysicalKey.Equals(statistics.Key));
        if (key != null)
        {
            vm.SelectedKey = key;
        }
    }
}
