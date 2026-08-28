using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Dto;

/// <summary>
/// Аргументы события обновления статистики клавиши.
/// </summary>
public class KeyStatisticsUpdatedEventArgs : EventArgs
{
    /// <summary>Клавиша, статистика которой обновилась.</summary>
    public PhysicalKey Key { get; init; } = null!;

    /// <summary>Актуальная статистика клавиши.</summary>
    public KeyStatistics Statistics { get; init; } = null!;
}
