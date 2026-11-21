namespace Gymshinary.Bot.Domain.Entities;

public class SessionExerciseSnapshot
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public Guid ExerciseId { get; init; }
    public int OrderIndex { get; init; }
    public string ExerciseName { get; init; } = string.Empty;
    public int PlannedSets { get; init; }
    public int MinReps { get; init; }
    public int MaxReps { get; init; }
}
