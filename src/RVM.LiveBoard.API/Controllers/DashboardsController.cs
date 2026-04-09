using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardsController(
    IDashboardRepository dashboardRepo,
    IDashboardPanelRepository panelRepo,
    ILogger<DashboardsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DashboardResponse>>> GetAll(CancellationToken ct)
    {
        var dashboards = await dashboardRepo.GetAllAsync(ct);
        return dashboards.Select(d => MapDashboard(d)).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DashboardResponse>> GetById(Guid id, CancellationToken ct)
    {
        var dashboard = await dashboardRepo.GetByIdWithPanelsAsync(id, ct);
        if (dashboard is null) return NotFound();
        return MapDashboard(dashboard, includePanels: true);
    }

    [HttpPost]
    public async Task<ActionResult<DashboardResponse>> Create(CreateDashboardRequest request, CancellationToken ct)
    {
        var dashboard = new Dashboard
        {
            Name = request.Name,
            Description = request.Description,
            IsDefault = request.IsDefault,
            RefreshIntervalSeconds = request.RefreshIntervalSeconds,
        };

        await dashboardRepo.AddAsync(dashboard, ct);
        logger.LogInformation("Created dashboard '{Name}'", dashboard.Name);
        return CreatedAtAction(nameof(GetById), new { id = dashboard.Id }, MapDashboard(dashboard));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DashboardResponse>> Update(Guid id, UpdateDashboardRequest request, CancellationToken ct)
    {
        var dashboard = await dashboardRepo.GetByIdAsync(id, ct);
        if (dashboard is null) return NotFound();

        if (request.Name is not null) dashboard.Name = request.Name;
        if (request.Description is not null) dashboard.Description = request.Description;
        if (request.IsDefault.HasValue) dashboard.IsDefault = request.IsDefault.Value;
        if (request.RefreshIntervalSeconds.HasValue) dashboard.RefreshIntervalSeconds = request.RefreshIntervalSeconds.Value;

        await dashboardRepo.UpdateAsync(dashboard, ct);
        return MapDashboard(dashboard);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var dashboard = await dashboardRepo.GetByIdAsync(id, ct);
        if (dashboard is null) return NotFound();

        await dashboardRepo.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{dashboardId:guid}/panels")]
    public async Task<ActionResult<PanelResponse>> AddPanel(Guid dashboardId, CreatePanelRequest request, CancellationToken ct)
    {
        var dashboard = await dashboardRepo.GetByIdAsync(dashboardId, ct);
        if (dashboard is null) return NotFound();

        var panel = new DashboardPanel
        {
            DashboardId = dashboardId,
            Title = request.Title,
            PanelType = Enum.TryParse<PanelType>(request.PanelType, true, out var pt) ? pt : PanelType.LineChart,
            MetricName = request.MetricName,
            Aggregation = Enum.TryParse<MetricAggregation>(request.Aggregation, true, out var agg) ? agg : MetricAggregation.Last,
            TimeWindowMinutes = request.TimeWindowMinutes,
            SortOrder = request.SortOrder,
            GridColumn = request.GridColumn,
            GridRow = request.GridRow,
            GridWidth = request.GridWidth,
            GridHeight = request.GridHeight,
        };

        await panelRepo.AddAsync(panel, ct);
        return CreatedAtAction(nameof(GetById), new { id = dashboardId }, MapPanel(panel));
    }

    [HttpPut("panels/{panelId:guid}")]
    public async Task<ActionResult<PanelResponse>> UpdatePanel(Guid panelId, UpdatePanelRequest request, CancellationToken ct)
    {
        var panel = await panelRepo.GetByIdAsync(panelId, ct);
        if (panel is null) return NotFound();

        if (request.Title is not null) panel.Title = request.Title;
        if (request.PanelType is not null && Enum.TryParse<PanelType>(request.PanelType, true, out var pt)) panel.PanelType = pt;
        if (request.MetricName is not null) panel.MetricName = request.MetricName;
        if (request.Aggregation is not null && Enum.TryParse<MetricAggregation>(request.Aggregation, true, out var agg)) panel.Aggregation = agg;
        if (request.TimeWindowMinutes.HasValue) panel.TimeWindowMinutes = request.TimeWindowMinutes;
        if (request.SortOrder.HasValue) panel.SortOrder = request.SortOrder.Value;
        if (request.GridColumn.HasValue) panel.GridColumn = request.GridColumn.Value;
        if (request.GridRow.HasValue) panel.GridRow = request.GridRow.Value;
        if (request.GridWidth.HasValue) panel.GridWidth = request.GridWidth.Value;
        if (request.GridHeight.HasValue) panel.GridHeight = request.GridHeight.Value;

        await panelRepo.UpdateAsync(panel, ct);
        return MapPanel(panel);
    }

    [HttpDelete("panels/{panelId:guid}")]
    public async Task<IActionResult> DeletePanel(Guid panelId, CancellationToken ct)
    {
        var panel = await panelRepo.GetByIdAsync(panelId, ct);
        if (panel is null) return NotFound();

        await panelRepo.DeleteAsync(panelId, ct);
        return NoContent();
    }

    private static DashboardResponse MapDashboard(Dashboard d, bool includePanels = false)
    {
        var panels = includePanels && d.Panels.Count > 0
            ? d.Panels.Select(MapPanel).ToList()
            : null;

        return new DashboardResponse(d.Id, d.Name, d.Description, d.IsDefault,
            d.RefreshIntervalSeconds, d.CreatedAt, d.UpdatedAt, panels);
    }

    private static PanelResponse MapPanel(DashboardPanel p) => new(
        p.Id, p.DashboardId, p.Title, p.PanelType.ToString(),
        p.MetricName, p.Aggregation.ToString(), p.TimeWindowMinutes,
        p.SortOrder, p.GridColumn, p.GridRow, p.GridWidth, p.GridHeight);
}
