namespace RVM.LiveBoard.Domain.Entities;

public class Dashboard
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public int RefreshIntervalSeconds { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DashboardPanel> Panels { get; set; } = [];
}
