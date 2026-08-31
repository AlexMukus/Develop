using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.Application.ViewModels;

/// <summary>
/// Точка данных для live-графиков (интервалы между нажатиями / время удержания).
/// </summary>
public sealed record KeyDataPoint(
    TimeSpan RelativeTime,
    double ValueMs,
    PhysicalKey? Key,
    KeyStatus? Status = null);
