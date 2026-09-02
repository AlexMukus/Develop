using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KeyboardTester.Application.ViewModels;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Res = KeyboardTester.UI.Resources;
using SkiaSharp;

namespace KeyboardTester.UI.Controls;

/// <summary>
/// Панель live-графиков: интервалы между нажатиями и время удержания клавиш.
/// Подписывается на <see cref="MainViewModel.PressIntervalPoints"/> и
/// <see cref="MainViewModel.HoldDurationPoints"/> и сам ведёт серии LiveCharts.
/// v1.1.0: цвета осей, сетки и линий берутся из ресурсов темы и обновляются
/// при её смене (реакция на <see cref="MainViewModel.CurrentTheme"/>).
/// </summary>
public partial class ChartsPanelControl : UserControl
{
    private static readonly DateTime Epoch = DateTime.Today;

    private readonly ObservableCollection<DateTimePoint> _intervalValues = new();
    private readonly ObservableCollection<DateTimePoint> _holdValues = new();
    private readonly LineSeries<DateTimePoint> _intervalSeries;
    private readonly LineSeries<DateTimePoint> _holdSeries;
    private readonly Axis[] _allAxes;

    private MainViewModel? _viewModel;
    private NotifyCollectionChangedEventHandler? _intervalHandler;
    private NotifyCollectionChangedEventHandler? _holdHandler;
    private PropertyChangedEventHandler? _themeHandler;

    /// <summary>
    /// Создаёт панель графиков.
    /// </summary>
    public ChartsPanelControl()
    {
        InitializeComponent();

        _intervalSeries = new LineSeries<DateTimePoint>
        {
            Values = _intervalValues,
            GeometrySize = 4,
            Fill = null,
            Name = Res.Strings.IntervalSeriesName,
        };
        _holdSeries = new LineSeries<DateTimePoint>
        {
            Values = _holdValues,
            GeometrySize = 4,
            Fill = null,
            Name = Res.Strings.HoldSeriesName,
        };

        IntervalChart.Series = new ISeries[] { _intervalSeries };
        HoldChart.Series = new ISeries[] { _holdSeries };

        var intervalXAxis = new DateTimeAxis(TimeSpan.FromMinutes(1), static date => date.ToString("mm\\:ss"));
        var holdXAxis = new DateTimeAxis(TimeSpan.FromMinutes(1), static date => date.ToString("mm\\:ss"));
        var intervalYAxis = new Axis { MinLimit = 0 };
        var holdYAxis = new Axis { MinLimit = 0 };

        IntervalChart.XAxes = new Axis[] { intervalXAxis };
        HoldChart.XAxes = new Axis[] { holdXAxis };
        IntervalChart.YAxes = new Axis[] { intervalYAxis };
        HoldChart.YAxes = new Axis[] { holdYAxis };

        _allAxes = new[] { intervalXAxis, holdXAxis, intervalYAxis, holdYAxis };

        ApplyChartTheme();

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateHintVisibility();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Вкладка TabControl выгружает контрол при переключении; при возврате
        // DataContext уже установлен, но событие DataContextChanged не возникает —
        // присоединяемся повторно вручную.
        if (_viewModel == null && DataContext is MainViewModel vm)
        {
            AttachViewModel(vm);
        }
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

        // Перекраска графиков при смене темы оформления.
        _themeHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentTheme))
            {
                ApplyChartTheme();
            }
        };
        vm.PropertyChanged += _themeHandler;

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

            if (_themeHandler != null)
            {
                _viewModel.PropertyChanged -= _themeHandler;
                _themeHandler = null;
            }

            _viewModel = null;
        }

        _intervalValues.Clear();
        _holdValues.Clear();
        UpdateHintVisibility();
    }

    /// <summary>
    /// Применяет цвета текущей темы к осям, сетке и линиям графиков.
    /// Кисти ChartForegroundBrush/ChartGridBrush/AccentBrush определены
    /// в Themes/Dark.xaml и Themes/Light.xaml.
    /// </summary>
    private void ApplyChartTheme()
    {
        Color labelColor = ThemeColor("ChartForegroundBrush", Color.FromRgb(0xCC, 0xCC, 0xCC));
        Color gridColor = ThemeColor("ChartGridBrush", Color.FromRgb(0x3E, 0x3E, 0x3E));
        Color seriesColor = ThemeColor("AccentBrush", Color.FromRgb(0x00, 0x7A, 0xCC));

        var labelPaint = new SolidColorPaint(ToSkia(labelColor));
        var gridPaint = new SolidColorPaint(ToSkia(gridColor)) { StrokeThickness = 1 };
        var seriesPaint = new SolidColorPaint(ToSkia(seriesColor)) { StrokeThickness = 2 };

        foreach (Axis axis in _allAxes)
        {
            axis.LabelsPaint = labelPaint;
            axis.SeparatorsPaint = gridPaint;
        }

        _intervalSeries.Stroke = seriesPaint;
        _holdSeries.Stroke = seriesPaint;
    }

    private static SKColor ToSkia(Color color) => new(color.R, color.G, color.B, color.A);

    private static Color ThemeColor(string resourceKey, Color fallback)
    {
        return System.Windows.Application.Current?.TryFindResource(resourceKey) is SolidColorBrush brush
            ? brush.Color
            : fallback;
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
