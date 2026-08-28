using KeyboardTester.Core.Enums;

namespace KeyboardTester.Core.Models;

/// <summary>
/// Данные тестовой сессии.
/// </summary>
public sealed record TestSession(
    Guid Id,
    string Name,
    DateTime StartTime,
    DateTime? EndTime,
    KeyboardLayout Layout,
    TimeSpan Duration,
    IReadOnlyDictionary<PhysicalKey, KeyStatistics> Statistics,
    string? Notes);
