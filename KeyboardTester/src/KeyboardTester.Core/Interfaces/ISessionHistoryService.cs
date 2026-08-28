using KeyboardTester.Core.Models;

namespace KeyboardTester.Core.Interfaces;

/// <summary>
/// Сервис истории тестовых сессий.
/// </summary>
public interface ISessionHistoryService
{
    /// <summary>Событие изменения списка сессий.</summary>
    event EventHandler? SessionsChanged;

    /// <summary>Получить все сохранённые сессии.</summary>
    IReadOnlyList<TestSession> GetAllSessions();

    /// <summary>Сохранить сессию.</summary>
    void SaveSession(TestSession session);

    /// <summary>
    /// Удалить сессию.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    void DeleteSession(Guid sessionId);

    /// <summary>
    /// Получить сессию по идентификатору.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <returns>Сессия или null.</returns>
    TestSession? GetSession(Guid sessionId);

    /// <summary>
    /// Обновить заметки сессии.
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии.</param>
    /// <param name="notes">Новые заметки.</param>
    void UpdateSessionNotes(Guid sessionId, string notes);
}
