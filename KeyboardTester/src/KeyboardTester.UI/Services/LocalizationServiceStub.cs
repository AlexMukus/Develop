using System.Globalization;
using KeyboardTester.Core.Interfaces;

namespace KeyboardTester.UI.Services;

/// <summary>
/// Временная заглушка сервиса локализации: возвращает сам ключ.
/// Будет заменена настоящей реализацией на resx-ресурсах на этапе 5.
/// </summary>
public sealed class LocalizationServiceStub : ILocalizationService
{
    /// <inheritdoc />
    public string this[string key] => key;

    /// <inheritdoc />
    public string GetString(string key)
    {
        return key;
    }

    /// <inheritdoc />
    public string GetString(string key, params object[] args)
    {
        return args.Length == 0 ? key : string.Format(CultureInfo.CurrentCulture, key, args);
    }
}
