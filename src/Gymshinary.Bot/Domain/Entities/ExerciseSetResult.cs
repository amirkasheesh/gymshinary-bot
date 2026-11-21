namespace Gymshinary.Bot.Domain.Entities;

public class ExerciseSetResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public Guid ExerciseSnapshotId { get; init; }
    public int SetNumber { get; init; }
    public double Weight { get; init; }
    public int Reps { get; init; }
    public DateTime RecordedAt { get; init; } = DateTime.UtcNow;
}
