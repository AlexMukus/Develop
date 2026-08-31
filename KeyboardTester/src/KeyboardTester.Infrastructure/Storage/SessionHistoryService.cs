using System.Text.Json;
using System.Text.Json.Serialization;
using KeyboardTester.Core.Interfaces;
using KeyboardTester.Core.Models;
using KeyboardTester.Infrastructure.Storage.JsonConverters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeyboardTester.Infrastructure.Storage;

/// <summary>
/// Сервис сохранения и загрузки истории тестовых сессий в JSON.
/// </summary>
public sealed class SessionHistoryService : ISessionHistoryService, IDisposable
{
    private readonly string _filePath;
    private readonly ILogger<SessionHistoryService> _logger;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private List<TestSession> _sessions = new();

    /// <inheritdoc />
    public event EventHandler? SessionsChanged;

    /// <summary>
    /// Создаёт экземпляр сервиса истории сессий.
    /// </summary>
    public SessionHistoryService(string? baseDirectory = null, ILogger<SessionHistoryService>? logger = null)
    {
        _logger = logger ?? NullLogger<SessionHistoryService>.Instance;
        _filePath = BuildFilePath(baseDirectory);
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new PhysicalKeyDictionaryConverter() },
        };

        Load();
    }

    /// <inheritdoc />
    public IReadOnlyList<TestSession> GetAllSessions()
    {
        lock (_lock)
        {
            return _sessions.ToList();
        }
    }

    /// <inheritdoc />
    public TestSession? GetSession(Guid sessionId)
    {
        lock (_lock)
        {
            return _sessions.FirstOrDefault(s => s.Id == sessionId);
        }
    }

    /// <inheritdoc />
    public void SaveSession(TestSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_lock)
        {
            int index = _sessions.FindIndex(s => s.Id == session.Id);
            if (index >= 0)
            {
                _sessions[index] = session;
            }
            else
            {
                _sessions.Add(session);
            }

            SaveInternal();
        }

        _logger.LogInformation("Сессия сохранена: {SessionName} ({SessionId})", session.Name, session.Id);
        SessionsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void DeleteSession(Guid sessionId)
    {
        lock (_lock)
        {
            int removed = _sessions.RemoveAll(s => s.Id == sessionId);
            if (removed == 0)
            {
                return;
            }

            SaveInternal();
        }

        _logger.LogInformation("Сессия удалена: {SessionId}", sessionId);
        SessionsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void UpdateSessionNotes(Guid sessionId, string notes)
    {
        lock (_lock)
        {
            int index = _sessions.FindIndex(s => s.Id == sessionId);
            if (index < 0)
            {
                return;
            }

            _sessions[index] = _sessions[index] with { Notes = notes };
            SaveInternal();
        }

        _logger.LogInformation("Обновлены заметки сессии: {SessionId}", sessionId);
        SessionsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            SaveInternal();
        }
    }

    private static string BuildFilePath(string? baseDirectory)
    {
        baseDirectory ??= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directory = Path.Combine(baseDirectory, "KeyboardTester");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "history.json");
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _sessions = new List<TestSession>();
            return;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            _sessions = JsonSerializer.Deserialize<List<TestSession>>(json, _jsonOptions) ?? new List<TestSession>();
            _logger.LogInformation("Загружено сессий из истории: {Count}", _sessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось загрузить историю сессий из {FilePath}", _filePath);
            _sessions = new List<TestSession>();
        }
    }

    private void SaveInternal()
    {
        try
        {
            string json = JsonSerializer.Serialize(_sessions, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось сохранить историю сессий в {FilePath}", _filePath);
        }
    }
}
