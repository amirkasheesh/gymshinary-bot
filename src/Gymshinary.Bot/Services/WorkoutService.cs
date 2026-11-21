using Gymshinary.Bot.Configuration;
using Gymshinary.Bot.Domain.Entities;
using Gymshinary.Bot.Domain.Models;
using Microsoft.Extensions.Options;

namespace Gymshinary.Bot.Services;

public class WorkoutService : IWorkoutService, IOperatorWorkoutService
{
    private readonly IWorkoutRepository _repository;
    private readonly OperatorConfiguration _operatorConfig;

    public WorkoutService(IWorkoutRepository repository, IOptions<OperatorConfiguration> operatorOptions)
    {
        _repository = repository;
        _operatorConfig = operatorOptions.Value;
    }

    public async Task<WorkoutProgram> CreateProgramAsync(long userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название программы не может быть пустым", nameof(name));
        }

        var program = new WorkoutProgram
        {
            UserId = userId,
            Name = name.Trim()
        };

        await _repository.SaveProgramAsync(program);
        return program;
    }

    public async Task<WorkoutTemplate> CreateTemplateAsync(long userId, Guid programId, string name, List<TemplateExercise> exercises)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название шаблона не может быть пустым", nameof(name));
        }

        var program = await _repository.GetProgramByIdAsync(programId);
        if (program == null || program.UserId != userId)
        {
            throw new UnauthorizedAccessException("Нет доступа к указанной программе или программа не найдена");
        }

        var template = new WorkoutTemplate(programId, userId, name.Trim(), exercises);

        await _repository.SaveTemplateAsync(template);
        return template;
    }

    public async Task<WorkoutProgram?> GetProgramAsync(long userId, Guid programId)
    {
        var program = await _repository.GetProgramByIdAsync(programId);
        if (program == null || program.UserId != userId)
        {
            return null;
        }

        return program;
    }

    public async Task<WorkoutTemplate?> GetTemplateAsync(long userId, Guid templateId)
    {
        var template = await _repository.GetTemplateByIdAsync(templateId);
        if (template == null || template.UserId != userId)
        {
            return null;
        }

        return template;
    }

    public async Task<WorkoutSession> StartSessionAsync(long userId, Guid templateId)
    {
        var template = await _repository.GetTemplateByIdAsync(templateId);
        if (template == null || template.UserId != userId)
        {
            throw new UnauthorizedAccessException("Нет доступа к указанному шаблону или шаблон не найден");
        }

        var session = new WorkoutSession(userId, template.Id, template.Name);

        foreach (var ex in template.Exercises.OrderBy(e => e.OrderIndex))
        {
            session.AddExerciseSnapshot(new SessionExerciseSnapshot
            {
                SessionId = session.Id,
                ExerciseId = ex.Id,
                OrderIndex = ex.OrderIndex,
                ExerciseName = ex.ExerciseName,
                PlannedSets = ex.PlannedSets,
                MinReps = ex.MinReps,
                MaxReps = ex.MaxReps
            });
        }

        return await _repository.CreateSessionIfNoActiveAsync(session);
    }

    public async Task<WorkoutSession?> GetSessionAsync(long userId, Guid sessionId)
    {
        var session = await _repository.GetSessionByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
        {
            return null;
        }

        return session;
    }

    public async Task<ExerciseSetResult> RecordSetAsync(long userId, Guid sessionId, Guid exerciseSnapshotId, double? weight, int reps)
    {
        if (reps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reps), "Количество повторений должно быть положительным числом");
        }

        if (weight.HasValue && (!double.IsFinite(weight.Value) || weight.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Рабочий вес должен быть неотрицательным конечным числом");
        }

        ExerciseSetResult? createdResult = null;

        await _repository.UpdateSessionAtomicAsync(sessionId, session =>
        {
            if (session.UserId != userId)
            {
                throw new UnauthorizedAccessException("Нет доступа к указанной сессии или сессия не найдена");
            }

            var snapshot = session.ExerciseSnapshots.FirstOrDefault(s => s.Id == exerciseSnapshotId);
            if (snapshot == null)
            {
                throw new ArgumentException("Упражнение не найдено в плане текущей сессии", nameof(exerciseSnapshotId));
            }

            double finalWeight;
            if (weight.HasValue)
            {
                finalWeight = weight.Value;
            }
            else
            {
                var previousSet = session.SetResults
                    .Where(r => r.ExerciseSnapshotId == exerciseSnapshotId)
                    .OrderByDescending(r => r.SetNumber)
                    .FirstOrDefault();

                if (previousSet == null)
                {
                    throw new ArgumentException("Для первого подхода необходимо указать рабочий вес", nameof(weight));
                }

                finalWeight = previousSet.Weight;
            }

            var setNumber = session.SetResults.Count(r => r.ExerciseSnapshotId == exerciseSnapshotId) + 1;

            createdResult = new ExerciseSetResult
            {
                SessionId = session.Id,
                ExerciseSnapshotId = exerciseSnapshotId,
                SetNumber = setNumber,
                Weight = finalWeight,
                Reps = reps,
                RecordedAt = DateTime.UtcNow
            };

            session.AddSetResult(createdResult);
        });

        return createdResult!;
    }

    public async Task<WorkoutSession> CompleteSessionAsync(long userId, Guid sessionId)
    {
        return await _repository.UpdateSessionAtomicAsync(sessionId, session =>
        {
            if (session.UserId != userId)
            {
                throw new UnauthorizedAccessException("Нет доступа к указанной сессии или сессия не найдена");
            }

            session.Complete();
        });
    }

    public async Task<SessionTechnicalStatus> GetTechnicalStatusAsync(long operatorId, Guid sessionId)
    {
        if (!_operatorConfig.AllowedOperatorIds.Contains(operatorId))
        {
            throw new UnauthorizedAccessException("Пользователь не обладает полномочиями технического оператора");
        }

        var session = await _repository.GetSessionByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException("Сессия не найдена");
        }

        return new SessionTechnicalStatus
        {
            SessionId = session.Id,
            Status = session.Status,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt
        };
    }

    public async Task<SessionTechnicalStatus> InterruptSessionAsync(long operatorId, Guid sessionId, string reason)
    {
        if (!_operatorConfig.AllowedOperatorIds.Contains(operatorId))
        {
            throw new UnauthorizedAccessException("Пользователь не обладает полномочиями технического оператора");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Причина служебного прерывания не может быть пустой", nameof(reason));
        }

        var (session, _) = await _repository.InterruptSessionAtomicAsync(sessionId, operatorId, reason);

        return new SessionTechnicalStatus
        {
            SessionId = session.Id,
            Status = session.Status,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt
        };
    }
}
