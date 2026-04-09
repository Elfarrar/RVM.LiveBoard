namespace RVM.LiveBoard.Domain.Entities;

public class MetricDataPoint
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Unit { get; set; }
    public string? Source { get; set; }
    public string? Tags { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
