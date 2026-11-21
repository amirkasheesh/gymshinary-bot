using Gymshinary.Bot.Domain.Enums;

namespace Gymshinary.Bot.Domain.Models;

public class SessionTechnicalStatus
{
    public Guid SessionId { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
