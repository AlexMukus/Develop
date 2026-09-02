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
/// v1.1.0: цвета статусов и рамок берутся из ресурсов темы; нажатая клавиша
/// выделяется цветом акцента и утолщённой рамкой; в правом верхнем углу клавиши
/// отображается счётчик нажатий (до 4 символов).
/// </summary>
public partial class VirtualKeyboardControl : UserControl
{
    private const double BaseUnitSize = 50;  // px за 1u
    private const double RowHeight = 55;     // px за ряд
    private const double KeyGap = 4;         // промежуток между клавишами

    private static readonly TimeSpan PressDuration = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(300);

    private readonly KeyStatusToBrushConverter _statusToBrushConverter = new();
    private readonly PressCountToBadgeConverter _pressCountToBadgeConverter = new();
    private readonly Dictionary<KeyViewModel, PropertyChangedEventHandler> _keyHandlers = new();
    private readonly Dictionary<KeyViewModel, Border> _keyBorders = new();

    private MainViewModel? _viewModel;
    private NotifyCollectionChangedEventHandler? _keysChangedHandler;
    private PropertyChangedEventHandler? _themeChangedHandler;

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

        // Смена темы: кисти клавиш создаются из ресурсов при построении,
        // поэтому клавиатура перестраивается заново (v1.1.0).
        _themeChangedHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentTheme))
            {
                BuildKeyboard(vm.Keys);
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
        _keyBorders.Clear();
        KeyboardCanvas.Children.Clear();
    }

    private Border CreateKeyBorder(KeyViewModel key)
    {
        var nameText = new TextBlock
        {
            Text = key.PhysicalKey.DisplayName,
            Foreground = GetThemeBrush("KeyTextBrush", Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
        };

        // Счётчик нажатий в правом верхнем углу клавиши (v1.1.0): максимум 4 символа.
        var badgeText = new TextBlock
        {
            Foreground = GetThemeBrush("KeyBadgeTextBrush", Colors.White),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            TextAlignment = TextAlignment.Right,
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 4, 0),
        };
        badgeText.SetBinding(TextBlock.TextProperty, new Binding(nameof(KeyViewModel.PressCount))
        {
            Source = key,
            Converter = _pressCountToBadgeConverter,
        });

        var content = new Grid();
        content.Children.Add(nameText);
        content.Children.Add(badgeText);

        var border = new Border
        {
            Width = key.PhysicalKey.KeySize * BaseUnitSize - KeyGap,
            Height = RowHeight - KeyGap,
            CornerRadius = new CornerRadius(4),
            Background = KeyStatusToBrushConverter.CreateBrush(key.Status),
            BorderBrush = GetThemeBrush("KeyBorderBrush", Colors.DarkGray),
            BorderThickness = new Thickness(1),
            Child = content,
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

        _keyBorders[key] = border;

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

        // Нажатие: цвет акцента темы + утолщённая рамка — визуально отлично
        // от статусного цвета. Отпускание: возврат к цвету статуса, рамка 1px.
        if (key.IsPressed)
        {
            border.BorderThickness = new Thickness(2);
            border.BorderBrush = GetThemeBrush("KeyPressedBrush", Color.FromRgb(0x00, 0x7A, 0xCC));
            AnimateBackground(border, GetAccentColor(), PressDuration);
        }
        else
        {
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = GetThemeBrush("KeyBorderBrush", Colors.DarkGray);
            AnimateBackground(border, GetStatusColor(key.Status), FadeDuration);
        }
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

    private static Color GetAccentColor()
    {
        return GetThemeBrush("KeyPressedBrush", Color.FromRgb(0x00, 0x7A, 0xCC)).Color;
    }

    private static Color GetStatusColor(KeyStatus status) => status switch
    {
        KeyStatus.NotTested => GetThemeBrush("KeyNotTestedBrush", Color.FromRgb(0x50, 0x50, 0x50)).Color,
        KeyStatus.Ok => GetThemeBrush("KeyOkBrush", Color.FromRgb(0x2E, 0xCC, 0x71)).Color,
        KeyStatus.Warning => GetThemeBrush("KeyWarningBrush", Color.FromRgb(0xF1, 0xC4, 0x0F)).Color,
        KeyStatus.Critical => GetThemeBrush("KeyCriticalBrush", Color.FromRgb(0xE7, 0x4C, 0x3C)).Color,
        _ => Colors.Gray,
    };

    private static SolidColorBrush GetThemeBrush(string resourceKey, Color fallback)
    {
        if (System.Windows.Application.Current?.TryFindResource(resourceKey) is SolidColorBrush brush)
        {
            return new SolidColorBrush(brush.Color);
        }

        return new SolidColorBrush(fallback);
    }

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
