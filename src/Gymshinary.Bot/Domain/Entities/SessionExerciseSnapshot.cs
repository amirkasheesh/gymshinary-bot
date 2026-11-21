namespace Gymshinary.Bot.Domain.Entities;

public class SessionExerciseSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid ExerciseId { get; set; }
    public int OrderIndex { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int PlannedSets { get; set; }
    public int MinReps { get; set; }
    public int MaxReps { get; set; }
}
