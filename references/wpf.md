# WPF-специфика

Дополняет SKILL.md, mvvm.md и xaml.md. Всё здесь — только о том, что уникально для WPF.

## Содержание

- Потоки и Dispatcher
- DependencyProperty и пользовательские контролы
- Привязки: подводные камни WPF
- Ресурсы: StaticResource vs DynamicResource
- Freezable
- События и утечки
- Виртуализация и списки

## Потоки и Dispatcher

- Обращение к UI-элементам и `ObservableCollection` — только из UI-потока. Из фонового кода — `Dispatcher.InvokeAsync` (не `Invoke` без необходимости: синхронный вызов — риск взаимоблокировки).
- `PropertyChanged` из фонового потока WPF маршалит сам (для скалярных свойств), но коллекции — нет: добавление в `ObservableCollection` из фонового потока = исключение. Правило: коллекции мутируем только в UI-потоке, даже если «работало».
- Не проверять `CheckAccess()` вручную по всему коду — правильное место маршалинга: граница фонового сервиса и VM.
- `async void` допустим только в обработчиках событий View; исключение в нём роняет процесс — оборачивать тело в общий SafeExecute.

## DependencyProperty и пользовательские контролы

```csharp
public static readonly DependencyProperty ProgressProperty =
    DependencyProperty.Register(
        nameof(Progress),
        typeof(double),
        typeof(ProgressRing),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

public double Progress
{
    get => (double)GetValue(ProgressProperty);
    set => SetValue(ProgressProperty, value);
}
```

- CLR-обёртка DP — только `GetValue`/`SetValue`, без дополнительной логики и полей: WPF часто вызывает `SetValue` напрямую, логика в обёртке будет пропущена. Побочные эффекты — в `PropertyChangedCallback` метаданных.
- Значения по умолчанию — в метаданных, а не в конструкторе контрола.
- Не хранить состояние контрола в приватных полях, если оно влияет на отображение, — это рассинхрон с привязками. Состояние, видимое снаружи, — только DP.
- UserControl — для композиции; CustomControl (с `Themes/Generic.xaml`, `DefaultStyleKey`, частями `PART_*`) — только для переиспользуемых контролов с переопределяемым шаблоном. Не создавать CustomControl «для одного экрана».

## Привязки: подводные камни WPF

- Ошибки привязок не бросают исключений — смотреть окно Output (категория «привязка данных»). При ревью/отладке включать трассировку (`PresentationTraceSources.TraceLevel=High`) временно; в коде её не оставлять.
- Привязка к обычному POCO-свойству без INPC обновляется один раз — не оставлять такое нечаянно; либо INPC, либо осознанный `Mode=OneTime`.
- `Binding` без `Path` (`{Binding}`) — только там, где весь объект и есть источник (шаблоны элементов); в основной разметке — явный Path.
- `ElementName` и `RelativeSource` — осознанно; глубокие `RelativeSource AncestorType` — признак, что элементу нужна своя VM или `x:Name`.
- `DataContext` не назначать в XAML через `new ViewModel()` — VM приходит из DI/навигатора (см. mvvm.md). В шаблонах `d:DataContext` для дизайнера — приветствуется.

## Ресурсы: StaticResource vs DynamicResource

- По умолчанию — `StaticResource` (быстрее, ошибки ключа видны при загрузке).
- `DynamicResource` — только для того, что реально меняется в рантайме: темы, системные цвета (`SystemColors`), локализация с горячим переключением.
- Ресурс должен быть объявлен до использования по дереву/словарям; «иногда не находит ресурс» — почти всегда порядок MergedDictionaries.

## Freezable

- Кисти, геометрии, трансформации, создаваемые в коде и не меняющиеся, — `Freeze()`: меньше память, безопасное разделение между потоками.
- Ресурсы из XAML уже заморожены — не клонировать их без необходимости.

## События и утечки

- Классические источники утечек WPF: статические события (`SystemEvents`, `CompositionTarget.Rendering`), подписка долгоживущего объекта на короткоживущий, `DependencyPropertyDescriptor.AddValueChanged` без снятия, `TextBox` с отключённым лимитом Undo.
- Подписка есть — есть отписка (Dispose, `Closed`, `Unloaded`). Либо `WeakEventManager`.
- `CompositionTarget.Rendering` — не использовать как таймер логики; только кадровая визуализация и только пока нужна.
- `DispatcherTimer` — для UI-периодики; `System.Threading.Timer`/`PeriodicTimer` — для фоновой, с маршалингом обратно.

## Виртуализация и списки

- Длинные списки — виртуализация включена по умолчанию у `ListBox`/`ListView`/`DataGrid`; ломают её: `ScrollViewer.CanContentScroll="False"`, обёртка списка в `StackPanel`, высота строк в разнобой, `Grouping` (отключает UI-виртуализацию). При ревью списка с >100 элементов — проверять эти три пункта.
- `DataGrid`: колонки — явные `DataGridTextColumn` и т.п. с `Binding`, `AutoGenerateColumns="False"`, если формат/заголовки важны. Не править ячейки через code-behind — только шаблоны и стили.
- Массовое обновление `ObservableCollection` в цикле — тысячи `CollectionChanged` и фриз UI: либо приостановить уведомления (расширение-обёртка `AddRange`), либо пересоздать коллекцию и поднять одно `PropertyChanged`.
