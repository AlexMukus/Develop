using KeyboardTester.Core.Enums;

namespace KeyboardTester.Core.Models;

/// <summary>
/// Событие дребезга (chatter) с меткой времени и тяжестью.
/// </summary>
public sealed record ChatterEvent(
    long TimestampMicroseconds,
    double IntervalMs,
    ChatterSeverity Severity);
