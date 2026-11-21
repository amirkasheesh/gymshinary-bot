using Gymshinary.Bot.Domain.Entities;

namespace Gymshinary.Bot.Services;

public interface IWorkoutRepository
{
    Task SaveProgramAsync(WorkoutProgram program);
    Task<WorkoutProgram?> GetProgramByIdAsync(Guid programId);
    Task<List<WorkoutProgram>> GetProgramsByUserIdAsync(long userId);

    Task SaveTemplateAsync(WorkoutTemplate template);
    Task<WorkoutTemplate?> GetTemplateByIdAsync(Guid templateId);
    Task<List<WorkoutTemplate>> GetTemplatesByProgramIdAsync(Guid programId);

    Task<WorkoutSession> CreateSessionIfNoActiveAsync(WorkoutSession session);
    Task<WorkoutSession?> GetSessionByIdAsync(Guid sessionId);
    Task<WorkoutSession?> GetActiveSessionByUserIdAsync(long userId);
    Task<WorkoutSession> UpdateSessionAtomicAsync(Guid sessionId, Action<WorkoutSession> updateAction);
    Task<(WorkoutSession Session, OperatorActionLog Log)> InterruptSessionAtomicAsync(Guid sessionId, long operatorId, string reason);

    Task<List<OperatorActionLog>> GetOperatorActionLogsBySessionIdAsync(Guid sessionId);
}
