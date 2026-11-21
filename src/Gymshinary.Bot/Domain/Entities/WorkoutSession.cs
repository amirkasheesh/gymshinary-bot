using Gymshinary.Bot.Domain.Enums;

namespace Gymshinary.Bot.Domain.Entities;

public class WorkoutSession
{
    private readonly List<SessionExerciseSnapshot> _exerciseSnapshots = new();
    private readonly List<ExerciseSetResult> _setResults = new();

    public Guid Id { get; init; } = Guid.NewGuid();
    public long UserId { get; init; }
    public Guid TemplateId { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public SessionStatus Status { get; private set; } = SessionStatus.Active;
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; private set; }

    public IReadOnlyList<SessionExerciseSnapshot> ExerciseSnapshots => _exerciseSnapshots.AsReadOnly();
    public IReadOnlyList<ExerciseSetResult> SetResults => _setResults.AsReadOnly();

    public WorkoutSession() { }

    public WorkoutSession(long userId, Guid templateId, string templateName)
    {
        UserId = userId;
        TemplateId = templateId;
        TemplateName = templateName;
        Status = SessionStatus.Active;
        StartedAt = DateTime.UtcNow;
    }

    public void AddExerciseSnapshot(SessionExerciseSnapshot snapshot)
    {
        if (Status != SessionStatus.Active)
        {
            throw new InvalidOperationException("Нельзя добавлять упражнения в неактивную сессию");
        }

        _exerciseSnapshots.Add(snapshot);
    }

    public void AddSetResult(ExerciseSetResult result)
    {
        if (Status != SessionStatus.Active)
        {
            throw new InvalidOperationException("Нельзя добавлять результаты подходов в неактивную сессию");
        }

        _setResults.Add(result);
    }

    public void Complete()
    {
        if (Status != SessionStatus.Active)
        {
            throw new InvalidOperationException("Сессия уже завершена или прервана");
        }

        Status = SessionStatus.Completed;
        EndedAt = DateTime.UtcNow;
    }

    public void Interrupt()
    {
        if (Status != SessionStatus.Active)
        {
            throw new InvalidOperationException("Нельзя прервать неактивную сессию");
        }

        Status = SessionStatus.Interrupted;
        EndedAt = DateTime.UtcNow;
    }

    public WorkoutSession Clone()
    {
        var clone = new WorkoutSession(UserId, TemplateId, TemplateName)
        {
            Id = Id,
            Status = Status,
            StartedAt = StartedAt,
            EndedAt = EndedAt
        };

        foreach (var snap in _exerciseSnapshots)
        {
            clone._exerciseSnapshots.Add(new SessionExerciseSnapshot
            {
                Id = snap.Id,
                SessionId = snap.SessionId,
                ExerciseId = snap.ExerciseId,
                OrderIndex = snap.OrderIndex,
                ExerciseName = snap.ExerciseName,
                PlannedSets = snap.PlannedSets,
                MinReps = snap.MinReps,
                MaxReps = snap.MaxReps
            });
        }

        foreach (var set in _setResults)
        {
            clone._setResults.Add(new ExerciseSetResult
            {
                Id = set.Id,
                SessionId = set.SessionId,
                ExerciseSnapshotId = set.ExerciseSnapshotId,
                SetNumber = set.SetNumber,
                Weight = set.Weight,
                Reps = set.Reps,
                RecordedAt = set.RecordedAt
            });
        }

        return clone;
    }
}
