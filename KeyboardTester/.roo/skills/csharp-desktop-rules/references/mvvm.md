# MVVM: ViewModel, команды, DI, навигация, валидация

Базовый стек: **CommunityToolkit.Mvvm** (source generators) + **Microsoft.Extensions.DependencyInjection** + **Microsoft.Extensions.Logging**. Если проект уже живёт на Prism, ReactiveUI, Caliburn.Micro — не мигрировать молча, следовать принятому стеку.

## Содержание

- View
- ViewModel
- Model
- Команды и SafeExecute
- Межкомпонентная связь (Messenger)
- DI и время жизни
- Навигация и диалоги
- Валидация

## View

- Только XAML + минимальный code-behind. Допустимо в code-behind: чисто визуальная логика (анимации, установка фокуса, drag&drop-отрисовка, работа с Win32-хендлом окна).
- Запрещено в View: обращение к сервисам, БД, файлам, сети; создание ViewModel вручную (`new MainViewModel()` в code-behind — антипаттерн); бизнес-решения в обработчиках.
- Если обработчик события в code-behind длиннее пары строк — это сигнал перенести логику в команду VM.

## ViewModel

```csharp
public partial class OrderViewModel : ObservableObject
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]   // зависимые свойства — атрибутом, не ручным вызовом
    public partial string FirstName { get; set; }

    public string FullName => $"{FirstName} {LastName}";   // вычисляемое — без поля

    [ObservableProperty]
    public partial string LastName { get; set; }
}
```

- Генераторы вместо ручного INPC: `[ObservableProperty]`, `[RelayCommand]`, `[NotifyPropertyChangedFor]`, `[NotifyCanExecuteChangedFor]`. Ручной `OnPropertyChanged` — только там, где генератор не покрывает сценарий.
- VM не наследуется от Model и не содержит доменной логики — только логику представления и оркестрацию сервисов.
- Конструктор лёгкий. Загрузка данных — в `InitializeAsync` (паттерн async-инициализации), вызываемый из навигатора/фабрики VM, не из конструктора View.
- Зависимости — только через конструктор, только интерфейсы (кроме самих VM/моделей). Property injection запрещён.
- VM не создаёт окна, диалоги и контролы. Нужно окно → `INavigationService`, диалог → `IDialogService` (см. ниже).
- Одна VM — одно представление. «Мега-VM» на весь экран с 30 свойствами и 10 командами — делить на дочерние VM по секциям экрана.

## Model

- Доменные сущности — POCO или `record`, без INPC и без UI-типов.
- Редактируемые в UI сущности — оборачивать в VM-обёртку или делать отдельную «редактируемую» модель с INPC; не заставлять весь домен реализовывать INotifyPropertyChanged «на всякий случай».

## Команды и SafeExecute

```csharp
[RelayCommand(CanExecute = nameof(CanSave), AllowConcurrentExecutions = false)]
private async Task SaveAsync(CancellationToken cancellationToken)
    => await _guard.ExecuteAsync(() => SaveCoreAsync(cancellationToken));
```

- Все async-команды — через общий SafeExecute-фасад (`IErrorGuard`/`AsyncCommandGuard`): ловит исключение → `ILogger.LogError` → уведомление пользователю через `INotificationService`. Голый `try/catch` в каждой команде с `MessageBox` запрещён.
- `AllowConcurrentExecutions = false` для команд, где повторный запуск вреден (сохранение, отправка).
- Длительные команды принимают `CancellationToken`; кнопка «Отмена» отменяет токен.
- `CanExecute` — свойство-условие; при изменении условия — `[NotifyCanExecuteChangedFor(nameof(SaveCommand))]`. Не вызывать `SaveCommand.NotifyCanExecuteChanged()` размазанно по коду.
- Обработчики событий (`Click=`) в XAML не использовать, если действие можно выразить командой. Допустимо для чисто визуальных реакций.

## Межкомпонентная связь (Messenger)

- Связь между VM — через `WeakReferenceMessenger` (CommunityToolkit.Mvvm), сообщения — `record`. Не протягивать ссылки VM↔VM и не строить цепочки событий через три уровня.
- VM, подписавшаяся на сообщения, — `ObservableRecipient` с `IsActive = true` или явная регистрация/дерегистрация; не оставлять живых подписок у закрытых окон.

## DI и время жизни

- Регистрация — в одном месте (composition root, обычно `App.xaml.cs` / `Program.cs` + `IServiceCollection`).
- Singleton: сервисы без состояния сессии, логгеры, настройки. Transient: VM окон/страниц. Scoped — только если в проекте уже есть области (редко в desktop).
- Запрещён ServiceLocator (`App.Services.Get<T>()`, статический `ServiceProvider`). Разрешено только на самой границе, куда DI не достаёт (например, активация окон фреймворком) — с комментарием.
- Не прокидывать `IServiceProvider` в VM — это замаскированный ServiceLocator.
- Фабрики для VM с параметрами: `Func<int, OrderViewModel>` или явный интерфейс фабрики.

## Навигация и диалоги

```csharp
public interface INavigationService
{
    void ShowWindow<TViewModel>() where TViewModel : class;
    bool? ShowDialog<TViewModel>() where TViewModel : class;
}

public interface IDialogService
{
    Task<string?> PickFileAsync(string filter);
    Task<bool> ConfirmAsync(string title, string message);
}
```

- VM вызывает только эти интерфейсы; реализация живёт в слое Views и знает про фреймворк.
- Маппинг VM → View: DataTemplate без `x:Key` по типу VM или явный словарь в composition root. Не искать View через строки-имена.
- `MessageBox` (WPF) / нативные диалоги напрямую из VM запрещены — только `IDialogService`. Иначе VM нетестируема.

## Валидация

- `ObservableValidator` + атрибуты (`[Required]`, `[Range]`, `[CustomValidation]`) на генерируемых свойствах; сложная межполевая валидация — методом через `ValidateProperty`/кастомный атрибут.
- Ошибки показываются привязкой к `INotifyDataErrorInfo`, а не ручной подкраской контролов из code-behind.
- Валидировать в VM. Валидация в View (ValidationRules в WPF) — только форматная (например, «только цифры») и только если она не дублирует доменные правила.
- Команда сохранения в `CanExecute` проверяет `!HasErrors` — не полагаться на то, что «UI не даст ввести».
