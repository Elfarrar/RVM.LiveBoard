using Microsoft.AspNetCore.SignalR;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.API.Hubs;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.API.Services;

public class MetricIngestionService(
    IMetricDataPointRepository metricRepo,
    IHubContext<LiveMetricHub> hubContext,
    ILogger<MetricIngestionService> logger)
{
    public async Task<int> IngestAsync(List<IngestMetricRequest> batch, CancellationToken ct = default)
    {
        var points = batch.Select(m => new MetricDataPoint
        {
            MetricName = m.MetricName,
            Value = m.Value,
            Unit = m.Unit,
            Source = m.Source,
            Tags = m.Tags,
            Timestamp = m.Timestamp ?? DateTime.UtcNow,
        }).ToList();

        await metricRepo.AddBatchAsync(points, ct);

        foreach (var point in points)
        {
            var dto = new MetricDataPointResponse(
                point.Id, point.MetricName, point.Value, point.Unit,
                point.Source, point.Tags, point.Timestamp);

            await hubContext.Clients.Group($"metric-{point.MetricName}")
                .SendAsync("MetricReceived", dto, ct);
            await hubContext.Clients.All.SendAsync("MetricReceived", dto, ct);
        }

        logger.LogInformation("Ingested {Count} metric data points", points.Count);
        return points.Count;
    }
}
