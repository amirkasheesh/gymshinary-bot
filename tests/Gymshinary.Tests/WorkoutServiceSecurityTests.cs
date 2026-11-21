using Gymshinary.Bot.Configuration;
using Gymshinary.Bot.Domain.Entities;
using Gymshinary.Bot.Domain.Enums;
using Gymshinary.Bot.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gymshinary.Tests;

public class WorkoutServiceSecurityTests
{
    private const long UserA = 1001;
    private const long UserB = 1002;
    private const long OperatorId = 9999;
    private const long NonOperatorId = 8888;

    private readonly WorkoutService _service;
    private readonly IOperatorWorkoutService _operatorService;
    private readonly InMemoryWorkoutRepository _repository;

    public WorkoutServiceSecurityTests()
    {
        _repository = new InMemoryWorkoutRepository();
        var operatorConfig = Options.Create(new OperatorConfiguration
        {
            AllowedOperatorIds = new List<long> { OperatorId }
        });
        _service = new WorkoutService(_repository, operatorConfig);
        _operatorService = _service;
    }

    [Fact]
    public async Task HappyPath_UserCreatesProgramTemplateRunsSessionAndCompletes()
    {
        var program = await _service.CreateProgramAsync(UserA, "Силовая программа");
        var exercises = new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Жим лежа", PlannedSets = 3, MinReps = 5, MaxReps = 8 },
            new() { OrderIndex = 2, ExerciseName = "Приседания", PlannedSets = 3, MinReps = 5, MaxReps = 8 }
        };
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "День А", exercises);

        Assert.All(template.Exercises, e => Assert.Equal(template.Id, e.TemplateId));

        var savedProgram = await _service.GetProgramAsync(UserA, program.Id);
        Assert.NotNull(savedProgram);
        Assert.Single(savedProgram.Templates);
        Assert.Equal("День А", savedProgram.Templates[0].Name);

        var session = await _service.StartSessionAsync(UserA, template.Id);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Equal(2, session.ExerciseSnapshots.Count);
        Assert.All(session.ExerciseSnapshots, s => Assert.Equal(session.Id, s.SessionId));

        var firstSnapshot = session.ExerciseSnapshots.First(s => s.ExerciseName == "Жим лежа");

        var firstSet = await _service.RecordSetAsync(UserA, session.Id, firstSnapshot.Id, 100.0, 6);
        Assert.Equal(1, firstSet.SetNumber);
        Assert.Equal(100.0, firstSet.Weight);
        Assert.Equal(6, firstSet.Reps);
        Assert.Equal(session.Id, firstSet.SessionId);

        var secondSet = await _service.RecordSetAsync(UserA, session.Id, firstSnapshot.Id, null, 5);
        Assert.Equal(2, secondSet.SetNumber);
        Assert.Equal(100.0, secondSet.Weight);
        Assert.Equal(5, secondSet.Reps);

        var completedSession = await _service.CompleteSessionAsync(UserA, session.Id);
        Assert.Equal(SessionStatus.Completed, completedSession.Status);
        Assert.NotNull(completedSession.EndedAt);
    }

    [Fact]
    public async Task AddExercise_DoesNotMutateOriginalExercise_AndGeneratesUniqueIdsForTemplates()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var originalExercises = new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Жим", PlannedSets = 3, MinReps = 5, MaxReps = 5 }
        };

        var template1 = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон 1", originalExercises);
        var template2 = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон 2", originalExercises);

        Assert.Equal(template1.Id, template1.Exercises[0].TemplateId);
        Assert.Equal(template2.Id, template2.Exercises[0].TemplateId);
        Assert.NotEqual(template1.Exercises[0].TemplateId, template2.Exercises[0].TemplateId);

        Assert.NotEqual(template1.Exercises[0].Id, template2.Exercises[0].Id);
        Assert.NotEqual(Guid.Empty, template1.Exercises[0].Id);
        Assert.NotEqual(Guid.Empty, template2.Exercises[0].Id);

        Assert.Equal(Guid.Empty, originalExercises[0].TemplateId);
    }

    [Fact]
    public void ReplaceExercises_AcceptsTheTemplatesOwnExercises()
    {
        var template = new WorkoutTemplate(Guid.NewGuid(), UserA, "День А",
            [new() { ExerciseName = "Жим" }, new() { ExerciseName = "Тяга" }]);
        var originalIds = template.Exercises.Select(exercise => exercise.Id).ToArray();

        template.ReplaceExercises(template.Exercises);

        Assert.Equal(originalIds, template.Exercises.Select(exercise => exercise.Id));
    }

    [Fact]
    public async Task SaveTemplate_UpdatesEveryReadPathAndIsolatesUnsavedChanges()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "День А",
            [new() { ExerciseName = "Жим" }]);
        var updated = new WorkoutTemplate { Id = template.Id, ProgramId = program.Id, UserId = UserA, Name = "День Б" };
        updated.AddExercise(new TemplateExercise { ExerciseName = "Тяга" });
        await _repository.SaveTemplateAsync(updated);

        updated.Name = "Черновик";
        var byId = await _service.GetTemplateAsync(UserA, template.Id);
        var byProgram = await _service.GetProgramAsync(UserA, program.Id);
        Assert.NotNull(byId);
        Assert.NotNull(byProgram);
        var listedProgram = Assert.Single(await _repository.GetProgramsByUserIdAsync(UserA));
        var listedTemplate = Assert.Single(await _repository.GetTemplatesByProgramIdAsync(program.Id));
        Assert.All(new[] { byId, Assert.Single(byProgram.Templates), Assert.Single(listedProgram.Templates), listedTemplate }, item =>
        {
            Assert.Equal("День Б", item.Name);
            Assert.Equal(updated.Exercises[0].Id, Assert.Single(item.Exercises).Id);
        });

        byId.ReplaceExercises([]);
        var session = await _service.StartSessionAsync(UserA, template.Id);
        Assert.Equal("Тяга", Assert.Single(session.ExerciseSnapshots).ExerciseName);
    }

    [Fact]
    public async Task SR01_UserCannotAccessOrModifyAnotherUsersData()
    {
        var programA = await _service.CreateProgramAsync(UserA, "Программа А");
        var exercisesA = new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Жим", PlannedSets = 3, MinReps = 5, MaxReps = 5 }
        };
        var templateA = await _service.CreateTemplateAsync(UserA, programA.Id, "Шаблон А", exercisesA);
        var sessionA = await _service.StartSessionAsync(UserA, templateA.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateTemplateAsync(UserB, programA.Id, "Попытка шаблона B", new List<TemplateExercise>()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.StartSessionAsync(UserB, templateA.Id));

        var getProgram = await _service.GetProgramAsync(UserB, programA.Id);
        Assert.Null(getProgram);

        var getTemplate = await _service.GetTemplateAsync(UserB, templateA.Id);
        Assert.Null(getTemplate);

        var getSession = await _service.GetSessionAsync(UserB, sessionA.Id);
        Assert.Null(getSession);

        var snapshotId = sessionA.ExerciseSnapshots.First().Id;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.RecordSetAsync(UserB, sessionA.Id, snapshotId, 80.0, 10));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CompleteSessionAsync(UserB, sessionA.Id));
    }

    [Fact]
    public async Task SR01_SR02_OperatorInterruptSession_ReturnsTechnicalStatusOnly_AndLogsAction()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон", new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Тяга", PlannedSets = 1, MinReps = 5, MaxReps = 5 }
        });
        var session = await _service.StartSessionAsync(UserA, template.Id);
        var snapshotId = session.ExerciseSnapshots.First().Id;
        await _service.RecordSetAsync(UserA, session.Id, snapshotId, 120.0, 5);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _operatorService.GetTechnicalStatusAsync(NonOperatorId, session.Id));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _operatorService.InterruptSessionAsync(NonOperatorId, session.Id, "Служебная причина"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _operatorService.InterruptSessionAsync(OperatorId, session.Id, "   "));

        var statusResult = await _operatorService.InterruptSessionAsync(OperatorId, session.Id, "Технический сбой связи");
        Assert.Equal(session.Id, statusResult.SessionId);
        Assert.Equal(SessionStatus.Interrupted, statusResult.Status);
        Assert.NotNull(statusResult.EndedAt);

        var ownerSession = await _service.GetSessionAsync(UserA, session.Id);
        Assert.NotNull(ownerSession);
        Assert.Equal(SessionStatus.Interrupted, ownerSession.Status);
        Assert.Single(ownerSession.SetResults);

        var logs = await _repository.GetOperatorActionLogsBySessionIdAsync(session.Id);
        var log = Assert.Single(logs);
        Assert.Equal(OperatorId, log.OperatorId);
        Assert.Equal("Технический сбой связи", log.Reason);

        var techStatus = await _operatorService.GetTechnicalStatusAsync(OperatorId, session.Id);
        Assert.Equal(session.Id, techStatus.SessionId);
        Assert.Equal(SessionStatus.Interrupted, techStatus.Status);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _operatorService.InterruptSessionAsync(OperatorId, session.Id, "Повторное прерывание"));
    }

    [Fact]
    public async Task SR02_D02_FailureOnOperatorAuditLogSave_RollbacksSessionState()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон", new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Тяга", PlannedSets = 1, MinReps = 5, MaxReps = 5 }
        });
        var session = await _service.StartSessionAsync(UserA, template.Id);
        var snapshotId = session.ExerciseSnapshots.First().Id;
        await _service.RecordSetAsync(UserA, session.Id, snapshotId, 110.0, 5);

        _repository.FailOnOperatorLogSave = true;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _operatorService.InterruptSessionAsync(OperatorId, session.Id, "Служебное действие"));

        Assert.Contains("Сбой при сохранении записи действия оператора в журнал аудита", ex.Message);

        var sessionAfterFailedInterrupt = await _service.GetSessionAsync(UserA, session.Id);
        Assert.NotNull(sessionAfterFailedInterrupt);
        Assert.Equal(SessionStatus.Active, sessionAfterFailedInterrupt.Status);
        Assert.Null(sessionAfterFailedInterrupt.EndedAt);
        Assert.Single(sessionAfterFailedInterrupt.SetResults);

        var logs = await _repository.GetOperatorActionLogsBySessionIdAsync(session.Id);
        Assert.Empty(logs);
    }

    [Fact]
    public async Task SR03_CannotRecordSetInClosedOrInterruptedSession()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон", new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Присед", PlannedSets = 3, MinReps = 5, MaxReps = 5 }
        });

        var session1 = await _service.StartSessionAsync(UserA, template.Id);
        var snapshotId1 = session1.ExerciseSnapshots.First().Id;
        await _service.CompleteSessionAsync(UserA, session1.Id);

        var exCompleted = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecordSetAsync(UserA, session1.Id, snapshotId1, 100.0, 5));
        Assert.Contains("Нельзя добавлять результаты подходов в неактивную сессию", exCompleted.Message);

        var session2 = await _service.StartSessionAsync(UserA, template.Id);
        var snapshotId2 = session2.ExerciseSnapshots.First().Id;
        await _operatorService.InterruptSessionAsync(OperatorId, session2.Id, "Операторское прерывание");

        var exInterrupted = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecordSetAsync(UserA, session2.Id, snapshotId2, 100.0, 5));
        Assert.Contains("Нельзя добавлять результаты подходов в неактивную сессию", exInterrupted.Message);
    }

    [Fact]
    public async Task SR04_ModifyingTemplateDoesNotAffectActiveSessionSnapshot()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var exercises = new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Жим лежа", PlannedSets = 3, MinReps = 5, MaxReps = 8 }
        };
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон А", exercises);

        var session = await _service.StartSessionAsync(UserA, template.Id);
        Assert.Single(session.ExerciseSnapshots);
        Assert.Equal("Жим лежа", session.ExerciseSnapshots[0].ExerciseName);
        Assert.Equal(template.Exercises[0].Id, session.ExerciseSnapshots[0].ExerciseId);

        template.ReplaceExercises(new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Брусья", PlannedSets = 5, MinReps = 10, MaxReps = 12 }
        });
        await _repository.SaveTemplateAsync(template);

        var updatedTemplate = await _service.GetTemplateAsync(UserA, template.Id);
        Assert.NotNull(updatedTemplate);
        Assert.Equal("Брусья", updatedTemplate.Exercises[0].ExerciseName);

        var sessionAfter = await _service.GetSessionAsync(UserA, session.Id);
        Assert.NotNull(sessionAfter);
        Assert.Single(sessionAfter.ExerciseSnapshots);
        Assert.Equal("Жим лежа", sessionAfter.ExerciseSnapshots[0].ExerciseName);
        Assert.Equal(3, sessionAfter.ExerciseSnapshots[0].PlannedSets);
    }

    [Fact]
    public async Task D02_ConcurrentSessionStart_OnlyOneSucceeds()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон", new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Жим", PlannedSets = 3, MinReps = 5, MaxReps = 5 }
        });

        var task1 = Task.Run(() => _service.StartSessionAsync(UserA, template.Id));
        var task2 = Task.Run(() => _service.StartSessionAsync(UserA, template.Id));

        var results = await Task.WhenAll(
            task1.ContinueWith(t => t.IsCompletedSuccessfully),
            task2.ContinueWith(t => t.IsCompletedSuccessfully)
        );

        Assert.True(results.Count(r => r) == 1);
        Assert.True(results.Count(r => !r) == 1);
    }

    [Fact]
    public async Task D02_TransactionRollbackOnException_PreservesPreviousState()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон", new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Жим", PlannedSets = 3, MinReps = 5, MaxReps = 5 }
        });
        var session = await _service.StartSessionAsync(UserA, template.Id);

        await Assert.ThrowsAsync<ApplicationException>(() =>
            _repository.UpdateSessionAtomicAsync(session.Id, s =>
            {
                s.Complete();
                throw new ApplicationException("Имитация сбоя после изменения статуса");
            }));

        var sessionAfterFail = await _service.GetSessionAsync(UserA, session.Id);
        Assert.NotNull(sessionAfterFail);
        Assert.Equal(SessionStatus.Active, sessionAfterFail.Status);
        Assert.Null(sessionAfterFail.EndedAt);
    }

    [Fact]
    public async Task InputValidation_InvalidRepsOrWeightThrowsArgumentException()
    {
        var program = await _service.CreateProgramAsync(UserA, "Программа");
        var template = await _service.CreateTemplateAsync(UserA, program.Id, "Шаблон", new List<TemplateExercise>
        {
            new() { OrderIndex = 1, ExerciseName = "Жим", PlannedSets = 3, MinReps = 5, MaxReps = 5 }
        });
        var session = await _service.StartSessionAsync(UserA, template.Id);
        var snapshotId = session.ExerciseSnapshots.First().Id;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.RecordSetAsync(UserA, session.Id, snapshotId, 100.0, 0));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.RecordSetAsync(UserA, session.Id, snapshotId, 100.0, -5));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.RecordSetAsync(UserA, session.Id, snapshotId, -10.0, 5));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.RecordSetAsync(UserA, session.Id, snapshotId, double.NaN, 5));
    }
}
