using KeyboardTester.Core.Dto;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using Microsoft.Extensions.Logging;

namespace KeyboardTester.Application.Services;

/// <summary>
/// Состояния визарда автоматического определения раскладки.
/// </summary>
public enum KeyboardDetectionState
{
    /// <summary>Детекция не активна.</summary>
    Idle,

    /// <summary>Ожидание нажатия Enter цифрового блока (или кнопки «Нет numpad»).</summary>
    WaitingNumpadEnter,

    /// <summary>Ожидание нажатия клавиши слева от левого Shift.</summary>
    WaitingLeftShift,

    /// <summary>Маркеры собраны — показать диалог предложения (с ручным выбором).</summary>
    Proposal,
}

/// <summary>
/// Конечный автомат автоматического определения раскладки клавиатуры
/// по маркерным нажатиям (план v1.2.0):
/// WaitingNumpadEnter → WaitingLeftShift → Proposal.
/// Нажатия клавиш фильтруются по DevicePath целевого устройства.
/// </summary>
public sealed class KeyboardDetectionService
{
    private readonly ILayoutHeuristics _heuristics;
    private readonly ILogger<KeyboardDetectionService> _logger;

    private bool _numpadEnterPressed;
    private bool _numpadMarkedAbsent;
    private bool _isoNeighborSeen;
    private bool _ansiNeighborSeen;

    /// <summary>
    /// Создаёт сервис детекции.
    /// </summary>
    public KeyboardDetectionService(
        ILayoutHeuristics heuristics,
        ILogger<KeyboardDetectionService> logger)
    {
        _heuristics = heuristics ?? throw new ArgumentNullException(nameof(heuristics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Целевое устройство детекции (null вне активной детекции).</summary>
    public InputDevice? Target { get; private set; }

    /// <summary>Текущее состояние автомата.</summary>
    public KeyboardDetectionState State { get; private set; } = KeyboardDetectionState.Idle;

    /// <summary>Идёт ли детекция (любое состояние кроме Idle).</summary>
    public bool IsActive => State != KeyboardDetectionState.Idle;

    /// <summary>Раскладка, предложенная эвристикой (валидна в состоянии Proposal).</summary>
    public KeyboardLayout? SuggestedLayout { get; private set; }

    /// <summary>Событие смены состояния автомата.</summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Запускает детекцию для целевого устройства. Повторный вызов
    /// перезапускает автомат и сбрасывает собранные маркеры.
    /// </summary>
    public void Start(InputDevice target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
        ResetMarkers();
        SuggestedLayout = null;
        TransitionTo(KeyboardDetectionState.WaitingNumpadEnter);
        _logger.LogInformation("Запущено автоопределение раскладки для {DevicePath}", target.DevicePath);
    }

    /// <summary>
    /// Отменяет детекцию и возвращается в Idle. Маркеры сбрасываются.
    /// </summary>
    public void Cancel()
    {
        if (!IsActive)
        {
            return;
        }

        Target = null;
        ResetMarkers();
        SuggestedLayout = null;
        TransitionTo(KeyboardDetectionState.Idle);
        _logger.LogInformation("Автоопределение раскладки отменено");
    }

    /// <summary>
    /// Пользователь отметил отсутствие цифрового блока — переходит
    /// к ожиданию клавиши слева от Shift (если ещё не перешёл).
    /// </summary>
    public void MarkNumpadAbsent()
    {
        if (State is not (KeyboardDetectionState.WaitingNumpadEnter or KeyboardDetectionState.WaitingLeftShift))
        {
            return;
        }

        _numpadMarkedAbsent = true;
        if (State == KeyboardDetectionState.WaitingNumpadEnter)
        {
            TransitionTo(KeyboardDetectionState.WaitingLeftShift);
        }
    }

    /// <summary>
    /// Проглатывает нажатие клавиши: маркерные скан-коды от целевого
    /// устройства обновляют маркеры, остальные игнорируются.
    /// </summary>
    public void HandleKeyPress(RawKeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (!IsActive || e.DevicePath != Target?.DevicePath)
        {
            return;
        }

        switch (e.ScanCode)
        {
            case LayoutMarkerScanCodes.NumpadEnter:
                _numpadEnterPressed = true;
                if (State == KeyboardDetectionState.WaitingNumpadEnter)
                {
                    TransitionTo(KeyboardDetectionState.WaitingLeftShift);
                }

                break;

            case LayoutMarkerScanCodes.IsoLeftShiftNeighbor:
                _isoNeighborSeen = true;
                TryAdvanceToProposal();
                break;

            case LayoutMarkerScanCodes.AnsiLeftShiftNeighbor:
                _ansiNeighborSeen = true;
                TryAdvanceToProposal();
                break;
        }
    }

    /// <summary>
    /// Подтверждает раскладку, выбранную в диалоге предложения.
    /// Детекция завершается и возвращается в Idle.
    /// </summary>
    public void Confirm(KeyboardLayout layout)
    {
        Target = null;
        ResetMarkers();
        SuggestedLayout = null;
        TransitionTo(KeyboardDetectionState.Idle);
        _logger.LogInformation("Автоопределение завершено, выбрана раскладка {Layout}", layout);
    }

    private void TryAdvanceToProposal()
    {
        if (State != KeyboardDetectionState.WaitingLeftShift)
        {
            return;
        }

        var markers = new LayoutMarkers(_numpadEnterPressed, _numpadMarkedAbsent, _isoNeighborSeen, _ansiNeighborSeen);
        SuggestedLayout = _heuristics.SuggestLayout(markers);

        // Даже при null (неоднозначно) переходим в Proposal: диалог
        // откроется с ручным выбором без предвыбранной раскладки.
        TransitionTo(KeyboardDetectionState.Proposal);
        _logger.LogInformation(
            "Маркеры собраны: numpad={Numpad}, absent={Absent}, iso={Iso}, ansi={Ansi}; предложение: {Suggestion}",
            _numpadEnterPressed,
            _numpadMarkedAbsent,
            _isoNeighborSeen,
            _ansiNeighborSeen,
            SuggestedLayout?.ToString() ?? "ручной выбор");
    }

    private void ResetMarkers()
    {
        _numpadEnterPressed = false;
        _numpadMarkedAbsent = false;
        _isoNeighborSeen = false;
        _ansiNeighborSeen = false;
    }

    private void TransitionTo(KeyboardDetectionState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
