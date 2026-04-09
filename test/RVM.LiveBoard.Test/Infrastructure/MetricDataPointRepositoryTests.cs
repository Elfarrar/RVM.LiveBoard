using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Infrastructure.Data;
using RVM.LiveBoard.Infrastructure.Repositories;

namespace RVM.LiveBoard.Test.Infrastructure;

public class MetricDataPointRepositoryTests : IDisposable
{
    private readonly LiveBoardDbContext _db;
    private readonly MetricDataPointRepository _repo;

    public MetricDataPointRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<LiveBoardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new LiveBoardDbContext(options);
        _repo = new MetricDataPointRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddBatchAsync_PersistsPoints()
    {
        var points = new List<MetricDataPoint>
        {
            new() { MetricName = "cpu.usage", Value = 45.2, Source = "server-1" },
            new() { MetricName = "cpu.usage", Value = 52.1, Source = "server-2" },
        };

        await _repo.AddBatchAsync(points);

        Assert.Equal(2, await _db.MetricDataPoints.CountAsync());
    }

    [Fact]
    public async Task GetByMetricAsync_FiltersCorrectly()
    {
        await SeedMetrics();
        var from = DateTime.UtcNow.AddHours(-2);
        var to = DateTime.UtcNow.AddHours(1);

        var results = await _repo.GetByMetricAsync("cpu.usage", from, to, null, 100);

        Assert.All(results, p => Assert.Equal("cpu.usage", p.MetricName));
    }

    [Fact]
    public async Task GetByMetricAsync_FiltersBySource()
    {
        await SeedMetrics();
        var from = DateTime.UtcNow.AddHours(-2);
        var to = DateTime.UtcNow.AddHours(1);

        var results = await _repo.GetByMetricAsync("cpu.usage", from, to, "server-1", 100);

        Assert.All(results, p => Assert.Equal("server-1", p.Source));
    }

    [Fact]
    public async Task GetByMetricAsync_RespectsLimit()
    {
        await SeedMetrics();
        var from = DateTime.UtcNow.AddHours(-2);
        var to = DateTime.UtcNow.AddHours(1);

        var results = await _repo.GetByMetricAsync("cpu.usage", from, to, null, 1);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetDistinctMetricNamesAsync_ReturnsUniqueNames()
    {
        await SeedMetrics();

        var names = await _repo.GetDistinctMetricNamesAsync();

        Assert.Equal(2, names.Count);
        Assert.Contains("cpu.usage", names);
        Assert.Contains("memory.usage", names);
    }

    [Theory]
    [InlineData("avg", 55.0)]
    [InlineData("sum", 165.0)]
    [InlineData("min", 40.0)]
    [InlineData("max", 70.0)]
    [InlineData("count", 3.0)]
    public async Task GetAggregatedValueAsync_CalculatesCorrectly(string aggregation, double expected)
    {
        _db.MetricDataPoints.AddRange(
            new MetricDataPoint { MetricName = "test", Value = 40.0 },
            new MetricDataPoint { MetricName = "test", Value = 55.0 },
            new MetricDataPoint { MetricName = "test", Value = 70.0 });
        await _db.SaveChangesAsync();

        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow.AddHours(1);

        var result = await _repo.GetAggregatedValueAsync("test", aggregation, from, to);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value, 1);
    }

    [Fact]
    public async Task GetAggregatedValueAsync_Last_ReturnsNewest()
    {
        _db.MetricDataPoints.Add(new MetricDataPoint
        {
            MetricName = "test", Value = 10.0,
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
        });
        _db.MetricDataPoints.Add(new MetricDataPoint
        {
            MetricName = "test", Value = 99.0,
            Timestamp = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow.AddHours(1);

        var result = await _repo.GetAggregatedValueAsync("test", "last", from, to);

        Assert.Equal(99.0, result);
    }

    [Fact]
    public async Task GetAggregatedValueAsync_ReturnsNullWhenNoData()
    {
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow.AddHours(1);

        var result = await _repo.GetAggregatedValueAsync("nonexistent", "avg", from, to);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOldPoints()
    {
        _db.MetricDataPoints.Add(new MetricDataPoint
        {
            MetricName = "old", Value = 1, Timestamp = DateTime.UtcNow.AddDays(-30),
        });
        _db.MetricDataPoints.Add(new MetricDataPoint
        {
            MetricName = "recent", Value = 2, Timestamp = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();

        var deleted = await _repo.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-7));

        Assert.Equal(1, deleted);
        Assert.Equal(1, await _db.MetricDataPoints.CountAsync());
    }

    private async Task SeedMetrics()
    {
        _db.MetricDataPoints.AddRange(
            new MetricDataPoint { MetricName = "cpu.usage", Value = 45.2, Source = "server-1" },
            new MetricDataPoint { MetricName = "cpu.usage", Value = 52.1, Source = "server-2" },
            new MetricDataPoint { MetricName = "cpu.usage", Value = 60.0, Source = "server-1" },
            new MetricDataPoint { MetricName = "memory.usage", Value = 72.0, Source = "server-1" });
        await _db.SaveChangesAsync();
    }
}
