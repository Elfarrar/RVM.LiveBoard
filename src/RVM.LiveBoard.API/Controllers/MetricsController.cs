using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.API.Services;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetricsController(
    MetricIngestionService ingestionService,
    IMetricDataPointRepository metricRepo,
    IDashboardPanelRepository panelRepo) : ControllerBase
{
    [HttpPost("ingest")]
    public async Task<ActionResult<IngestMetricBatchResponse>> Ingest(IngestMetricBatchRequest request, CancellationToken ct)
    {
        if (request.Metrics is null || request.Metrics.Count == 0)
            return BadRequest(new { error = "Metrics list cannot be empty." });

        var accepted = await ingestionService.IngestAsync(request.Metrics, ct);
        return Ok(new IngestMetricBatchResponse(accepted));
    }

    [HttpGet("{metricName}")]
    public async Task<ActionResult<MetricQueryResponse>> Query(
        string metricName,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? source,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var toDate = to ?? DateTime.UtcNow;
        var fromDate = from ?? toDate.AddHours(-1);

        var points = await metricRepo.GetByMetricAsync(metricName, fromDate, toDate, source, limit, ct);

        var items = points.Select(p => new MetricDataPointResponse(
            p.Id, p.MetricName, p.Value, p.Unit, p.Source, p.Tags, p.Timestamp)).ToList();

        return new MetricQueryResponse(metricName, items, items.Count);
    }

    [HttpGet("names")]
    public async Task<ActionResult<MetricNamesResponse>> GetNames(CancellationToken ct)
    {
        var names = await metricRepo.GetDistinctMetricNamesAsync(ct);
        return new MetricNamesResponse(names);
    }

    [HttpGet("panel/{panelId:guid}/data")]
    public async Task<ActionResult<PanelDataResponse>> GetPanelData(Guid panelId, CancellationToken ct)
    {
        var panel = await panelRepo.GetByIdAsync(panelId, ct);
        if (panel is null) return NotFound();

        var to = DateTime.UtcNow;
        var from = panel.TimeWindowMinutes.HasValue
            ? to.AddMinutes(-panel.TimeWindowMinutes.Value)
            : to.AddHours(-1);

        var aggregatedValue = await metricRepo.GetAggregatedValueAsync(
            panel.MetricName, panel.Aggregation.ToString(), from, to, ct);

        var points = await metricRepo.GetByMetricAsync(panel.MetricName, from, to, null, 200, ct);
        var items = points.Select(p => new MetricDataPointResponse(
            p.Id, p.MetricName, p.Value, p.Unit, p.Source, p.Tags, p.Timestamp)).ToList();

        return new PanelDataResponse(panelId, panel.MetricName, panel.Aggregation.ToString(),
            aggregatedValue, items);
    }
}
