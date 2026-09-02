# KeyboardTester

Десктопное приложение для Windows 10/11 для тестирования и настройки параметров клавиатуры: проверка работоспособности всех клавиш, выявление залипания и дребезга контактов с демонстрацией графиков работы по каждой клавише.

## Возможности

- Захват событий всех клавиш через Windows Raw Input с микросекундной точностью (QueryPerformanceCounter)
- Виртуальная клавиатура с подсветкой нажатых и проблемных клавиш
- Детекция дребезга (chatter): интервалы < 20 мс — критические, 20–50 мс — умеренные
- Детекция залипания клавиш (удержание сверх настраиваемого порога)
- Графики интервалов между нажатиями и времени удержания по каждой клавише
- Экспорт отчётов в PDF с полной статистикой
- Поддержка нескольких клавиатур (различение по device handle)

## Скриншоты

> TODO: скриншот главного окна с виртуальной клавиатурой

> TODO: скриншот графиков интервалов и удержания

## Стек

- **.NET 9 / WPF** — нативный UI для Windows
- **LiveCharts2** — графики
- **CommunityToolkit.Mvvm** — MVVM
- **Microsoft.Extensions.DependencyInjection** — DI
- **Serilog** — структурированное логирование
- **QuestPDF** — генерация PDF-отчётов
- **xUnit, Moq, FluentAssertions** — тестирование

## Структура решения

```
KeyboardTester/
├── src/
│   ├── KeyboardTester.Core/             # Domain Layer: модели, интерфейсы, перечисления
│   ├── KeyboardTester.Infrastructure/   # Raw Input, хранение, экспорт (PDF)
│   ├── KeyboardTester.Application/      # Оркестрация тестов, анализ дребезга
│   └── KeyboardTester.UI/               # WPF: виды, контролы, конвертеры
├── tests/
│   ├── KeyboardTester.Core.Tests/       # Модульные тесты (net9.0)
│   └── KeyboardTester.Integration.Tests/# Интеграционные тесты (net9.0-windows)
├── Directory.Build.props                # Общие настройки компиляции
├── Directory.Packages.props             # Централизованное управление пакетами (CPM)
└── KeyboardTester.sln
```

## Системные требования

- Windows 10 (1809+) или Windows 11
- Для запуска: .NET 9 Desktop Runtime (или self-contained сборка)
- Для разработки: .NET 9 SDK

## Сборка и тестирование

```bash
# Восстановление зависимостей и сборка всего решения
dotnet build KeyboardTester.sln

# Запуск всех тестов
dotnet test KeyboardTester.sln

# Запуск приложения
dotnet run --project src/KeyboardTester.UI/KeyboardTester.UI.csproj
```

### Portable-сборка (один EXE, без установки .NET)

```powershell
# Release + тесты + ZIP-архив в artifacts/KeyboardTester-<версия>-win-x64.zip
.\build-portable.ps1

# Без прогона тестов (например, когда SDK установлен только для сборки)
.\build-portable.ps1 -SkipTests

# Явно задать версию в имени артефакта
.\build-portable.ps1 -Version v1.0.0
```

Скрипт выполняет `dotnet publish` self-contained single-file (win-x64), удаляет `.pdb`,
укладывает результат в `artifacts/KeyboardTester-<версия>-win-x64/` и создаёт ZIP
для распространения. Версия берётся из `git describe --tags --always`,
при отсутствии git/тегов — `dev`.

### Запуск из VS Code

Открыть папку проекта и нажать **F5** (конфигурация «Launch WPF»):
сборка UI-проекта через задачу `build` и запуск `KeyboardTester.UI.exe`.
Задача `publish-portable` (Terminal → Run Task…) вызывает `build-portable.ps1`.

## Лицензия

Проект распространяется под лицензией MIT. См. файл [LICENSE](LICENSE).
