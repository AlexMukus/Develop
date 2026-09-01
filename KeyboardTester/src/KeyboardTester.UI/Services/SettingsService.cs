using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeyboardTester.Core.Enums;
using KeyboardTester.Core.Models;

namespace KeyboardTester.UI.Services;

/// <summary>
/// Настройки приложения: пороги диагностики и тема оформления.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Пороги дебаунса и диагностики.</summary>
    public DebounceSettings Debounce { get; set; } = new();

    /// <summary>Тема оформления.</summary>
    public AppTheme Theme { get; set; } = AppTheme.System;
}

/// <summary>
/// Загрузка и сохранение настроек приложения в JSON-файл
/// (%AppData%/KeyboardTester/settings.json).
/// Устойчив к отсутствию или повреждению файла — в этом случае
/// используются значения по умолчанию.
/// </summary>
public sealed class SettingsService
{
    private static readonly string DefaultDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeyboardTester");

    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Создаёт сервис настроек и сразу загружает их из файла.
    /// </summary>
    /// <param name="baseDirectory">Каталог настроек; null — %AppData%/KeyboardTester.</param>
    public SettingsService(string? baseDirectory = null)
    {
        string directory = baseDirectory ?? DefaultDirectory;
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "settings.json");
        Current = Load();
    }

    /// <summary>Путь к файлу настроек.</summary>
    public string FilePath => _filePath;

    /// <summary>Текущие настройки.</summary>
    public AppSettings Current { get; private set; } = new();

    /// <summary>
    /// Сохраняет новые настройки в файл.
    /// </summary>
    public void Save(DebounceSettings debounce, AppTheme theme)
    {
        ArgumentNullException.ThrowIfNull(debounce);

        Current = new AppSettings { Debounce = debounce, Theme = theme };
        try
        {
            string json = JsonSerializer.Serialize(Current, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception)
        {
            // Ошибка записи настроек не должна ронять приложение.
        }
    }

    private AppSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return new AppSettings();
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }
}
