namespace RVM.LiveBoard.API.Dtos;

public record CreateDashboardRequest(
    string Name,
    string? Description = null,
    bool IsDefault = false,
    int RefreshIntervalSeconds = 10);

public record UpdateDashboardRequest(
    string? Name,
    string? Description,
    bool? IsDefault,
    int? RefreshIntervalSeconds);

public record DashboardResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    int RefreshIntervalSeconds,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<PanelResponse>? Panels = null);
