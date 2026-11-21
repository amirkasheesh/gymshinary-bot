namespace Gymshinary.Bot.Domain.Entities;

public class WorkoutTemplate
{
    private List<TemplateExercise> _exercises = new();

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProgramId { get; init; }
    public long UserId { get; init; }
    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<TemplateExercise> Exercises => _exercises.AsReadOnly();

    public WorkoutTemplate() { }

    public WorkoutTemplate(Guid programId, long userId, string name, IEnumerable<TemplateExercise>? exercises = null)
    {
        ProgramId = programId;
        UserId = userId;
        Name = name;

        if (exercises != null)
        {
            foreach (var ex in exercises)
            {
                AddExercise(ex);
            }
        }
    }

    public void AddExercise(TemplateExercise exercise)
    {
        _exercises.Add(CreateExercise(exercise));
    }

    private TemplateExercise CreateExercise(TemplateExercise exercise)
    {
        return new TemplateExercise
        {
            TemplateId = Id,
            OrderIndex = exercise.OrderIndex,
            ExerciseName = exercise.ExerciseName,
            PlannedSets = exercise.PlannedSets,
            MinReps = exercise.MinReps,
            MaxReps = exercise.MaxReps
        };
    }

    public void ReplaceExercises(IEnumerable<TemplateExercise> newExercises)
    {
        var existingIds = _exercises.Select(exercise => exercise.Id).ToHashSet();
        var replacement = new List<TemplateExercise>();
        foreach (var exercise in newExercises)
        {
            var keepId = exercise.TemplateId == Id && existingIds.Remove(exercise.Id);
            replacement.Add(keepId ? exercise : CreateExercise(exercise));
        }
        _exercises = replacement;
    }

    internal WorkoutTemplate Copy()
    {
        return new WorkoutTemplate
        {
            Id = Id,
            ProgramId = ProgramId,
            UserId = UserId,
            Name = Name,
            _exercises = new List<TemplateExercise>(_exercises)
        };
    }
}
