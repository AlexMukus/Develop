using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KeyboardTester.Application.ViewModels;

namespace KeyboardTester.UI.Controls;

/// <summary>
/// Контрол ghosting-теста: матрица клавиш с мгновенной (без анимации)
/// подсветкой удерживаемых клавиш и счётчиками NKRO.
/// </summary>
public partial class GhostingTestControl : UserControl
{
    private const double BaseUnitSize = 50;
    private const double RowHeight = 55;
    private const double KeyGap = 4;

    private static readonly Color PressedColor = Color.FromRgb(0x2E, 0xCC, 0x71);
    private static readonly Color NormalColor = Color.FromRgb(0x50, 0x50, 0x50);

    private readonly Dictionary<KeyViewModel, (Border Border, PropertyChangedEventHandler Handler)> _keyVisuals = new();

    private MainViewModel? _viewModel;
    private NotifyCollectionChangedEventHandler? _keysChangedHandler;
    private PropertyChangedEventHandler? _themeChangedHandler;

    /// <summary>
    /// Создаёт контрол ghosting-теста.
    /// </summary>
    public GhostingTestControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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
        BuildMatrix(vm.Keys);

        _keysChangedHandler = (_, _) => BuildMatrix(vm.Keys);
        vm.Keys.CollectionChanged += _keysChangedHandler;

        // Смена темы: кисти матрицы создаются из ресурсов при построении,
        // поэтому матрица перестраивается заново (v1.1.0).
        _themeChangedHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentTheme))
            {
                BuildMatrix(vm.Keys);
            }
        };
        vm.PropertyChanged += _themeChangedHandler;
    }

    private void DetachViewModel()
    {
        if (_viewModel != null)
        {
            if (_keysChangedHandler != null)
            {
                _viewModel.Keys.CollectionChanged -= _keysChangedHandler;
                _keysChangedHandler = null;
            }

            if (_themeChangedHandler != null)
            {
                _viewModel.PropertyChanged -= _themeChangedHandler;
                _themeChangedHandler = null;
            }

            _viewModel = null;
        }

        ClearKeys();
    }

    private void BuildMatrix(IEnumerable<KeyViewModel> keys)
    {
        ClearKeys();

        double maxRight = 0;
        double maxBottom = 0;

        Brush pressedBrush = ThemeBrush("KeyPressedBrush", PressedColor);
        Brush normalBrush = ThemeBrush("KeyNotTestedBrush", NormalColor);
        Brush borderBrush = ThemeBrush("KeyBorderBrush", Colors.DarkGray);
        Brush textBrush = ThemeBrush("KeyTextBrush", Colors.White);

        foreach (KeyViewModel key in keys)
        {
            var border = new Border
            {
                Width = key.PhysicalKey.KeySize * BaseUnitSize - KeyGap,
                Height = RowHeight - KeyGap,
                CornerRadius = new CornerRadius(4),
                Background = key.IsPressed ? pressedBrush : normalBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = key.PhysicalKey.DisplayName,
                    Foreground = textBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                },
            };

            Canvas.SetLeft(border, key.PhysicalKey.Column * BaseUnitSize + KeyGap / 2);
            Canvas.SetTop(border, key.PhysicalKey.Row * RowHeight + KeyGap / 2);

            PropertyChangedEventHandler handler = (_, args) =>
            {
                if (args.PropertyName == nameof(KeyViewModel.IsPressed))
                {
                    // Мгновенная подсветка без анимации для максимальной отзывчивости.
                    border.Background = key.IsPressed ? pressedBrush : normalBrush;
                }
            };
            key.PropertyChanged += handler;
            _keyVisuals[key] = (border, handler);

            GhostingCanvas.Children.Add(border);
            maxRight = Math.Max(maxRight, key.PhysicalKey.Column + key.PhysicalKey.KeySize);
            maxBottom = Math.Max(maxBottom, key.PhysicalKey.Row + 1);
        }

        GhostingCanvas.Width = maxRight * BaseUnitSize + KeyGap;
        GhostingCanvas.Height = maxBottom * RowHeight + KeyGap;
    }

    private static Brush ThemeBrush(string resourceKey, Color fallback)
    {
        if (System.Windows.Application.Current?.TryFindResource(resourceKey) is SolidColorBrush brush)
        {
            return new SolidColorBrush(brush.Color);
        }

        return new SolidColorBrush(fallback);
    }

    private void ClearKeys()
    {
        foreach (KeyValuePair<KeyViewModel, (Border Border, PropertyChangedEventHandler Handler)> pair in _keyVisuals)
        {
            pair.Key.PropertyChanged -= pair.Value.Handler;
        }

        _keyVisuals.Clear();
        GhostingCanvas.Children.Clear();
    }
}
