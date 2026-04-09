using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Domain.Interfaces;

public interface IMetricDataPointRepository
{
    Task AddBatchAsync(List<MetricDataPoint> points, CancellationToken ct = default);
    Task<List<MetricDataPoint>> GetByMetricAsync(string metricName, DateTime from, DateTime to,
        string? source, int limit, CancellationToken ct = default);
    Task<List<string>> GetDistinctMetricNamesAsync(CancellationToken ct = default);
    Task<double?> GetAggregatedValueAsync(string metricName, string aggregation,
        DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
