using System.Globalization;
using System.Resources;
using KeyboardTester.Core.Interfaces;

namespace KeyboardTester.UI.Services;

/// <summary>
/// Сервис локализации строк интерфейса на основе встроенных resx-ресурсов.
/// Нейтральный ресурс содержит русские строки и используется как fallback
/// для любой культуры операционной системы.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;

    /// <summary>
    /// Создаёт сервис локализации, читающий строки из ресурса
    /// <c>KeyboardTester.UI.Resources.Strings</c>.
    /// </summary>
    public LocalizationService()
    {
        _resourceManager = new ResourceManager(
            "KeyboardTester.UI.Resources.Strings",
            typeof(LocalizationService).Assembly);
    }

    /// <inheritdoc />
    public string this[string key] => GetString(key);

    /// <inheritdoc />
    public string GetString(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }

    /// <inheritdoc />
    public string GetString(string key, params object[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(args);

        string format = GetString(key);
        return args.Length == 0 ? format : string.Format(CultureInfo.CurrentCulture, format, args);
    }
}
