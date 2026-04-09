using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Interfaces;
using RVM.LiveBoard.Infrastructure.Data;

namespace RVM.LiveBoard.Infrastructure.Repositories;

public class MetricDataPointRepository(LiveBoardDbContext db) : IMetricDataPointRepository
{
    public async Task AddBatchAsync(List<MetricDataPoint> points, CancellationToken ct = default)
    {
        db.MetricDataPoints.AddRange(points);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<MetricDataPoint>> GetByMetricAsync(string metricName, DateTime from, DateTime to,
        string? source, int limit, CancellationToken ct = default)
    {
        var q = db.MetricDataPoints
            .Where(p => p.MetricName == metricName && p.Timestamp >= from && p.Timestamp <= to);

        if (!string.IsNullOrEmpty(source))
            q = q.Where(p => p.Source == source);

        return await q.OrderByDescending(p => p.Timestamp)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetDistinctMetricNamesAsync(CancellationToken ct = default)
    {
        return await db.MetricDataPoints
            .Select(p => p.MetricName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);
    }

    public async Task<double?> GetAggregatedValueAsync(string metricName, string aggregation,
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var points = await db.MetricDataPoints
            .Where(p => p.MetricName == metricName && p.Timestamp >= from && p.Timestamp <= to)
            .ToListAsync(ct);

        if (points.Count == 0) return null;

        return aggregation.ToLowerInvariant() switch
        {
            "avg" or "average" => points.Average(p => p.Value),
            "sum" => points.Sum(p => p.Value),
            "min" => points.Min(p => p.Value),
            "max" => points.Max(p => p.Value),
            "count" => points.Count,
            _ => points.OrderByDescending(p => p.Timestamp).First().Value, // "last"
        };
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        var old = await db.MetricDataPoints.Where(p => p.Timestamp < cutoff).ToListAsync(ct);
        db.MetricDataPoints.RemoveRange(old);
        await db.SaveChangesAsync(ct);
        return old.Count;
    }
}
