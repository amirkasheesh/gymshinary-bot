using Gymshinary.Bot.Domain.Entities;

namespace Gymshinary.Bot.Services;

public interface IWorkoutService
{
    Task<WorkoutProgram> CreateProgramAsync(long userId, string name);
    Task<WorkoutTemplate> CreateTemplateAsync(long userId, Guid programId, string name, List<TemplateExercise> exercises);
    Task<WorkoutProgram?> GetProgramAsync(long userId, Guid programId);
    Task<WorkoutTemplate?> GetTemplateAsync(long userId, Guid templateId);

    Task<WorkoutSession> StartSessionAsync(long userId, Guid templateId);
    Task<WorkoutSession?> GetSessionAsync(long userId, Guid sessionId);
    Task<ExerciseSetResult> RecordSetAsync(long userId, Guid sessionId, Guid exerciseSnapshotId, double? weight, int reps);
    Task<WorkoutSession> CompleteSessionAsync(long userId, Guid sessionId);
}
