namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Сервис локализации строк интерфейса.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Получить локализованную строку по ключу.
    /// </summary>
    /// <param name="key">Ключ строки.</param>
    /// <returns>Локализованная строка.</returns>
    string this[string key] { get; }

    /// <summary>
    /// Получить локализованную строку по ключу.
    /// </summary>
    /// <param name="key">Ключ строки.</param>
    /// <returns>Локализованная строка.</returns>
    string GetString(string key);

    /// <summary>
    /// Получить локализованную строку по ключу с подстановкой аргументов.
    /// </summary>
    /// <param name="key">Ключ строки.</param>
    /// <param name="args">Аргументы форматирования.</param>
    /// <returns>Локализованная строка.</returns>
    string GetString(string key, params object[] args);
}
