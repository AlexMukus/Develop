using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using KeyboardTester.Application.ViewModels;
using LiveChartsCore;
using Res = KeyboardTester.UI.Resources;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace KeyboardTester.UI.Controls;

/// <summary>
/// Панель live-графиков: интервалы между нажатиями и время удержания клавиш.
/// Подписывается на <see cref="MainViewModel.PressIntervalPoints"/> и
/// <see cref="MainViewModel.HoldDurationPoints"/> и сам ведёт серии LiveCharts.
/// </summary>
public partial class ChartsPanelControl : UserControl
{
    private static readonly DateTime Epoch = DateTime.Today;

    private readonly ObservableCollection<DateTimePoint> _intervalValues = new();
    private readonly ObservableCollection<DateTimePoint> _holdValues = new();

    private MainViewModel? _viewModel;
    private NotifyCollectionChangedEventHandler? _intervalHandler;
    private NotifyCollectionChangedEventHandler? _holdHandler;

    /// <summary>
    /// Создаёт панель графиков.
    /// </summary>
    public ChartsPanelControl()
    {
        InitializeComponent();

        IntervalChart.Series = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = _intervalValues,
                GeometrySize = 4,
                Fill = null,
                Name = Res.Strings.IntervalSeriesName,
            },
        };
        HoldChart.Series = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = _holdValues,
                GeometrySize = 4,
                Fill = null,
                Name = Res.Strings.HoldSeriesName,
            },
        };

        Axis[] timeAxes =
        {
            new DateTimeAxis(TimeSpan.FromMinutes(1), static date => date.ToString("mm\\:ss")),
        };
        IntervalChart.XAxes = timeAxes;
        HoldChart.XAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromMinutes(1), static date => date.ToString("mm\\:ss")),
        };
        IntervalChart.YAxes = new Axis[] { new Axis { MinLimit = 0 } };
        HoldChart.YAxes = new Axis[] { new Axis { MinLimit = 0 } };

        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
        UpdateHintVisibility();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachViewModel();
        if (e.NewValue is MainViewModel vm)
        {
            AttachViewModel(vm);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachViewModel();
    }

    private void AttachViewModel(MainViewModel vm)
    {
        _viewModel = vm;

        foreach (KeyDataPoint point in vm.PressIntervalPoints)
        {
            _intervalValues.Add(ToPoint(point));
        }

        foreach (KeyDataPoint point in vm.HoldDurationPoints)
        {
            _holdValues.Add(ToPoint(point));
        }

        _intervalHandler = (_, args) => OnPointsChanged(vm.PressIntervalPoints, _intervalValues, args);
        _holdHandler = (_, args) => OnPointsChanged(vm.HoldDurationPoints, _holdValues, args);
        vm.PressIntervalPoints.CollectionChanged += _intervalHandler;
        vm.HoldDurationPoints.CollectionChanged += _holdHandler;

        UpdateHintVisibility();
    }

    private void DetachViewModel()
    {
        if (_viewModel != null)
        {
            if (_intervalHandler != null)
            {
                _viewModel.PressIntervalPoints.CollectionChanged -= _intervalHandler;
                _intervalHandler = null;
            }

            if (_holdHandler != null)
            {
                _viewModel.HoldDurationPoints.CollectionChanged -= _holdHandler;
                _holdHandler = null;
            }

            _viewModel = null;
        }

        _intervalValues.Clear();
        _holdValues.Clear();
        UpdateHintVisibility();
    }

    private void OnPointsChanged(
        ObservableCollection<KeyDataPoint> source,
        ObservableCollection<DateTimePoint> target,
        NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (args.NewItems != null)
                {
                    foreach (KeyDataPoint point in args.NewItems)
                    {
                        target.Add(ToPoint(point));
                    }
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                // MainViewModel подрезает коллекцию с начала (RemoveAt(0)).
                if (args.OldStartingIndex >= 0 && args.OldItems != null)
                {
                    for (int i = 0; i < args.OldItems.Count && args.OldStartingIndex < target.Count; i++)
                    {
                        target.RemoveAt(args.OldStartingIndex);
                    }
                }

                break;
            default:
                // Reset/Replace/Move: просто пересинхронизировать.
                target.Clear();
                foreach (KeyDataPoint point in source)
                {
                    target.Add(ToPoint(point));
                }

                break;
        }

        UpdateHintVisibility();
    }

    private void UpdateHintVisibility()
    {
        EmptyHint.Visibility = _intervalValues.Count == 0 && _holdValues.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static DateTimePoint ToPoint(KeyDataPoint point)
    {
        return new DateTimePoint(Epoch.Add(point.RelativeTime), point.ValueMs);
    }
}
