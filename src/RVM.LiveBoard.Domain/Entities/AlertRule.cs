using RVM.LiveBoard.Domain.Enums;

namespace RVM.LiveBoard.Domain.Entities;

public class AlertRule
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Condition { get; set; } = "gt";
    public double Threshold { get; set; }
    public int EvaluationWindowMinutes { get; set; } = 5;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Alert> Alerts { get; set; } = [];
}
