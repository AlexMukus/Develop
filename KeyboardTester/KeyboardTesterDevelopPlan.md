# План разработки KeyboardTester (после Этапов 0 и 1)

## Контекст

**Уже реализовано:**
- **Этап 0** — структура решения (6 проектов), `Directory.Build.props`, `Directory.Packages.props` (CPM), `.editorconfig`, `.gitignore`, `README.md`, `LICENSE`, `KeyboardTester.sln`, GitHub Actions (`build.yml`, `release.yml`).
- **Этап 1** — Domain Layer в `src/KeyboardTester.Core/`: модели (`KeyEvent`, `PhysicalKey`, `KeyStatistics`, `ChatterEvent`, `TestSession`, `DebounceSettings`, `GhostingTestResult`, `InputDevice`), enum'ы (`KeyboardLayout`, `KeyStatus`, `ChatterSeverity`, `AppTheme`), DTO (`RawKeyEventArgs`, `KeyStatisticsUpdatedEventArgs`, `DebounceResult`), интерфейсы (`IRawInputCapture`, `IDebounceAnalyzer`, `IStatisticsEngine`, `ISessionHistoryService`, `IGhostingTestEngine`, `ILayoutProvider`, `IThemeService`, `ILocalizationService`).

**Уточнённые требования из «Требования к проекту КлавТестер.docx»:**
- Одна активная клавиатура за раз (выбор через `SelectDevice`, без мульти-режима).
- Ghosting test (NKRO/6KRO) с визуализацией матрицы — нужен.
- Аудио-фидбек — не нужен (только визуал).
- История сессий — нужна (сравнение «до/после»).
- Графики — live-обновление при каждом нажатии.
- **Экспорт отчётов — НЕ нужен** → QuestPDF и генерация PDF исключаются из scope.
- Темы: Системная (по умолчанию), Тёмная, Светлая.
- Анимация плавного затухания на виртуальной клавиатуре — нужна.
- Дистрибуция: portable zip (уже настроено в CI); обновления — ручная загрузка с GitHub.

**Ограничение среды:**  Код пишем максимально аккуратно: `TreatWarningsAsErrors=true`, nullable, русские XML-доки.

---

## Этап 2: Infrastructure Layer (файлы в `src/KeyboardTester.Infrastructure/`)

### 2.0 Подготовка (корректировка по уточнённым требованиям)
- Удалить `QuestPDF` из `src/KeyboardTester.Infrastructure/KeyboardTester.Infrastructure.csproj` и из `Directory.Packages.props` (экспорт отменён).
- Добавить `<UseWPF>true</UseWPF>` в Infrastructure.csproj (нужен `HwndSource` для Raw Input).

### 2.1 Raw Input Capture (`Input/`)
Файлы:
- `NativeMethods.cs` — `[DllImport("user32.dll")]` / `kernel32.dll`: `RegisterRawInputDevices`, `GetRawInputData`, `GetRawInputDeviceList`, `GetRawInputDeviceInfo`, `GetMessageTime`, `QueryPerformanceCounter`, `QueryPerformanceFrequency`, `keybd_event` (для тестов). Атрибуты `SetLastError = true`.
- `RawInputStructures.cs` — `RAWINPUTDEVICE`, `RAWINPUTHEADER`, `RAWKEYBOARD`, `RAWINPUT`, `RAWINPUTDEVICELIST`, `RID_DEVICE_INFO` (+ вложенные keyboard/mouse/hid). `[StructLayout(LayoutKind.Sequential)]`.
- `RawInputConstants.cs` — `HID_USAGE_PAGE_GENERIC (0x01)`, `HID_USAGE_GENERIC_KEYBOARD (0x06)`, `RIDEV_INPUTSINK (0x100)`, `RIDEV_DEVNOTIFY (0x2000)`, `RID_INPUT (0x10000003)`, `RIM_TYPEKEYBOARD (1)`, `RI_KEY_MAKE (0)`, `RI_KEY_BREAK (1)`, `RI_KEY_E0 (2)`, `RI_KEY_E1 (4)`, `RIDI_DEVICENAME`, `RIDI_DEVICEINFO`, `WM_INPUT (0x00FF)`, `WM_DEVICECHANGE (0x0219)`.
- `INativeMethods.cs` — интерфейс поверх P/Invoke для мокирования в тестах; `WindowsNativeMethods.cs` — реализация.
- `RawInputCapture.cs` — реализация `IRawInputCapture`:
  - скрытое окно через `HwndSource`, регистрация `RIDEV_INPUTSINK | RIDEV_DEVNOTIFY`;
  - `WndProc`: `WM_INPUT` → `ProcessRawInput`, `WM_DEVICECHANGE` → `EnumerateDevices`;
  - фильтрация автоповтора (`ExtraInformation == 0x1000000`), фильтрация по `_selectedDevicePath`;
  - timestamp: `QueryPerformanceCounter` → микросекунды `(qpc * 1_000_000) / frequency`;
  - события через `SynchronizationContext.Current?.Post()` (UI-поток);
  - ошибки WinAPI через `Marshal.GetLastWin32Error()` + логирование `ILogger`.

### 2.2 Debounce Analyzer (`Analysis/DebounceAnalyzer.cs`)
- Реализация `IDebounceAnalyzer` по псевдокоду из ТЗ.
- **Важная поправка к ТЗ:** `KeyEvent` хранит QPC-микросекунды (не wall-clock), поэтому `IsStuckKey` и `StuckDuration` считать через `Stopwatch.GetTimestamp()` → микросекунды, а не `DateTime`. Параметр `now` в сигнатуре игнорировать (оставлен для совместимости интерфейса).

### 2.3 Statistics Engine + История сессий
- `Analysis/StatisticsEngine.cs` — `IStatisticsEngine`:
  - `ConcurrentDictionary<PhysicalKey, KeyStatistics>`, `_pendingKeyDowns`;
  - **Дополнение к ТЗ:** конструктор принимает также `ILayoutProvider` (маппинг ScanCode → PhysicalKey); выбранная раскладка — свойство `SelectedLayout` (по умолчанию `Ansi104`);
  - подсчёт интервалов, удержаний, дребезга, статуса; `TrimBuffer` по `MaxEventsPerKey`; событие `StatisticsUpdated`.
- `Storage/SessionHistoryService.cs` — `ISessionHistoryService`:
  - путь `%AppData%/KeyboardTester/history.json`, thread-safe через `lock`;
  - **Дополнение к ТЗ:** `System.Text.Json` не сериализует словарь с ключом-записью `PhysicalKey` — добавить конвертер `Storage/JsonConverters/PhysicalKeyDictionaryConverter.cs` (через `List<KeyValuePair<PhysicalKey, KeyStatistics>>`).

### 2.4 Layout Provider (`Layouts/LayoutProvider.cs`)
- Полные данные клавиш для `Ansi104`, `Iso105`, `Tkl`, `Layout75`, `Layout60`, `Numpad`: VK-коды, скан-коды, `Row`/`Column`/`KeySize` для рендеринга.
- ISO-отличия: Left Shift 1.25u + доп. клавиша `\|`; Enter — вертикальный (упрощённо одной клавишей 2.25u, комментарий в коде).
- `GetLayoutSize` по значениям из ТЗ; `DetectLayout` — по набору скан-кодов.

### 2.5 Ghosting Test Engine (`Analysis/GhostingTestEngine.cs`)
- Реализация `IGhostingTestEngine` по псевдокоду ТЗ: подписка на `IRawInputCapture`, `HashSet<PhysicalKey>` нажатых, `GhostingTestResult` на каждое нажатие, `IsNKeyRollover = count > 6`.

## Этап 3: Application Layer (`src/KeyboardTester.Application/`)
- `ViewModels/MainViewModel.cs` — `ObservableObject` (CommunityToolkit.Mvvm), поля/команды по ТЗ: `IsSessionRunning`, `SelectedLayout`, `SelectedKey`, `SessionDuration`, `TotalPressCount`, `ProblematicKeysCount`, `IsGhostingTestActive`, `CurrentTheme`, коллекции `Keys`, `PressIntervalPoints`, `HoldDurationPoints`, `GhostingResults`, `SessionHistory`; команды `StartTest`, `StopTest`, `Reset`, `OpenSettings`, `OpenAbout`, `StartGhostingTest`, `StopGhostingTest`, `SaveSession`, `DeleteSession`, `CompareSessions`.
- `ViewModels/KeyViewModel.cs`, `ViewModels/TestSessionViewModel.cs`, `ViewModels/KeyDataPoint.cs`.
- Существующий `Services/TestSessionService.cs` привести в соответствие с новой моделью `KeyEvent` (record) — он уже компилируется, но проверить логику (старт/стоп сессии, `SessionDuration` через таймер).
- **Примечание:** `CommunityToolkit.Mvvm` нужен в `KeyboardTester.Application` — добавить `PackageReference` (версия уже в CPM).

## Этап 4: Presentation Layer (WPF UI)

### 4.1 Главное окно

Промпт для ИИ:
Создай MainWindow.xaml и MainWindow.xaml.cs в KeyboardTester.UI/Views/:
XAML структура:
xml
<Window x:Class="KeyboardTester.UI.Views.MainWindow"
        Title="{Binding Title}"
        Background="{DynamicResource WindowBackgroundBrush}"
        MinWidth="1200" MinHeight="800">
    
    <Window.Resources>
        <!-- Конвертеры -->
        <converters:KeyStatusToBrushConverter x:Key="StatusToBrushConverter"/>
        <converters:KeyStatusToDescriptionConverter x:Key="StatusToDescConverter"/>
        <converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
    </Window.Resources>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>    <!-- Toolbar -->
            <RowDefinition Height="*"/>       <!-- Content -->
            <RowDefinition Height="Auto"/>    <!-- Status bar -->
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <ToolBar Grid.Row="0" Background="{DynamicResource ToolbarBackgroundBrush}">
            <ComboBox ItemsSource="{Binding AvailableLayouts}" 
                      SelectedItem="{Binding SelectedLayout}"
                      Width="150" Margin="5"/>
            <Separator/>
            <Button Content="▶ Начать тест" Command="{Binding StartTestCommand}"
                    IsEnabled="{Binding IsSessionRunning, Converter={StaticResource InverseBoolConverter}}"/>
            <Button Content="⏹ Остановить" Command="{Binding StopTestCommand}"
                    IsEnabled="{Binding IsSessionRunning}"/>
            <Button Content="🔄 Сбросить" Command="{Binding ResetTestCommand}"/>
            <Separator/>
            <Button Content="👻 Ghosting Test" Command="{Binding StartGhostingTestCommand}"/>
            <Separator/>
            <Button Content="⚙ Настройки" Click="OpenSettings_Click"/>
            <Button Content="❓ О программе" Click="OpenAbout_Click"/>
        </ToolBar>
        
        <!-- Content -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="2*"/>  <!-- Keyboard + Charts -->
                <ColumnDefinition Width="1*"/>  <!-- Sidebar -->
            </Grid.ColumnDefinitions>
            
            <!-- Left: Virtual Keyboard + Charts -->
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>  <!-- Virtual Keyboard -->
                    <RowDefinition Height="*"/>     <!-- Charts -->
                </Grid.RowDefinitions>
                
                <local:VirtualKeyboardControl Grid.Row="0"
                    DataContext="{Binding}"
                    Margin="10"/>
                
                <TabControl Grid.Row="1" Margin="10">
                    <TabItem Header="📊 Графики">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            
                            <!-- Интервалы -->
                            <lvc:CartesianChart Grid.Column="0"
                                Series="{Binding IntervalSeries}"
                                XAxes="{Binding IntervalXAxes}"
                                YAxes="{Binding IntervalYAxes}"/>
                            
                            <!-- Удержание -->
                            <lvc:CartesianChart Grid.Column="1"
                                Series="{Binding HoldSeries}"
                                XAxes="{Binding HoldXAxes}"
                                YAxes="{Binding HoldYAxes}"/>
                        </Grid>
                    </TabItem>
                    
                    <TabItem Header="👻 Ghosting Test" IsSelected="{Binding IsGhostingTestActive}">
                        <local:GhostingTestControl DataContext="{Binding}"/>
                    </TabItem>
                </TabControl>
            </Grid>
            
            <!-- Right: Statistics + History -->
            <TabControl Grid.Column="1" Margin="10">
                <TabItem Header="📋 Статистика">
                    <local:StatisticsPanel DataContext="{Binding}"/>
                </TabItem>
                <TabItem Header="📁 История">
                    <local:SessionHistoryPanel DataContext="{Binding}"/>
                </TabItem>
            </TabControl>
        </Grid>
        
        <!-- Status Bar -->
        <StatusBar Grid.Row="2" Background="{DynamicResource StatusBarBackgroundBrush}">
            <TextBlock Text="{Binding SessionDuration, StringFormat='Длительность: {0:hh\\:mm\\:ss}'}"/>
            <Separator/>
            <TextBlock Text="{Binding TotalPressCount, StringFormat='Всего нажатий: {0}'}"/>
            <Separator/>
            <TextBlock Text="{Binding ProblematicKeysCount, StringFormat='Проблемных клавиш: {0}'}"/>
            <Separator/>
            <TextBlock Text="v1.0.0"/>
        </StatusBar>
    </Grid>
</Window>

### 4.2 VirtualKeyboardControl (с анимацией)

Промпт для ИИ:
Создай VirtualKeyboardControl.xaml и code-behind в KeyboardTester.UI/Controls/:
XAML:
xml
<UserControl x:Class="KeyboardTester.UI.Controls.VirtualKeyboardControl">
    <Grid x:Name="KeyboardGrid">
        <!-- Клавиши добавляются программно в code-behind -->
    </Grid>
</UserControl>
Code-behind:
csharp
public partial class VirtualKeyboardControl : UserControl
{
    private readonly Dictionary<Guid, Border> _keyBorders = new();
    private const double BaseUnitSize = 50;  // px за 1u
    private const double RowHeight = 55;     // px за ряд
    
    public VirtualKeyboardControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            BuildKeyboard(vm.Keys);
            
            // Подписаться на изменения
            vm.Keys.CollectionChanged += (s, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                    BuildKeyboard(vm.Keys);
            };
        }
    }
    
    private void BuildKeyboard(ObservableCollection<KeyViewModel> keys)
    {
        KeyboardGrid.Children.Clear();
        _keyBorders.Clear();
        
        foreach (var key in keys)
        {
            var border = CreateKeyBorder(key);
            _keyBorders[key.Key.Id] = border;
            KeyboardGrid.Children.Add(border);
        }
    }
    
    private Border CreateKeyBorder(KeyViewModel key)
    {
        var border = new Border
        {
            Width = key.KeySize * BaseUnitSize - 4,  // 4px gap
            Height = RowHeight - 4,
            CornerRadius = new CornerRadius(4),
            Background = GetBrushForStatus(key.Status),
            BorderBrush = new SolidColorBrush(Colors.DarkGray),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = key.DisplayName,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            }
        };
        
        // Позиционирование
        Canvas.SetLeft(border, key.Column * BaseUnitSize + 2);
        Canvas.SetTop(border, key.Row * RowHeight + 2);
        
        // Привязки
        var statusBinding = new Binding(nameof(KeyViewModel.Status))
        {
            Source = key,
            Converter = new KeyStatusToBrushConverter()
        };
        border.SetBinding(Border.BackgroundProperty, statusBinding);
        
        // Анимация нажатия
        key.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(KeyViewModel.IsPressed))
            {
                if (key.IsPressed)
                {
                    // Яркий цвет при нажатии
                    var pressedColor = GetPressedColor(key.Status);
                    AnimateBackground(border, pressedColor, TimeSpan.FromMilliseconds(50));
                }
                else
                {
                    // Плавное затухание обратно
                    var normalColor = GetBrushForStatus(key.Status).Color;
                    AnimateBackground(border, normalColor, TimeSpan.FromMilliseconds(300));
                }
            }
        };
        
        // ToolTip с статистикой
        border.ToolTip = CreateTooltip(key);
        
        // Клик для выбора
        border.MouseLeftButtonDown += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
                vm.SelectedKey = key;
        };
        
        return border;
    }
    
    private void AnimateBackground(Border border, Color targetColor, TimeSpan duration)
    {
        var anim = new ColorAnimation(targetColor, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        
        if (border.Background is SolidColorBrush brush)
        {
            brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
        }
        else
        {
            border.Background = new SolidColorBrush(targetColor);
        }
    }
    
    private SolidColorBrush GetBrushForStatus(KeyStatus status) => status switch
    {
        KeyStatus.NotTested => new SolidColorBrush(Color.FromRgb(80, 80, 80)),
        KeyStatus.Ok => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
        KeyStatus.Warning => new SolidColorBrush(Color.FromRgb(241, 196, 15)),
        KeyStatus.Critical => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
        _ => new SolidColorBrush(Colors.Gray)
    };
    
    private Color GetPressedColor(KeyStatus status) => status switch
    {
        KeyStatus.NotTested => Color.FromRgb(120, 120, 120),
        KeyStatus.Ok => Color.FromRgb(88, 214, 141),
        KeyStatus.Warning => Color.FromRgb(245, 215, 110),
        KeyStatus.Critical => Color.FromRgb(236, 112, 99),
        _ => Colors.LightGray
    };
}

### 4.3 Темы (Dark/Light/System)

Промпт для ИИ:
Создай систему тем в KeyboardTester.UI/Themes/:
1. ThemeManager.cs:
csharp
public sealed class ThemeManager : IThemeService
{
    public event EventHandler? ThemeChanged;
    
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;
    
    public void SetTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        ApplyTheme(theme);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public AppTheme GetSystemTheme()
    {
        // Чтение реестра Windows
        // HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme
        // 0 = Dark, 1 = Light
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 1 ? AppTheme.Light : AppTheme.Dark;
        }
        catch { return AppTheme.Dark; }
    }
    
    private void ApplyTheme(AppTheme theme)
    {
        var actualTheme = theme == AppTheme.System ? GetSystemTheme() : theme;
        var dict = new ResourceDictionary();
        
        if (actualTheme == AppTheme.Dark)
        {
            dict.Source = new Uri("pack://application:,,,/KeyboardTester.UI;component/Themes/Dark.xaml");
        }
        else
        {
            dict.Source = new Uri("pack://application:,,,/KeyboardTester.UI;component/Themes/Light.xaml");
        }
        
        // Заменить текущую тему
        var oldDict = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Themes/") == true);
        if (oldDict != null)
            Application.Current.Resources.MergedDictionaries.Remove(oldDict);
        
        Application.Current.Resources.MergedDictionaries.Add(dict);
    }
}
2. Dark.xaml:
xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <SolidColorBrush x:Key="WindowBackgroundBrush" Color="#FF1E1E1E"/>
    <SolidColorBrush x:Key="ToolbarBackgroundBrush" Color="#FF2D2D2D"/>
    <SolidColorBrush x:Key="StatusBarBackgroundBrush" Color="#FF2D2D2D"/>
    <SolidColorBrush x:Key="TextBrush" Color="#FFFFFFFF"/>
    <SolidColorBrush x:Key="AccentBrush" Color="#FF007ACC"/>
    <SolidColorBrush x:Key="BorderBrush" Color="#FF3E3E3E"/>
    <SolidColorBrush x:Key="ChartBackgroundBrush" Color="#FF252526"/>
    <SolidColorBrush x:Key="ChartForegroundBrush" Color="#FFCCCCCC"/>
    <SolidColorBrush x:Key="ChartGridBrush" Color="#FF3E3E3E"/>
</ResourceDictionary>
3. Light.xaml:
xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <SolidColorBrush x:Key="WindowBackgroundBrush" Color="#FFF5F5F5"/>
    <SolidColorBrush x:Key="ToolbarBackgroundBrush" Color="#FFE0E0E0"/>
    <SolidColorBrush x:Key="StatusBarBackgroundBrush" Color="#FFE0E0E0"/>
    <SolidColorBrush x:Key="TextBrush" Color="#FF333333"/>
    <SolidColorBrush x:Key="AccentBrush" Color="#FF007ACC"/>
    <SolidColorBrush x:Key="BorderBrush" Color="#FFCCCCCC"/>
    <SolidColorBrush x:Key="ChartBackgroundBrush" Color="#FFFFFFFF"/>
    <SolidColorBrush x:Key="ChartForegroundBrush" Color="#FF333333"/>
    <SolidColorBrush x:Key="ChartGridBrush" Color="#FFDDDDDD"/>
</ResourceDictionary>

### 4.4 Ghosting Test Control

Промпт для ИИ:
Создай GhostingTestControl.xaml в KeyboardTester.UI/Controls/:
XAML:
xml
<UserControl x:Class="KeyboardTester.UI.Controls.GhostingTestControl">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Инструкция -->
        <TextBlock Grid.Row="0" Text="Зажмите как можно больше клавиш одновременно. Система покажет сколько из них зарегистрировано."
                   TextWrapping="Wrap" Margin="10" FontSize="14"/>
        
        <!-- Визуализация матрицы -->
        <Viewbox Grid.Row="1" Margin="20">
            <Grid x:Name="GhostingGrid">
                <!-- Рендерится программно -->
            </Grid>
        </Viewbox>
        
        <!-- Статистика -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Center" Margin="10">
            <TextBlock Text="Нажато: " FontSize="16"/>
            <TextBlock Text="{Binding GhostingPressedCount}" FontSize="16" FontWeight="Bold" Margin="0,0,20,0"/>
            <TextBlock Text="Максимум: " FontSize="16"/>
            <TextBlock Text="{Binding GhostingMaxCount}" FontSize="16" FontWeight="Bold" Margin="0,0,20,0"/>
            <TextBlock Text="NKRO: " FontSize="16"/>
            <TextBlock Text="{Binding IsNKeyRollover}" FontSize="16" FontWeight="Bold"/>
        </StackPanel>
    </Grid>
</UserControl>
Code-behind логика:
Отображать виртуальную клавиатуру текущей раскладки
При нажатии клавиши — мгновенно подсвечивать зелёным (без анимации, для максимальной отзывчивости)
Счётчик обновляется в реальном времени
После отпускания всех клавиш — показывать итоговый результат

### 4.5 Остальные контролы

Промпт для ИИ:
Создай оставшиеся контролы:
1. StatisticsPanel.xaml:
DataGrid с колонками: Клавиша, Нажатий, Средний интервал (мс), Среднее удержание (мс), Дребезг, Статус (с цветным фоном)
Фильтр ComboBox: "Все", "Только проблемные", "Только не тестированные"
Сортировка по клику на заголовок
Двойной клик → выбор клавиши + переключение на графики
2. SessionHistoryPanel.xaml:
ListView сессий: Название, Дата, Длительность, Примечания
Кнопки: "Удалить", "Сравнить" (выбор 2 сессий)
При сравнении: показать side-by-side статистику изменений (+/- нажатий, изменение статусов)
3. SettingsDialog.xaml:
Пороги дребезга (3 NumericUpDown)
Порог залипания (NumericUpDown)
Макс. событий (NumericUpDown)
Тема (ComboBox: Системная, Тёмная, Светлая)
Сохранение в %AppData%/KeyboardTester/settings.json
4. AboutDialog.xaml:
Логотип/название
Версия (из Assembly)
Описание
Ссылка на GitHub (кликабельная)
Список используемых библиотек с лицензиями
Кнопка "Проверить обновления" → открывает GitHub releases в браузере

## Этап 5: Локализация (`src/KeyboardTester.UI/Resources/`)

Промпт для ИИ:
Создай русскую локализацию в KeyboardTester.UI/Resources/Strings.ru.resx:
Ключи и значения:
Table
Ключ	Значение
AppTitle	Keyboard Tester Pro
StartTest	▶ Начать тест
StopTest	⏹ Остановить
ResetTest	🔄 Сбросить
GhostingTest	👻 Тест Ghosting
Settings	⚙ Настройки
About	❓ О программе
Layout	Раскладка
Duration	Длительность
TotalPresses	Всего нажатий
ProblematicKeys	Проблемных клавиш
TabCharts	📊 Графики
TabGhosting	👻 Ghosting
TabStatistics	📋 Статистика
TabHistory	📁 История
ColumnKey	Клавиша
ColumnPressCount	Нажатий
ColumnAvgInterval	Средний интервал, мс
ColumnAvgHold	Среднее удержание, мс
ColumnChatter	Дребезг
ColumnStatus	Статус
StatusNotTested	Не тестирована
StatusOk	Исправна
StatusWarning	Требует внимания
StatusCritical	Критическая проблема
ThresholdCritical	Порог критического дребезга, мс
ThresholdWarning	Порог предупреждения, мс
ThresholdMild	Порог лёгкого дребезга, мс
ThresholdStuck	Порог залипания, мс
MaxEvents	Макс. событий в буфере
Theme	Тема
ThemeSystem	Системная
ThemeDark	Тёмная
ThemeLight	Светлая
CheckUpdates	Проверить обновления
Version	Версия
License	Лицензия MIT
GitHubLink	github.com/yourusername/KeyboardTester
NoData	Нет данных
SelectKeyHint	Выберите клавишу для просмотра графиков
SessionSaved	Сессия сохранена в историю
ConfirmDelete	Удалить сессию?
CompareSessions	Сравнить сессии
IntervalChartTitle	Интервалы между нажатиями, мс
HoldChartTitle	Время удержания, мс
GhostingInstruction	Зажмите как можно больше клавиш одновременно
GhostingPressed	Нажато
GhostingMax	Максимум
NKRODetected	NKRO обнаружен
SixKRODetected	6KRO (ограничение)
Создай LocalizationService реализующий ILocalizationService через ResourceManager.

## Этап 6: Тесты

### 6.1 Unit-тесты

Промпт для ИИ:
Создай тесты в KeyboardTester.Core.Tests/:
1. DebounceAnalyzerTests:
csharp
public class DebounceAnalyzerTests
{
    private readonly DebounceAnalyzer _analyzer = new();
    private readonly DebounceSettings _settings = new();
    
    [Fact] public void Analyze_EmptyList_ReturnsEmptyResult() { ... }
    [Fact] public void Analyze_SinglePress_NoChatter() { ... }
    [Theory]
    [InlineData(5, ChatterSeverity.Critical)]
    [InlineData(15, ChatterSeverity.Critical)]
    [InlineData(30, ChatterSeverity.Moderate)]
    [InlineData(45, ChatterSeverity.Moderate)]
    [InlineData(60, ChatterSeverity.Mild)]
    [InlineData(75, ChatterSeverity.Mild)]
    [InlineData(100, ChatterSeverity.None)]
    public void DetectChatter_VariousIntervals_ReturnsCorrectSeverity(double intervalMs, ChatterSeverity expected) { ... }
    [Fact] public void IsStuckKey_AfterThreshold_ReturnsTrue() { ... }
    [Fact] public void IsStuckKey_BeforeThreshold_ReturnsFalse() { ... }
    [Fact] public void CalculateStatus_WithCriticalChatter_ReturnsCritical() { ... }
    [Fact] public void CalculateStatus_WithModerateChatter_ReturnsWarning() { ... }
    [Fact] public void CalculateStatus_NoIssues_ReturnsOk() { ... }
}
2. StatisticsEngineTests:
csharp
public class StatisticsEngineTests
{
    private readonly StatisticsEngine _engine;
    private readonly Mock<IDebounceAnalyzer> _mockAnalyzer;
    
    [Fact] public void RecordKeyDown_IncrementsPressCount() { ... }
    [Fact] public void RecordKeyDown_CalculatesInterval() { ... }
    [Fact] public void RecordKeyUp_CalculatesHoldDuration() { ... }
    [Fact] public void RecordKeyDown_And_KeyUp_PairingWorks() { ... }
    [Fact] public void Reset_ClearsAllData() { ... }
    [Fact] public void GetStatistics_NonExistentKey_ReturnsNull() { ... }
    [Fact] public void TrimBuffer_RespectsMaxEvents() { ... }
}
3. LayoutProviderTests:
csharp
public class LayoutProviderTests
{
    private readonly LayoutProvider _provider = new();
    
    [Theory]
    [InlineData(KeyboardLayout.Ansi104, 104)]
    [InlineData(KeyboardLayout.Iso105, 105)]
    [InlineData(KeyboardLayout.Tkl, 87)]
    [InlineData(KeyboardLayout.Layout75, 84)]
    [InlineData(KeyboardLayout.Layout60, 61)]
    public void GetKeys_ReturnsCorrectCount(KeyboardLayout layout, int expectedCount) { ... }
    
    [Fact] public void GetKeys_AllKeysHaveUniquePositions() { ... }
    [Fact] public void GetKeys_AllKeysHaveValidScanCodes() { ... }
}

### 6.2 Интеграционные тесты

Промпт для ИИ:
Создай тесты в KeyboardTester.Integration.Tests/:
csharp
public class RawInputCaptureTests : IDisposable
{
    private readonly RawInputCapture _capture;
    
    public RawInputCaptureTests()
    {
        _capture = new RawInputCapture(Mock.Of<ILogger<RawInputCapture>>());
    }
    
    [Fact] public void StartCapture_BeginsListening() { ... }
    [Fact] public void StopCapture_StopsListening() { ... }
    [Fact] public void DeviceEnumeration_ReturnsKeyboards() { ... }
    
    public void Dispose() => _capture.Dispose();
}

public class TestSessionFlowTests
{
    [Fact] public void FullSession_FlowTest() 
    {
        // Arrange: создать все сервисы
        // Act: StartCapture → симулировать нажатия → StopCapture
        // Assert: статистика корректна, сессия сохранена
    }
}

## Этап 7: Сборка portable
Промпт для ИИ:
Создай скрипты сборки:
1. build-portable.ps1:
powershell
param([string]$Configuration = "Release")

$version = git describe --tags --always
$outputDir = "artifacts/KeyboardTester-$version"

dotnet publish src/KeyboardTester.UI/KeyboardTester.UI.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputDir

# Удалить ненужные файлы
Remove-Item "$outputDir/*.pdb" -ErrorAction SilentlyContinue

# Создать zip
Compress-Archive -Path $outputDir -DestinationPath "$outputDir.zip" -Force

Write-Host "Portable build created: $outputDir.zip"
2. launch.json для VS Code:
JSON
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch WPF",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/KeyboardTester.UI/bin/Debug/net9.0-windows/KeyboardTester.UI.exe",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "console": "internalConsole"
        }
    ]
}

## Этап 8 (v1.2.0): Автоматическое определение типа клавиатуры — ВЫПОЛНЕН

Требования и решения зафиксированы в `plans/v1.2.0-keyboard-autodetection.md`.

### Реализовано

1. **Каталог VID/PID** — `Infrastructure/Layouts/KeyboardCatalog.cs`: 52 записи топ-моделей (Logitech, Razer, Corsair, SteelSeries, Cherry, Keychron, Ducky, Varmilo, Glorious, Royal Kludge, Roccat); попадание → раскладка применяется и привязка сохраняется молча, имя модели — в статус-баре.
2. **Маркерная эвристика** — `Infrastructure/Layouts/LayoutHeuristics.cs` по матрице (NumpadEnter `0xE01C` + сосед Shift `0x56`/`0x2C`): ISO 105 / ANSI 104 / null (ручной выбор).
3. **Визард** — `Application/Services/KeyboardDetectionService.cs` (конечный автомат Idle → WaitingNumpadEnter → WaitingLeftShift → Proposal, фильтр нажатий по `DevicePath` цели).
4. **Персистентность** — `IDeviceLayoutStore` поверх `SettingsService`: ключ `VID_XXXX&PID_YYYY`, fallback `DevicePath` для ноутбучных; merge-защита в `Save(debounce, theme)` (диалог настроек не стирает привязки).
5. **UI** — баннер визарда между тулбаром и контентом (индикаторы маркеров, «Нет цифрового блока», «Отмена»), диалог `LayoutProposalDialog` (предложение + ручной выбор + «Запомнить»), кнопка «Определить автоматически» в тулбаре, 13 новых ключей локализации.
6. **MainViewModel** — автоприменение (стор → каталог → визард) при подключении/выборе устройства, ручная смена раскладки обновляет привязку, захват ввода продолжает работать во время визарда.

### Статус

- Сборка без предупреждений, 156 тестов зелёные (92 Core + 64 Integration, +60 к v1.1.x).
- Версия 1.2.0 в `Directory.Build.props`.

## Критерии приёмки
1. `dotnet build KeyboardTester.sln` и `dotnet test` зелёные в CI (windows-latest, .NET 9).
2. Приложение запускается, захватывает нажатия реальной клавиатуры (Raw Input), показывает их на виртуальной клавиатуре с анимацией.
3. Дребезг/залипание детектируются и отображаются (цвета + таблица статистики).
4. Ghosting-тест показывает матрицу и максимум одновременных нажатий.
5. История сессий сохраняется в `%AppData%/KeyboardTester/history.json`, доступно сравнение.
6. Темы Системная/Тёмная/Светлая переключаются; системная — по умолчанию.
7. `build-portable.ps1` выдаёт `KeyboardTester-<версия>.zip`.

## Риски
- **Нет локальной сборки** (.NET 8 SDK) — каждый этап пишется «вслепую», проверка только в CI. Рекомендуется: PR на каждый этап, чтобы CI отловил ошибки рано.
- `HwndSource` в Infrastructure требует UI-диспетчера — в интеграционных тестах окно создавать на STA-потоке (`WpfFact` или ручной STA-runner).
- Данные раскладок (2.4) — самый объёмный механический участок; вероятны мелкие расхождения координат, правятся при 4.2 по скриншоту.
