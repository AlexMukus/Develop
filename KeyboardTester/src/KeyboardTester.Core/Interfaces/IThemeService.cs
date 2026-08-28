using KeyboardTester.Core.Enums;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Сервис управления темой оформления приложения.
/// </summary>
public interface IThemeService
{
    /// <summary>Событие смены темы.</summary>
    event EventHandler? ThemeChanged;

    /// <summary>Текущая тема.</summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Установить тему оформления.
    /// </summary>
    /// <param name="theme">Новая тема.</param>
    void SetTheme(AppTheme theme);

    /// <summary>Получить текущую системную тему.</summary>
    AppTheme GetSystemTheme();
}
