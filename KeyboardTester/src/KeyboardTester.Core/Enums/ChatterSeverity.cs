namespace KeyboardTester.Core.Enums;

/// <summary>
/// Степень тяжести дребезга контактов (chatter) по интервалу между событиями.
/// </summary>
public enum ChatterSeverity
{
    /// <summary>Норма, интервал больше 80 мс.</summary>
    None,

    /// <summary>Лёгкий дребезг, 50–80 мс.</summary>
    Mild,

    /// <summary>Умеренный дребезг, 20–50 мс.</summary>
    Moderate,

    /// <summary>Критический дребезг, меньше 20 мс.</summary>
    Critical
}
