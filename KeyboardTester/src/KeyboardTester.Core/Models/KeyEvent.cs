namespace KeyboardTester.Core.Models;

/// <summary>
/// Событие клавиши, полученное от устройства ввода, с высокоточной меткой времени.
/// </summary>
public sealed record KeyEvent(
    Guid Id,
    uint VirtualKeyCode,
    uint ScanCode,
    string KeyName,
    long TimestampMicroseconds,
    bool IsKeyDown,
    string? DevicePath)
{
    /// <summary>
    /// Вычисляет время удержания клавиши в микросекундах.
    /// </summary>
    /// <param name="releaseEvent">Событие отпускания клавиши. Должно иметь <see cref="IsKeyDown"/> == false.</param>
    /// <returns>Разность меток времени в микросекундах.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если <paramref name="releaseEvent"/> не является событием отпускания.</exception>
    public long GetDurationMicroseconds(KeyEvent releaseEvent)
    {
        if (releaseEvent.IsKeyDown)
        {
            throw new ArgumentException("Событие отпускания клавиши должно иметь IsKeyDown == false.", nameof(releaseEvent));
        }

        return releaseEvent.TimestampMicroseconds - TimestampMicroseconds;
    }
}
