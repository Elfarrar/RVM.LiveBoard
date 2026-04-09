namespace RVM.LiveBoard.API.Dtos;

public record CreatePanelRequest(
    string Title,
    string PanelType,
    string MetricName,
    string Aggregation = "Last",
    int? TimeWindowMinutes = null,
    int SortOrder = 0,
    int GridColumn = 0,
    int GridRow = 0,
    int GridWidth = 6,
    int GridHeight = 4);

public record UpdatePanelRequest(
    string? Title,
    string? PanelType,
    string? MetricName,
    string? Aggregation,
    int? TimeWindowMinutes,
    int? SortOrder,
    int? GridColumn,
    int? GridRow,
    int? GridWidth,
    int? GridHeight);

public record PanelResponse(
    Guid Id,
    Guid DashboardId,
    string Title,
    string PanelType,
    string MetricName,
    string Aggregation,
    int? TimeWindowMinutes,
    int SortOrder,
    int GridColumn,
    int GridRow,
    int GridWidth,
    int GridHeight);
