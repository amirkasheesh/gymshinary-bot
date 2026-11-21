using Gymshinary.Bot.Domain.Models;

namespace Gymshinary.Bot.Services;

public interface IOperatorWorkoutService
{
    Task<SessionTechnicalStatus> GetTechnicalStatusAsync(long operatorId, Guid sessionId);
    Task<SessionTechnicalStatus> InterruptSessionAsync(long operatorId, Guid sessionId, string reason);
}
