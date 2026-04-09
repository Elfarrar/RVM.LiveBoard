using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.API.Hubs;
using RVM.LiveBoard.API.Services;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.Test.Services;

public class MetricIngestionServiceTests
{
    private readonly Mock<IMetricDataPointRepository> _metricRepo = new();
    private readonly Mock<IHubContext<LiveMetricHub>> _hubContext = new();
    private readonly Mock<ILogger<MetricIngestionService>> _logger = new();
    private readonly MetricIngestionService _service;

    public MetricIngestionServiceTests()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockGroupProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroupProxy.Object);
        _hubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _service = new MetricIngestionService(_metricRepo.Object, _hubContext.Object, _logger.Object);
    }

    [Fact]
    public async Task IngestAsync_PersistsAllMetrics()
    {
        var batch = new List<IngestMetricRequest>
        {
            new("cpu.usage", 45.2, "percent", "server-1"),
            new("memory.usage", 72.0, "percent", "server-2"),
        };

        var result = await _service.IngestAsync(batch);

        Assert.Equal(2, result);
        _metricRepo.Verify(r => r.AddBatchAsync(
            It.Is<List<MetricDataPoint>>(l => l.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_UsesProvidedTimestamp()
    {
        var ts = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var batch = new List<IngestMetricRequest>
        {
            new("cpu.usage", 50.0, Timestamp: ts),
        };

        await _service.IngestAsync(batch);

        _metricRepo.Verify(r => r.AddBatchAsync(
            It.Is<List<MetricDataPoint>>(l => l[0].Timestamp == ts),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_DefaultsTimestampWhenNull()
    {
        var before = DateTime.UtcNow;
        var batch = new List<IngestMetricRequest>
        {
            new("cpu.usage", 50.0),
        };

        await _service.IngestAsync(batch);

        _metricRepo.Verify(r => r.AddBatchAsync(
            It.Is<List<MetricDataPoint>>(l => l[0].Timestamp >= before),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_PushesToSignalR()
    {
        var mockClientProxy = new Mock<IClientProxy>();
        var mockGroupProxy = new Mock<IClientProxy>();
        var mockClients = new Mock<IHubClients>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroupProxy.Object);
        _hubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var batch = new List<IngestMetricRequest>
        {
            new("cpu.usage", 50.0, Source: "srv"),
        };

        await _service.IngestAsync(batch);

        mockClientProxy.Verify(c => c.SendCoreAsync("MetricReceived",
            It.Is<object?[]>(args => args.Length == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_SetsMetricProperties()
    {
        var batch = new List<IngestMetricRequest>
        {
            new("disk.io", 1024.5, "MB/s", "nas-1", """{"pool":"fast"}"""),
        };

        await _service.IngestAsync(batch);

        _metricRepo.Verify(r => r.AddBatchAsync(
            It.Is<List<MetricDataPoint>>(l =>
                l[0].MetricName == "disk.io" &&
                l[0].Value == 1024.5 &&
                l[0].Unit == "MB/s" &&
                l[0].Source == "nas-1" &&
                l[0].Tags!.Contains("pool")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
