namespace Gymshinary.Bot.Domain.Entities;

public class WorkoutProgram
{
    private readonly List<WorkoutTemplate> _templates = new();

    public Guid Id { get; init; } = Guid.NewGuid();
    public long UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public IReadOnlyList<WorkoutTemplate> Templates => _templates.AsReadOnly();

    internal WorkoutProgram CopyWithTemplates(IEnumerable<WorkoutTemplate> templates)
    {
        var result = new WorkoutProgram { Id = Id, UserId = UserId, Name = Name, CreatedAt = CreatedAt };
        result._templates.AddRange(templates);
        return result;
    }

    public void AddTemplate(WorkoutTemplate template)
    {
        if (template.ProgramId != Id)
        {
            throw new ArgumentException("Шаблон привязан к другой программе", nameof(template));
        }

        _templates.Add(template);
    }
}
