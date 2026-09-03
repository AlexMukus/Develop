# AGENTS.md — KeyboardTester

Файл для AI-агентов и разработчиков: обзор проекта, команды сборки/тестирования, конвенции кода и процессы.

## Обзор проекта

**KeyboardTester** — десктопное приложение для Windows 10/11 для тестирования клавиатуры: проверка работоспособности клавиш, выявление дребезга контактов (chatter) и залипания, ghosting-тест (NKRO/6KRO), история сессий сравнения «до/после».

- Язык: **C# 12 / .NET 9**, UI на **WPF** (нативный, только Windows).
- Текущая версия: **1.1.0** (задаётся в `Directory.Build.props`, читается в рантайме через `AppVersion.Current`).
- Лицензия: MIT.
- Язык кодовой базы: идентификаторы на английском, **XML-документация и комментарии — на русском**. UI и логирование — на русском (ресурсы в `src/KeyboardTester.UI/Resources/Strings.Designer.cs`).
- Захват клавиш — через Windows **Raw Input API** с микросекундной точностью (QueryPerformanceCounter).

> Внимание: `README.md` упоминает QuestPDF/PDF-экспорт — это устарело. По уточнённым требованиям (`KeyboardTesterDevelopPlan.md`, раздел 2.0) **экспорт отчётов исключён из scope**, QuestPDF не используется. Актуальный источник требований — `KeyboardTesterDevelopPlan.md` и `Release 1.1.0.md`.

## Структура решения

Решение `KeyboardTester.sln`, 6 проектов. Зависимости строго слоистые: Core ← Infrastructure, Core ← Application, UI → Application + Infrastructure.

```
src/
├── KeyboardTester.Core/             # Domain Layer (net9.0): модели, enum'ы, DTO, интерфейсы.
│                                    #   НЕ зависит от других проектов и от Windows-API.
│   ├── Models/                      #   KeyEvent, PhysicalKey, KeyStatistics, ChatterEvent,
│   │                                #   TestSession, DebounceSettings, GhostingTestResult, InputDevice
│   ├── Enums/                       #   KeyStatus, ChatterSeverity, KeyboardLayout, AppTheme, KeyboardConnectionType
│   ├── Dto/                         #   RawKeyEventArgs, KeyStatisticsUpdatedEventArgs, DebounceResult
│   └── Interfaces/                  #   IRawInputCapture, IDebounceAnalyzer, IStatisticsEngine,
│                                    #   IGhostingTestEngine, ILayoutProvider, ISessionHistoryService,
│                                    #   IThemeService, ILocalizationService
├── KeyboardTester.Infrastructure/   # Infrastructure Layer (net9.0-windows, UseWPF=true)
│   ├── Input/                       #   Raw Input: RawInputCapture, NativeMethods (P/Invoke),
│   │                                #   RawInputStructures/RawInputConstants, INativeMethods для моков
│   ├── Analysis/                    #   DebounceAnalyzer, StatisticsEngine, GhostingTestEngine
│   ├── Layouts/                     #   LayoutProvider (раскладки клавиатур)
│   ├── Storage/                     #   SessionHistoryService (JSON), PhysicalKeyDictionaryConverter
│   └── Logging/                     #   LoggingConfigurator (Serilog → файл)
├── KeyboardTester.Application/      # Application Layer (net9.0): оркестрация, ViewModels (MVVM)
│   ├── Services/TestSessionService.cs
│   ├── ViewModels/                  #   MainViewModel, TestSessionViewModel, KeyViewModel, KeyDataPoint, SessionComparison
│   └── ServiceCollectionExtensions.cs  # AddKeyboardTesterApplication()
└── KeyboardTester.UI/               # Presentation Layer (net9.0-windows, WinExe, WPF)
    ├── App.xaml.cs                  #   Точка входа: сборка DI-контейнера, темы, MainWindow
    ├── Views/                       #   MainWindow, SettingsDialog, AboutDialog
    ├── Controls/                    #   VirtualKeyboardControl, StatisticsPanel, ChartsPanelControl,
    │                                #   SessionHistoryPanel, GhostingTestControl
    ├── Converters/                  #   Value-конвертеры (KeyStatus→Brush, PressCount→Badge и др.)
    ├── Themes/                      #   Dark.xaml, Light.xaml, ThemeManager
    ├── Resources/                   #   Styles.xaml (общие стили на DynamicResource), Strings (локализация)
    └── Services/                    #   SettingsService, LocalizationService, AppVersion
tests/
├── KeyboardTester.Core.Tests/        # Модульные тесты (net9.0-windows): Analysis, Layouts, Models
└── KeyboardTester.Integration.Tests/ # Интеграционные тесты (net9.0-windows): Raw Input (через
                                      #   FakeNativeMethods), сессии, ViewModels, конвертеры UI
                                      #   (проект ссылается на все 4 src-проекта, включая UI)
```

## Технологический стек и ключевые зависимости

Версии всех пакетов задаются **централизованно** в `Directory.Packages.props` (CPM, включён `CentralPackageTransitivePinningEnabled`). В `.csproj` `PackageReference` указывается **без версии**; добавление нового пакета = запись `PackageVersion` в `Directory.Packages.props` + `PackageReference` в проект.

- `Microsoft.Extensions.DependencyInjection` / `Logging` — DI-контейнер и логирование (все сервисы — singleton, граф строится в `App.OnStartup`).
- `CommunityToolkit.Mvvm` — MVVM (source generators `[ObservableProperty]`, `[RelayCommand]`).
- `LiveChartsCore.SkiaSharpView.WPF` (2.0.0-rc2) — графики. `SkiaSharp.Views.WPF` запинен на 2.88.9: 3.x тянет .NETFramework-only активы (NU1701) — не обновлять без проверки.
- `Serilog` + `Serilog.Sinks.File` — логи в `%AppData%/KeyboardTester/logs/keyboard-tester.log`.
- Тесты: **xUnit + Moq + FluentAssertions**.
- Настройки приложения: JSON в `%AppData%/KeyboardTester/` (`SettingsService`).

## Команды сборки и тестирования

Требуется **.NET 9 SDK** и Windows (WPF). Все команды — из корня репозитория.

```bash
# Сборка всего решения
dotnet build KeyboardTester.sln

# Все тесты (96 тестов: 61 модульных + 35 интеграционных)
dotnet test KeyboardTester.sln

# Запуск приложения
dotnet run --project src/KeyboardTester.UI/KeyboardTester.UI.csproj
```

### Portable-сборка (self-contained single-file EXE)

```powershell
.\build-portable.ps1                  # Release + тесты + ZIP в artifacts/
.\build-portable.ps1 -SkipTests       # без прогона тестов
.\build-portable.ps1 -Version v1.2.3  # явная версия в имени артефакта
```

Скрипт запускает `dotnet test` (если не `-SkipTests`; падение тестов прерывает сборку), затем `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true`, удаляет `.pdb` и упаковывает результат в `artifacts/KeyboardTester-<версия>-win-x64.zip`. Версия по умолчанию — `git describe --tags --always`, fallback — `dev`.

### VS Code

Задачи (Terminal → Run Task…): `build` (UI-проект), `build-solution`, `test`, `publish-portable`. F5 — конфигурация «Launch WPF».

## Конвенции кода

Зафиксированы в `.editorconfig` и `Directory.Build.props`:

- `TreatWarningsAsErrors=true`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=12`. **Сборка не должна давать ни одного предупреждения.**
- **File-scoped namespaces** (warning), обязательные фигурные скобки (`csharp_prefer_braces=true:warning`), явные модификаторы доступа (`always:warning`).
- Интерфейсы с префиксом `I` (warning); `var` — только когда тип очевиден.
- Отступы 4 пробела, CRLF, UTF-8, final newline (для `.json/.md/.csproj` — 2 пробела).
- XML-документация (`/// <summary>`) на **русском** для публичных типов и членов; `ArgumentNullException.ThrowIfNull` в начале публичных методов.
- Новые сервисы регистрируются в DI: Infrastructure/presentation-сервисы — в `App.OnStartup` (`src/KeyboardTester.UI/App.xaml.cs`), Application-сервисы и ViewModels — в `ServiceCollectionExtensions.AddKeyboardTesterApplication()`.
- Архитектурное правило: **Core не ссылается ни на что**, Infrastructure и Application ссылаются только на Core, UI компонует всё. Не вводить ссылки в обратную сторону.

### Скилл `csharp-desktop-rules` (подробные правила C#/WPF/MVVM/XAML)

В репозитории лежит скилл с детальными стандартами кодирования: `.roo/skills/csharp-desktop-rules/` (`SKILL.md` + каталог `references/`). Это обязательный источник правил наряду с `.editorconfig`.

**Когда применять.** При любом написании, ревью и рефакторинге C#/XAML-кода проекта — сначала прочитать `.roo/skills/csharp-desktop-rules/SKILL.md` (ядро: десять главных правил, именование, чек-лист код-ревью). Явный вызов не требуется — скилл применяется ко всем задачам на код в этом репозитории.

**Справочники читать по задаче** (не загружать всё сразу):

| Задача | Файл |
|---|---|
| ViewModel, команды, DI, навигация, диалоги, валидация | `references/mvvm.md` |
| XAML: разметка, биндинги, ресурсы, стили, конвертеры | `references/xaml.md` |
| WPF-специфика: Dispatcher, DependencyProperty, Freezable, виртуализация, утечки | `references/wpf.md` |
| Производительность и память: списки, изображения, подписки | `references/performance.md` |
| Avalonia | `references/avalonia.md` — **не используется**, проект WPF-only; не читать |

**Приоритет при конфликте.** Конвенции этого AGENTS.md и `.editorconfig` важнее общих правил скилла. Известные осознанные расхождения (не исправлять под скилл):

- XML-документация на русском **обязательна** для публичных типов (в скилле — «по необходимости»).
- Все сервисы регистрируются как **singleton** (общее правило скилла про transient VM здесь не применяется: граф вытягивается в `App.OnStartup` до показа окна — см. «Особенности» ниже).
- Комментарии и логирование — на русском.

При обнаружении нового расхождения — следовать проекту и явно предупредить пользователя, не переписывать молча.

### Особенности, которые легко сломать

- `RawInputCapture` и `StatisticsEngine` должны **создаваться на UI-потоке** (нужны `HwndSource`/`SynchronizationContext`): весь граф зависимостей вытягивается в `App.OnStartup` до показа окна через разрешение `MainViewModel`.
- Win32-вызовы изолированы за интерфейсом `INativeMethods` (реализация `WindowsNativeMethods`) — это позволяет тестировать `RawInputCapture` через `FakeNativeMethods` без реального окна.
- Общие стили лежат в `Resources/Styles.xaml` и подключены в `App.xaml` отдельно от тем: `ThemeManager` при смене темы удаляет словари тем, Styles.xaml должен переживать смену.
- Тесты WPF-компонентов требуют STA-потока — см. хелпер `tests/KeyboardTester.Integration.Tests/Helpers/StaHelper.cs`.

## Тестирование

- Фреймворк: xUnit (`[Fact]`/`[Theory]`), утверждения через **FluentAssertions**, моки через **Moq**.
- Именование тестов: `Метод_Сценарий_ОжидаемыйРезультат` (например, `Analyze_TwoKeyDowns_10msApart_ReportsCriticalChatter`).
- Модульные тесты (`KeyboardTester.Core.Tests`) — чистая логика: анализ дребезга, статистика, раскладки, модели. Ссылаются на Core и Infrastructure.
- Интеграционные (`KeyboardTester.Integration.Tests`) — Raw Input через фейки (`Helpers/FakeNativeMethods.cs`, `FakeRawInputCapture.cs`), полный цикл сессии, ViewModels, UI-конвертеры. Ссылаются на все 4 проекта, включая UI.
- Перед завершением работы: `dotnet test KeyboardTester.sln` должен быть зелёным (сейчас 96 тестов). Оба тестовых проекта — `net9.0-windows`, поэтому тесты запускаются только на Windows.

## CI/CD и релизный процесс

- `.github/workflows/build.yml` — на каждый push/PR (windows-latest, .NET 9): restore → build (Release) → test → publish portable → артефакт `KeyboardTester-portable`.
- `.github/workflows/release.yml` — по тегу `v*`: полная сборка и тесты, ZIP `KeyboardTester-<тег>-win-x64.zip`, changelog из `git log` между тегами, GitHub Release через `softprops/action-gh-release`.
- Релиз: обновить `<Version>` в `Directory.Build.props`, запушить тег `vX.Y.Z` — остальное делает workflow. Дистрибуция — только portable ZIP, обновления вручную с GitHub.

## Безопасность и данные

- Приложение хранит данные только локально: настройки, история сессий (JSON) и логи — в `%AppData%/KeyboardTester/`. Сетевых вызовов нет.
- P/Invoke-объявления (`NativeMethods.cs`, `RawInputStructures.cs`) — точно сверять сигнатуры и размеры структур с Win32-документацией; ошибки здесь дают трудноуловимые сбои в рантайме.
- Не логировать и не коммитить чувствительные данные; `artifacts/`, `bin/`, `obj/`, логи и результаты тестов исключены в `.gitignore`.

## Полезные документы в репозитории

- `README.md` — краткое описание и команды (частично устарел: PDF-экспорт отменён).
- `KeyboardTesterDevelopPlan.md` — актуальный план разработки и уточнённые требования по этапам.
- `Release 1.1.0.md` — changelog текущей версии со ссылками на файлы.
- `КлавТестер.docx` / `Требования к проекту КлавТестер.docx` (+ извлечённые `docx_content.txt`, `req_content.txt`) — исходные требования.
