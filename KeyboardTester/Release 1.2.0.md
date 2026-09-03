# Release 1.2.0 — Автоматическое определение типа клавиатуры

**Дата:** 2026-09-03
**Версия:** 1.2.0 (сборка 1.2.0.0)
**План:** [plans/v1.2.0-keyboard-autodetection.md](plans/v1.2.0-keyboard-autodetection.md)

## Кратко

При подключении клавиатуры приложение теперь автоматически определяет раскладку тремя уровнями: сохранённая привязка пользователя → каталог топ-52 моделей по VID/PID → маркерная эвристика с визардом из двух нажатий. Выбор запоминается и применяется молча при повторных подключениях.

## Новое

### Автоматическое определение раскладки

- **Каталог VID/PID** (`KeyboardCatalog`): 52 популярных модели — Logitech, Razer, Corsair, SteelSeries, Cherry, Keychron, Ducky, Varmilo, Glorious, Royal Kludge, Roccat. Попадание: раскладка применяется и привязка сохраняется молча, имя модели показывается в статус-баре («Клавиатура распознана: Logitech G Pro X»).
- **Маркерная эвристика** (`LayoutHeuristics`): для неизвестных моделей баннер просит нажать Enter цифрового блока (если есть) и клавишу слева от левого Shift; по матрице маркеров предлагается ISO 105 или ANSI 104, при неоднозначности (нет numpad → 60/75/TKL) — ручной выбор.
- **Диалог предложения** (`LayoutProposalDialog`): комбобокс всех раскладок (предложенная предвыбрана), чекбокс «Запомнить для этой клавиатуры» (включён по умолчанию), ОК/Отмена.
- **Персистентность**: привязки в `%AppData%/KeyboardTester/settings.json`, ключ `VID_XXXX&PID_YYYY` (стабилен при смене USB-порта; для ноутбучных ACPI/PS-2 — путь устройства). Ручная смена раскладки при выбранном устройстве обновляет привязку.
- **Ручной запуск**: кнопка «🔍 Определить автоматически» рядом с комбобоксом раскладки.

### UX

- Баннер визарда между тулбаром и контентом: подсказка, индикаторы прогресса (Enter numpad ✔ / Клавиша у Shift ✔), кнопки «Нет цифрового блока» и «Отмена».
- Захват ввода работает во время визарда даже вне тестовой сессии.
- Кнопка «Нет цифрового блока» пропускает первый маркер для компактных клавиатур.

## Изменённые файлы

### Новые

| Файл | Назначение |
|---|---|
| `src/KeyboardTester.Core/Models/KnownKeyboard.cs` | Record записи каталога (VID/PID, бренд, модель, раскладка) |
| `src/KeyboardTester.Core/Models/InputDeviceExtensions.cs` | Хелпер ключа привязки `GetLayoutBindingKey()` |
| `src/KeyboardTester.Core/Dto/LayoutMarkers.cs` | DTO маркеров эвристики |
| `src/KeyboardTester.Core/Dto/LayoutMarkerScanCodes.cs` | Константы скан-кодов маркеров |
| `src/KeyboardTester.Core/Interfaces/IKeyboardCatalog.cs` | Интерфейс каталога |
| `src/KeyboardTester.Core/Interfaces/ILayoutHeuristics.cs` | Интерфейс эвристики |
| `src/KeyboardTester.Core/Interfaces/IDeviceLayoutStore.cs` | Интерфейс хранилища привязок |
| `src/KeyboardTester.Infrastructure/Layouts/KeyboardCatalog.cs` | Каталог топ-52 моделей (код-данные) |
| `src/KeyboardTester.Infrastructure/Layouts/LayoutHeuristics.cs` | Матрица маркерной эвристики |
| `src/KeyboardTester.Application/Services/KeyboardDetectionService.cs` | Конечный автомат визарда + enum `KeyboardDetectionState` |
| `src/KeyboardTester.UI/Views/LayoutProposalDialog.xaml(.cs)` | Диалог предложения раскладки |
| `tests/KeyboardTester.Core.Tests/Layouts/KeyboardCatalogTests.cs` | 10 тестов каталога |
| `tests/KeyboardTester.Core.Tests/Layouts/LayoutHeuristicsTests.cs` | 9 тестов эвристики |
| `tests/KeyboardTester.Core.Tests/Models/InputDeviceExtensionsTests.cs` | 5 тестов ключа привязки |
| `tests/KeyboardTester.Integration.Tests/Services/KeyboardDetectionServiceTests.cs` | 14 тестов автомата |
| `tests/KeyboardTester.Integration.Tests/Services/DeviceLayoutStoreTests.cs` | 8 тестов стора (roundtrip, merge-регресс) |
| `tests/KeyboardTester.Integration.Tests/Detection/MainViewModelDetectionTests.cs` | 8 сквозных тестов связки VM + детекция |

### Изменённые

| Файл | Изменение |
|---|---|
| `src/KeyboardTester.Application/ViewModels/MainViewModel.cs` | Свойства/команды детекции, автоприменение при подключении/выборе, флаг `_isApplyingLayout`, захват при активном визарде |
| `src/KeyboardTester.Application/ServiceCollectionExtensions.cs` | Регистрация `KeyboardDetectionService` |
| `src/KeyboardTester.UI/Services/SettingsService.cs` | `DeviceLayouts` в `AppSettings`, реализация `IDeviceLayoutStore`, merge-защита `Save()`, relaxed JSON-энкодер |
| `src/KeyboardTester.UI/App.xaml.cs` | DI: `IKeyboardCatalog`, `ILayoutHeuristics`, `IDeviceLayoutStore` |
| `src/KeyboardTester.UI/Views/MainWindow.xaml` | Ряд баннера детекции, кнопка автодетекта, статус распознанной клавиатуры |
| `src/KeyboardTester.UI/Views/MainWindow.xaml.cs` | Подписка `ProposalRequested` → `LayoutProposalDialog` |
| `src/KeyboardTester.UI/Resources/Strings.resx`, `Strings.Designer.cs` | 13 ключей локализации |
| `tests/KeyboardTester.Integration.Tests/Helpers/FakeRawInputCapture.cs` | Перегрузка `Press(scanCode, timestamp, devicePath)` |
| `Directory.Build.props` | Версия 1.2.0 |
| `README.md` | Раздел автодетекта, убраны упоминания QuestPDF/PDF |
| `KeyboardTesterDevelopPlan.md` | Этап 8 (v1.2.0) со статусом |

## Тестирование

- 156 тестов зелёные (92 Core.Tests + 64 Integration.Tests; было 96).
- Сборка без предупреждений (`TreatWarningsAsErrors=true`).
- Регресс-защита: `Save(debounce, theme)` из диалога настроек не стирает привязки устройств.

## Известные ограничения

- PID в каталоге соответствуют wired-версиям моделей; Bluetooth-варианты тех же моделей могут иметь другие PID и не распознаются каталогом (сработает визард).
- Эвристика не различает 60%/75%/TKL без цифрового блока — предлагается ручной выбор.
- Каталог не претендует на полноту: 52 проверенные модели; ошибка каталога не фатальна — ручная смена раскладки обновляет привязку.

## Обновление

Portable ZIP с GitHub Releases (заменой файлов); настройки `%AppData%/KeyboardTester/` сохраняются, новая секция `DeviceLayouts` создаётся автоматически.
