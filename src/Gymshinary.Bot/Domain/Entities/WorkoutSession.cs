using Gymshinary.Bot.Domain.Enums;

namespace Gymshinary.Bot.Domain.Entities;

public class WorkoutSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public List<SessionExerciseSnapshot> ExerciseSnapshots { get; set; } = new();
    public List<ExerciseSetResult> SetResults { get; set; } = new();
}
