using CommunityToolkit.Mvvm.ComponentModel;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Application.ViewModels;

/// <summary>
/// ViewModel отдельной клавиши виртуальной клавиатуры.
/// </summary>
public partial class KeyViewModel : ObservableObject
{
    /// <summary>
    /// Создаёт ViewModel для указанной физической клавиши.
    /// </summary>
    public KeyViewModel(PhysicalKey physicalKey)
    {
        PhysicalKey = physicalKey ?? throw new ArgumentNullException(nameof(physicalKey));
    }

    /// <summary>
    /// Физическая клавиша.
    /// </summary>
    public PhysicalKey PhysicalKey { get; }

    /// <summary>
    /// Текущий диагностический статус клавиши.
    /// </summary>
    [ObservableProperty]
    private KeyStatus _status = KeyStatus.NotTested;

    /// <summary>
    /// Нажата ли клавиша в текущий момент.
    /// </summary>
    [ObservableProperty]
    private bool _isPressed;

    /// <summary>
    /// Общее количество нажатий.
    /// </summary>
    [ObservableProperty]
    private int _pressCount;
}
