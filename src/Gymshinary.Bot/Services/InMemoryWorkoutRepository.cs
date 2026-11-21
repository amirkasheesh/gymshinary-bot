using System.Collections.Concurrent;
using Gymshinary.Bot.Domain.Entities;
using Gymshinary.Bot.Domain.Enums;

namespace Gymshinary.Bot.Services;

public class InMemoryWorkoutRepository : IWorkoutRepository
{
    private readonly Dictionary<Guid, WorkoutProgram> _programs = new();
    private readonly Dictionary<Guid, WorkoutTemplate> _templates = new();
    private readonly ConcurrentDictionary<Guid, WorkoutSession> _sessions = new();
    private readonly List<OperatorActionLog> _operatorLogs = new();
    private readonly object _lock = new();

    public bool FailOnOperatorLogSave { get; set; }

    public Task SaveProgramAsync(WorkoutProgram program)
    {
        lock (_lock)
        {
            foreach (var template in program.Templates)
            {
                _templates.TryAdd(template.Id, template.Copy());
            }
            _programs[program.Id] = program.CopyWithTemplates([]);
        }
        return Task.CompletedTask;
    }

    public Task<WorkoutProgram?> GetProgramByIdAsync(Guid programId)
    {
        lock (_lock)
        {
            return Task.FromResult(_programs.TryGetValue(programId, out var program) ? ReadProgram(program) : null);
        }
    }

    public Task<List<WorkoutProgram>> GetProgramsByUserIdAsync(long userId)
    {
        lock (_lock)
        {
            var programs = _programs.Values.Where(p => p.UserId == userId).Select(ReadProgram).ToList();
            return Task.FromResult(programs);
        }
    }

    private WorkoutProgram ReadProgram(WorkoutProgram program) =>
        program.CopyWithTemplates(ReadTemplates(program.Id));

    public Task SaveTemplateAsync(WorkoutTemplate template)
    {
        lock (_lock)
        {
            _templates[template.Id] = template.Copy();
        }
        return Task.CompletedTask;
    }

    public Task<WorkoutTemplate?> GetTemplateByIdAsync(Guid templateId)
    {
        lock (_lock)
        {
            return Task.FromResult(_templates.GetValueOrDefault(templateId)?.Copy());
        }
    }

    public Task<List<WorkoutTemplate>> GetTemplatesByProgramIdAsync(Guid programId)
    {
        lock (_lock)
        {
            return Task.FromResult(ReadTemplates(programId));
        }
    }

    private List<WorkoutTemplate> ReadTemplates(Guid programId) =>
        _templates.Values.Where(t => t.ProgramId == programId).Select(t => t.Copy()).ToList();

    public Task<WorkoutSession> CreateSessionIfNoActiveAsync(WorkoutSession session)
    {
        lock (_lock)
        {
            var hasActive = _sessions.Values.Any(s => s.UserId == session.UserId && s.Status == SessionStatus.Active);
            if (hasActive)
            {
                throw new InvalidOperationException("У пользователя уже есть активная тренировочная сессия");
            }

            var stored = session.Clone();
            _sessions[stored.Id] = stored;
            return Task.FromResult(stored.Clone());
        }
    }

    public Task<WorkoutSession?> GetSessionByIdAsync(Guid sessionId)
    {
        lock (_lock)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session?.Clone());
        }
    }

    public Task<WorkoutSession?> GetActiveSessionByUserIdAsync(long userId)
    {
        lock (_lock)
        {
            var activeSession = _sessions.Values.FirstOrDefault(s => s.UserId == userId && s.Status == SessionStatus.Active);
            return Task.FromResult(activeSession?.Clone());
        }
    }

    public Task<WorkoutSession> UpdateSessionAtomicAsync(Guid sessionId, Action<WorkoutSession> updateAction)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var originalSession))
            {
                throw new KeyNotFoundException("Сессия не найдена");
            }

            var workingCopy = originalSession.Clone();
            updateAction(workingCopy);

            _sessions[sessionId] = workingCopy;
            return Task.FromResult(workingCopy.Clone());
        }
    }

    public Task<(WorkoutSession Session, OperatorActionLog Log)> InterruptSessionAtomicAsync(Guid sessionId, long operatorId, string reason)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var originalSession))
            {
                throw new KeyNotFoundException("Сессия не найдена");
            }

            var previousSession = originalSession.Clone();
            var workingCopy = originalSession.Clone();
            workingCopy.Interrupt();

            var log = new OperatorActionLog
            {
                SessionId = sessionId,
                OperatorId = operatorId,
                Reason = reason.Trim(),
                Timestamp = DateTime.UtcNow
            };

            _sessions[sessionId] = workingCopy;

            try
            {
                SaveOperatorLogInternal(log);
            }
            catch
            {
                _sessions[sessionId] = previousSession;
                throw;
            }

            return Task.FromResult((workingCopy.Clone(), log));
        }
    }

    private void SaveOperatorLogInternal(OperatorActionLog log)
    {
        if (FailOnOperatorLogSave)
        {
            throw new InvalidOperationException("Сбой при сохранении записи действия оператора в журнал аудита");
        }

        _operatorLogs.Add(log);
    }

    public Task<List<OperatorActionLog>> GetOperatorActionLogsBySessionIdAsync(Guid sessionId)
    {
        lock (_lock)
        {
            var logs = _operatorLogs.Where(l => l.SessionId == sessionId).ToList();
            return Task.FromResult(logs);
        }
    }
}
