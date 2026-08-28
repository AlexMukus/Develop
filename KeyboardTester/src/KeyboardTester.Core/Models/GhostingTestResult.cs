namespace KeyboardTester.Core.Models;

/// <summary>
/// Результат тестирования на ghosting / NKRO.
/// </summary>
public sealed record GhostingTestResult(
    DateTime Timestamp,
    IReadOnlyList<PhysicalKey> PressedKeys,
    IReadOnlyList<PhysicalKey> RegisteredKeys,
    bool IsNKeyRollover,
    int MaxSimultaneousKeys);
