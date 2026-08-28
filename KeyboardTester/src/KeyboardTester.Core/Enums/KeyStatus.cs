namespace KeyboardTester.Core.Enums;

/// <summary>
/// Статус клавиши по результатам диагностики.
/// </summary>
public enum KeyStatus
{
    /// <summary>Клавиша ещё не тестировалась (серая).</summary>
    NotTested,

    /// <summary>Клавиша работает корректно (зелёная).</summary>
    Ok,

    /// <summary>Умеренные отклонения (жёлтая).</summary>
    Warning,

    /// <summary>Критическая проблема (красная).</summary>
    Critical
}
