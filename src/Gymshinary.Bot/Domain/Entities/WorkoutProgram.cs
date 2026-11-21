namespace Gymshinary.Bot.Domain.Entities;

public class WorkoutProgram
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<WorkoutTemplate> Templates { get; set; } = new();
}
