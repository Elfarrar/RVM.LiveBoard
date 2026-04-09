using RVM.LiveBoard.Domain.Enums;

namespace RVM.LiveBoard.Domain.Entities;

public class Alert
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AlertRuleId { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Active;
    public double TriggerValue { get; set; }
    public string? Message { get; set; }
    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public AlertRule Rule { get; set; } = null!;
}
