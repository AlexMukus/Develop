# Avalonia-специфика

Дополняет SKILL.md, mvvm.md и xaml.md. Расписаны только отличия от WPF и подводные камни Avalonia 11+.

## Содержание

- Карта отличий от WPF
- Компилируемые привязки (x:CompileBindings)
- StyledProperty
- Стили и селекторы (вместо триггеров WPF)
- Потоки и Dispatcher.UIThread
- MVVM-стек: CommunityToolkit vs ReactiveUI
- Кроссплатформенная дисциплина

## Карта отличий от WPF

| WPF | Avalonia |
|---|---|
| `DependencyProperty` | `StyledProperty<T>` (или `DirectProperty` для полей) |
| `Trigger` / `DataTrigger` в XAML | Селекторы стилей с псевдоклассами (`:pointerover`) и `Classes` |
| `StaticResource` / `DynamicResource` | Те же имена, но предпочтительны `{StaticResource}` + темы `DynamicResource` |
| `RelativeSource AncestorType=...` | `$parent[DataGrid]`, `$parent[Window]` в привязках |
| `x:Type` | `x:Type` (11+) |
| DataGrid в коробке | Отдельный пакет `Avalonia.Controls.DataGrid` |
| `Dispatcher.InvokeAsync` | `Dispatcher.UIThread.Post` / `InvokeAsync` |
| Поведение по умолчанию «как в Windows» | Кроссплатформенность — проверять на всех ОС |

При переносе XAML из WPF механическая замена пространств имён не работает: триггеры, `x:Static`-константы, некоторые разметочные расширения и контролы имеют другой синтаксис — переписывать осознанно, а не «добиваться компиляции».

## Компилируемые привязки (x:CompileBindings)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:App.ViewModels"
             x:Class="App.Views.OrderView"
             x:DataType="vm:OrderViewModel"
             x:CompileBindings="True">
```

- `x:CompileBindings="True"` + `x:DataType` — обязательны во всех представлениях и шаблонах: опечатки в Path ловятся при компиляции, а не молча в рантайме, плюс производительность выше.
- Внутри `DataTemplate` — свой `x:DataType` типа элемента.
- Где компилируемая привязка невозможна (позднее связывание), — осознанно `x:CompileBindings="False"` на ветке с комментарием.
- После рефакторинга VM (переименования) компилятор покажет все сломанные привязки — это и есть проверка.

## StyledProperty

```csharp
public static readonly StyledProperty<double> ProgressProperty =
    AvaloniaProperty.Register<ProgressRing, double>(nameof(Progress));

public double Progress
{
    get => GetValue(ProgressProperty);
    set => SetValue(ProgressProperty, value);
}
```

- Состояние, видимое снаружи и влияющее на отображение, — `StyledProperty`; внутреннее неотображаемое с INPC — `DirectProperty` с backing-полем.
- Как и в WPF: CLR-обёртка без логики; побочные эффекты — через подписку на изменение свойства (`GetObservable`/`AddClassHandler`), не в сеттере.

## Стили и селекторы (вместо триггеров WPF)

```xml
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{DynamicResource PrimaryBrush}"/>
</Style>
<Style Selector="Button.primary:pointerover /template/ ContentPresenter">
    <Setter Property="Background" Value="{DynamicResource PrimaryHoverBrush}"/>
</Style>
```

- Триггеров WPF нет: состояние — псевдоклассы (`:pointerover`, `:pressed`, `:disabled`, `:focus`) и классы (`Classes="primary"` / `^primary`).
- Вариант оформления — через `Classes` на элементе + селектор в теме, а не через привязку `Background` к VM.
- Селекторы максимально конкретные; «звёздные» селекторы (`*`) запрещены — медленно и ломает чужие контролы.
- `/template/` — обращение внутрь шаблона; применять только к частям своего ControlTheme.

## Потоки и Dispatcher.UIThread

- UI-элементы и `ObservableCollection` — только UI-поток; из фона — `Dispatcher.UIThread.Post`/`InvokeAsync` или `await` с возвратом в контекст.
- В отличие от WPF, не рассчитывать на автоматический маршаллинг уведомлений для коллекций: правило одно — коллекции мутируем из UI-потока.
- `TopLevel` — точка доступа к платформенным сервисам (clipboard, storage, launcher): `TopLevel.GetTopLevel(control)`. В VM платформенные вызовы — только через интерфейсы сервисов (см. mvvm.md).

## MVVM-стек: CommunityToolkit vs ReactiveUI

- По умолчанию — CommunityToolkit.Mvvm (как и в WPF): единый стек для обоих фреймворков.
- ReactiveUI — только если проект уже на нём. Не смешивать `ReactiveObject` и `ObservableObject` в соседних VM одного проекта: это два разных стиля подписок и валидации.
- В ReactiveUI-проекте: `WhenAnyValue` вместо `[NotifyPropertyChangedFor]`, `ReactiveCommand` вместо `[RelayCommand]`, подписки — через `WhenActivated` с автоотпиской.

## Кроссплатформенная дисциплина

- Windows-специфичный код (Win32, реестр, пути `C:\`) — за интерфейсом сервиса с реализацией на платформу; в общем коде — ни P/Invoke, ни `OperatingSystem.IsWindows()`-лапши без обоснования.
- Пути — `Path.Combine`/`Path.Join`, переносы строк — `Environment.NewLine`; не зашивать `\` и `\r\n`.
- Шрифты и метрики отличаются между ОС: не фиксировать размеры впритык, тестировать Layout на всех целевых платформах.
- Горячие клавиши и меню — учитывать macOS (`Cmd` вместо `Ctrl`), если macOS заявлена платформой.
