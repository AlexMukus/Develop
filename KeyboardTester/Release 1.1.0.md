Реализованы все изменения версии 1.1.0. Сборка успешна, все 96 тестов проходят (79 существующих + 17 новых).

## Что сделано по пунктам

**1) Цветовые темы для панелей Статистика, История, Графики и Ghosting Test**
- [Dark.xaml](src/KeyboardTester.UI/Themes/Dark.xaml:1) / [Light.xaml](src/KeyboardTester.UI/Themes/Light.xaml:1): добавлены кисти для панелей, таблиц, заголовков, строк, подсказок и клавиатуры (`PanelBackgroundBrush`, `ItemBackground*Brush`, `KeyOkBrush` и др.);
- Новый [Styles.xaml](src/KeyboardTester.UI/Resources/Styles.xaml:1) — общие стили DataGrid, ListView/GridView, TabControl, ComboBox, Button, ToolTip на `DynamicResource`; подключён в [App.xaml](src/KeyboardTester.UI/App.xaml:1) (лежит в Resources/, чтобы ThemeManager не удалял его при смене темы);
- [ChartsPanelControl](src/KeyboardTester.UI/Controls/ChartsPanelControl.xaml.cs:166): цвета осей, сетки и линий LiveCharts берутся из темы (`ChartForegroundBrush`/`ChartGridBrush`/`AccentBrush`) и перекрашиваются при смене темы через подписку на `CurrentTheme`;
- [GhostingTestControl](src/KeyboardTester.UI/Controls/GhostingTestControl.xaml.cs:85): захардкоженные кисти заменены на ресурсы темы, матрица перестраивается при смене темы.

**2) Просмотр Истории**
- Развёрнутая панель [Expander в SessionHistoryPanel.xaml](src/KeyboardTester.UI/Controls/SessionHistoryPanel.xaml:49): метаданные (начало/завершение/раскладка), заметки, сводка и полная таблица статистики клавиш сессии;
- [TestSessionViewModel](src/KeyboardTester.Application/ViewModels/TestSessionViewModel.cs:46): добавлен `StatisticsList` (сортировка по нажатиям);
- [MainViewModel](src/KeyboardTester.Application/ViewModels/MainViewModel.cs:397): `IsSessionDetailsExpanded` с автосворачиванием при снятии выбора.

**3) Колонка Статус**
- [StatisticsPanel.xaml](src/KeyboardTester.UI/Controls/StatisticsPanel.xaml:46): обход дефекта WPF (`DataGridTemplateColumn` + `Auto` не измеряет шаблон) — колонка `Width="*"` + `MinWidth=140`, бейдж растягивается на всю ячейку, полное сообщение «Критическая проблема» помещается.

**4) Нажатие/отпускание клавиш**
- [VirtualKeyboardControl](src/KeyboardTester.UI/Controls/VirtualKeyboardControl.xaml.cs:203): нажатие — цвет акцента темы + утолщённая рамка 2px; отпускание — возврат к цвету статуса с рамкой 1px и плавным затуханием 300 мс. Состояния чётко различимы визуально.

**5) Счётчик нажатий на клавише**
- Правый верхний угол клавиши: бейдж через [PressCountToBadgeConverter](src/KeyboardTester.UI/Converters/PressCountToBadgeConverter.cs:1) — максимум 4 символа (0 — пусто, 1–9999 — число, 10000+ — «10k+»…«99k+»), обновляется биндингом на `PressCount`.

**6) Автоподстановка версии**
- [Directory.Build.props](Directory.Build.props:1): `<Version>1.1.0</Version>` для всех сборок;
- Новый [AppVersion.Current](src/KeyboardTester.UI/Services/AppVersion.cs:1): читает `AssemblyInformationalVersion` и обрезает суффикс `+хеш` (проверено: `1.1.0+cfdf…` → «1.1.0»); используется в [статус-баре](src/KeyboardTester.UI/Views/MainWindow.xaml.cs:31) и [окне «О программе»](src/KeyboardTester.UI/Views/AboutDialog.xaml.cs:21).

## Тесты
- [PressCountToBadgeConverterTests](tests/KeyboardTester.Integration.Tests/Converters/PressCountToBadgeConverterTests.cs:1) — 11 тестов формата бейджа;
- [TestSessionViewModelTests](tests/KeyboardTester.Integration.Tests/Session/TestSessionViewModelTests.cs:1) — 6 тестов панели деталей, включая цикл сохранения/загрузки истории.