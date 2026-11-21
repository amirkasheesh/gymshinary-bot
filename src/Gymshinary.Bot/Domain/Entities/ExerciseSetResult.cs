namespace Gymshinary.Bot.Domain.Entities;

public class ExerciseSetResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid ExerciseSnapshotId { get; set; }
    public int SetNumber { get; set; }
    public double Weight { get; set; }
    public int Reps { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
