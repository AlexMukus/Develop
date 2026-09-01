using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using KeyboardTester.Application.ViewModels;
using KeyboardTester.Core.Enums;
using KeyboardTester.UI.Converters;
using Res = KeyboardTester.UI.Resources;

namespace KeyboardTester.UI.Controls;

/// <summary>
/// Виртуальная клавиатура: отображает клавиши текущей раскладки,
/// подсвечивает нажатия с анимацией и раскрашивает клавиши по статусу диагностики.
/// </summary>
public partial class VirtualKeyboardControl : UserControl
{
    private const double BaseUnitSize = 50;  // px за 1u
    private const double RowHeight = 55;     // px за ряд
    private const double KeyGap = 4;         // промежуток между клавишами

    private static readonly TimeSpan PressDuration = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(300);

    private readonly KeyStatusToBrushConverter _statusToBrushConverter = new();
    private readonly Dictionary<KeyViewModel, PropertyChangedEventHandler> _keyHandlers = new();

    private MainViewModel? _viewModel;
    private NotifyCollectionChangedEventHandler? _keysChangedHandler;

    /// <summary>
    /// Создаёт контрол виртуальной клавиатуры.
    /// </summary>
    public VirtualKeyboardControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
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
        BuildKeyboard(vm.Keys);

        _keysChangedHandler = (_, args) => BuildKeyboard(vm.Keys);
        vm.Keys.CollectionChanged += _keysChangedHandler;
    }

    private void DetachViewModel()
    {
        if (_viewModel != null && _keysChangedHandler != null)
        {
            _viewModel.Keys.CollectionChanged -= _keysChangedHandler;
            _keysChangedHandler = null;
        }

        _viewModel = null;
        ClearKeys();
    }

    private void BuildKeyboard(IEnumerable<KeyViewModel> keys)
    {
        ClearKeys();

        double maxRight = 0;
        double maxBottom = 0;

        foreach (KeyViewModel key in keys)
        {
            Border border = CreateKeyBorder(key);
            KeyboardCanvas.Children.Add(border);
            maxRight = Math.Max(maxRight, key.PhysicalKey.Column + key.PhysicalKey.KeySize);
            maxBottom = Math.Max(maxBottom, key.PhysicalKey.Row + 1);
        }

        KeyboardCanvas.Width = maxRight * BaseUnitSize + KeyGap;
        KeyboardCanvas.Height = maxBottom * RowHeight + KeyGap;
    }

    private void ClearKeys()
    {
        foreach (KeyValuePair<KeyViewModel, PropertyChangedEventHandler> pair in _keyHandlers)
        {
            pair.Key.PropertyChanged -= pair.Value;
        }

        _keyHandlers.Clear();
        KeyboardCanvas.Children.Clear();
    }

    private Border CreateKeyBorder(KeyViewModel key)
    {
        var border = new Border
        {
            Width = key.PhysicalKey.KeySize * BaseUnitSize - KeyGap,
            Height = RowHeight - KeyGap,
            CornerRadius = new CornerRadius(4),
            Background = KeyStatusToBrushConverter.CreateBrush(key.Status),
            BorderBrush = new SolidColorBrush(Colors.DarkGray),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = key.PhysicalKey.DisplayName,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
            },
        };

        Canvas.SetLeft(border, key.PhysicalKey.Column * BaseUnitSize + KeyGap / 2);
        Canvas.SetTop(border, key.PhysicalKey.Row * RowHeight + KeyGap / 2);

        // Фон следует за статусом клавиши. Конвертер возвращает новую
        // (не замороженную) кисть, поэтому цвет можно анимировать.
        var statusBinding = new Binding(nameof(KeyViewModel.Status))
        {
            Source = key,
            Converter = _statusToBrushConverter,
        };
        border.SetBinding(Border.BackgroundProperty, statusBinding);

        PropertyChangedEventHandler handler = (_, args) => OnKeyPropertyChanged(key, border, args);
        key.PropertyChanged += handler;
        _keyHandlers[key] = handler;

        border.ToolTip = CreateTooltip(key);

        border.MouseLeftButtonDown += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedKey = key;
            }
        };

        return border;
    }

    private static void OnKeyPropertyChanged(KeyViewModel key, Border border, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(KeyViewModel.IsPressed))
        {
            return;
        }

        Color target = key.IsPressed ? GetPressedColor(key.Status) : GetNormalColor(key.Status);
        TimeSpan duration = key.IsPressed ? PressDuration : FadeDuration;
        AnimateBackground(border, target, duration);
    }

    private static void AnimateBackground(Border border, Color targetColor, TimeSpan duration)
    {
        var animation = new ColorAnimation(targetColor, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };

        if (border.Background is SolidColorBrush brush)
        {
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
        else
        {
            border.Background = new SolidColorBrush(targetColor);
        }
    }

    private static Color GetNormalColor(KeyStatus status) => status switch
    {
        KeyStatus.NotTested => Color.FromRgb(0x50, 0x50, 0x50),
        KeyStatus.Ok => Color.FromRgb(0x2E, 0xCC, 0x71),
        KeyStatus.Warning => Color.FromRgb(0xF1, 0xC4, 0x0F),
        KeyStatus.Critical => Color.FromRgb(0xE7, 0x4C, 0x3C),
        _ => Colors.Gray,
    };

    private static Color GetPressedColor(KeyStatus status) => status switch
    {
        KeyStatus.NotTested => Color.FromRgb(120, 120, 120),
        KeyStatus.Ok => Color.FromRgb(88, 214, 141),
        KeyStatus.Warning => Color.FromRgb(245, 215, 110),
        KeyStatus.Critical => Color.FromRgb(236, 112, 99),
        _ => Colors.LightGray,
    };

    private static ToolTip CreateTooltip(KeyViewModel key)
    {
        var text = new TextBlock();
        text.Inlines.Add(new Run(key.PhysicalKey.DisplayName) { FontWeight = FontWeights.Bold });
        text.Inlines.Add(new LineBreak());
        text.Inlines.Add(new Run(Res.Strings.TooltipPressCount));

        var pressCountRun = new Run();
        pressCountRun.SetBinding(Run.TextProperty, new Binding(nameof(KeyViewModel.PressCount)) { Source = key });
        text.Inlines.Add(pressCountRun);

        text.Inlines.Add(new LineBreak());
        text.Inlines.Add(new Run(Res.Strings.TooltipStatus));

        var statusRun = new Run();
        statusRun.SetBinding(Run.TextProperty, new Binding(nameof(KeyViewModel.Status))
        {
            Source = key,
            Converter = new KeyStatusToDescriptionConverter(),
        });
        text.Inlines.Add(statusRun);

        return new ToolTip { Content = text };
    }
}
