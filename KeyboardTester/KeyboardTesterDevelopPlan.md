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

**Ограничение среды:** локально нет .NET 9 SDK (не устанавливать) — сборка/тесты локально не проверяются; валидация через CI (`dotnet-version: '9.0.x'`). Код пишем максимально аккуратно: `TreatWarningsAsErrors=true`, nullable, русские XML-доки.

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

## Этап 4: Presentation Layer (`src/KeyboardTester.UI/`)
- **4.1** `Views/MainWindow.xaml` — Toolbar (раскладка, старт/стоп/сброс, настройки, о программе), `VirtualKeyboardControl`, TabControl (Графики / Ghosting / Статистика / История), status bar; конвертеры `Converters/KeyStatusToBrushConverter.cs`, `KeyStatusToDescriptionConverter.cs`, `BoolToVisibilityConverter.cs`, `InverseBoolConverter.cs`.
- **4.2** `Controls/VirtualKeyboardControl.xaml(.cs)` — программный рендер клавиш по `Row`/`Column`/`KeySize` (Canvas), **анимация плавного затухания** через `ColorAnimation` на `SolidColorBrush` при `KeyReleased`.
- **4.3** `Themes/` — `ThemeManager.cs` (реестр `AppsUseLightTheme`, `pack://` словари), `Themes/Dark.xaml`, `Themes/Light.xaml`; тема по умолчанию — системная; сохранение выбора в settings.
- **4.4** `Controls/GhostingTestControl.xaml(.cs)` — инструкция, Viewbox-матрица, счётчики «Нажато/Максимум/NKRO», подсветка нажатых.
- **4.5** Остальное:
  - `Controls/StatisticsPanel.xaml` — DataGrid (Клавиша/Нажатий/Сред. интервал/Сред. удержание/Дребезг/Статус), фильтр, сортировка, двойной клик → выбор клавиши;
  - `Controls/SessionHistoryPanel.xaml` — список сессий, удаление, сравнение двух сессий side-by-side (+/- нажатий, смена статусов);
  - `Dialogs/SettingsDialog.xaml` — пороги дребезга/залипания, макс. событий, тема; сохранение в `%AppData%/KeyboardTester/settings.json`;
  - `Dialogs/AboutDialog.xaml` — версия из сборки, ссылка на GitHub, список библиотек, «Проверить обновления» → открытие страницы Releases (ручное обновление).
  - Графики: `LiveChartsCore.SkiaSharpView.WPF` CartesianChart ×2 (интервалы, удержание), live-обновление через `ObservableCollection<KeyDataPoint>` из ViewModel.
- DI-композиция в `App.xaml.cs`: регистрация всех сервисов Infrastructure + ViewModels, запуск `MainWindow`.

## Этап 5: Локализация (`src/KeyboardTester.UI/Resources/`)
- `Strings.ru.resx` (+ `Strings.resx` как fallback) по таблице ключей из ТЗ.
- Реализация `ILocalizationService` (`Services/LocalizationService.cs` в UI или Infrastructure) поверх resx-менеджера; биндинги в XAML через `{x:Static}`/локатор по мере необходимости. Ключи из ТЗ: AppTitle, StartTest, StopTest, ResetTest, GhostingTest, Settings, About, Layout, Duration, TotalPresses, ProblematicKeys, Tab*, Column*, Status*, Threshold*, Theme*, CheckUpdates, Version, License, GitHubLink, NoData, SelectKeyHint, SessionSaved, ConfirmDelete, CompareSessions, IntervalChartTitle, HoldChartTitle, GhostingInstruction.

## Этап 6: Тесты
- **Unit (`tests/KeyboardTester.Core.Tests/`)**: `DebounceAnalyzerTests` (пустой список, одиночное нажатие, `Theory` по порогам 5/15/30/45/60/75/100, залипание до/после порога, статусы Critical/Warning/Ok), `StatisticsEngineTests` (Moq для `IDebounceAnalyzer`: счётчики, интервалы, удержание, парность Down/Up, `Reset`, `GetStatistics` для несуществующей клавиши). Тестовые классы Infrastructure-реализаций физически размещаются в Core.Tests только если тестируют Core; тесты Infrastructure — в `KeyboardTester.Integration.Tests` либо отдельным набором (как в ТЗ: DebounceAnalyzer/StatisticsEngine тесты рядом, допустимо в Integration.Tests — они на `net9.0-windows`).
- **Integration (`tests/KeyboardTester.Integration.Tests/`)**: `RawInputCaptureTests` (Start/Stop/EnumerateDevices; `keybd_event` для симуляции), `TestSessionFlowTests` (полный цикл: захват → симулированные нажатия → статистика → сохранение сессии), тесты `SessionHistoryService` (round-trip JSON с конвертером словаря).

## Этап 7: Сборка portable
- `build-portable.ps1` в корне: `git describe --tags --always`, `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`, удаление `*.pdb`, `Compress-Archive`.
- `.vscode/launch.json` — конфигурация «Launch WPF».
- Сверка с CI: `release.yml` уже создаёт zip по тегам `v*` — убедиться, что локальный скрипт и CI дают одинаковый артефакт.

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
