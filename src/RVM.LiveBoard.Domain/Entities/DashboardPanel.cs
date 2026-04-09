using RVM.LiveBoard.Domain.Enums;

namespace RVM.LiveBoard.Domain.Entities;

public class DashboardPanel
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DashboardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public PanelType PanelType { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public MetricAggregation Aggregation { get; set; } = MetricAggregation.Last;
    public int? TimeWindowMinutes { get; set; }
    public int SortOrder { get; set; }
    public int GridColumn { get; set; }
    public int GridRow { get; set; }
    public int GridWidth { get; set; } = 6;
    public int GridHeight { get; set; } = 4;

    public Dashboard Dashboard { get; set; } = null!;
}
