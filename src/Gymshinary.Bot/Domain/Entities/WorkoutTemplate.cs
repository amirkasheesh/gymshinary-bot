namespace Gymshinary.Bot.Domain.Entities;

public class WorkoutTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProgramId { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TemplateExercise> Exercises { get; set; } = new();
}
