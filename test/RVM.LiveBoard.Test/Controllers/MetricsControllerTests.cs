using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RVM.LiveBoard.API.Controllers;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.API.Hubs;
using RVM.LiveBoard.API.Services;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.Test.Controllers;

public class MetricsControllerTests
{
    private readonly Mock<IMetricDataPointRepository> _metricRepoMock = new();
    private readonly Mock<IDashboardPanelRepository> _panelRepoMock = new();
    private readonly MetricIngestionService _ingestionService;

    public MetricsControllerTests()
    {
        var hubCtxMock = new Mock<IHubContext<LiveMetricHub>>();
        var clientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);
        hubCtxMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _ingestionService = new MetricIngestionService(
            _metricRepoMock.Object,
            hubCtxMock.Object,
            new Mock<ILogger<MetricIngestionService>>().Object);
    }

    private MetricsController CreateController() =>
        new(_ingestionService, _metricRepoMock.Object, _panelRepoMock.Object);

    // -------------------------------------------------------------------------
    // Ingest
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Ingest_EmptyMetrics_ReturnsBadRequest()
    {
        var request = new IngestMetricBatchRequest([]);
        var result = await CreateController().Ingest(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Ingest_NullMetrics_ReturnsBadRequest()
    {
        var request = new IngestMetricBatchRequest(null!);
        var result = await CreateController().Ingest(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Ingest_ValidBatch_ReturnsAcceptedCount()
    {
        var request = new IngestMetricBatchRequest(
        [
            new IngestMetricRequest("cpu.usage", 45.2),
            new IngestMetricRequest("mem.usage", 72.0),
        ]);

        var result = await CreateController().Ingest(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<IngestMetricBatchResponse>(ok.Value);
        Assert.Equal(2, response.Accepted);
    }

    [Fact]
    public async Task Ingest_SingleMetric_PersistsToRepo()
    {
        var request = new IngestMetricBatchRequest([new IngestMetricRequest("cpu.usage", 55.0)]);

        await CreateController().Ingest(request, CancellationToken.None);

        _metricRepoMock.Verify(r => r.AddBatchAsync(
            It.Is<List<MetricDataPoint>>(l => l.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Query
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Query_ReturnsDataPoints()
    {
        var points = new List<MetricDataPoint>
        {
            new() { MetricName = "cpu.usage", Value = 45.0, Unit = "percent", Source = "srv1", Timestamp = DateTime.UtcNow },
            new() { MetricName = "cpu.usage", Value = 50.0, Unit = "percent", Source = "srv2", Timestamp = DateTime.UtcNow.AddMinutes(-5) },
        };
        _metricRepoMock.Setup(r => r.GetByMetricAsync("cpu.usage", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var result = await CreateController().Query("cpu.usage", null, null, null, 100, CancellationToken.None);

        var response = Assert.IsType<MetricQueryResponse>(result.Value);
        Assert.Equal("cpu.usage", response.MetricName);
        Assert.Equal(2, response.DataPoints.Count);
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task Query_WithSource_PassesSourceToRepo()
    {
        _metricRepoMock.Setup(r => r.GetByMetricAsync("cpu.usage", It.IsAny<DateTime>(), It.IsAny<DateTime>(), "srv1", 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await CreateController().Query("cpu.usage", null, null, "srv1", 100, CancellationToken.None);

        _metricRepoMock.Verify(r => r.GetByMetricAsync("cpu.usage",
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), "srv1", 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Query_WithDateRange_UsesProvidedDates()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        _metricRepoMock.Setup(r => r.GetByMetricAsync("cpu.usage", from, to, null, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await CreateController().Query("cpu.usage", from, to, null, 100, CancellationToken.None);

        _metricRepoMock.Verify(r => r.GetByMetricAsync("cpu.usage", from, to, null, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Query_Empty_ReturnsEmptyList()
    {
        _metricRepoMock.Setup(r => r.GetByMetricAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateController().Query("nonexistent", null, null, null, 100, CancellationToken.None);

        Assert.Equal(0, result.Value!.TotalCount);
        Assert.Empty(result.Value!.DataPoints);
    }

    // -------------------------------------------------------------------------
    // GetNames
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetNames_ReturnsDistinctMetricNames()
    {
        _metricRepoMock.Setup(r => r.GetDistinctMetricNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["cpu.usage", "mem.usage", "disk.io"]);

        var result = await CreateController().GetNames(CancellationToken.None);

        Assert.Equal(3, result.Value!.Names.Count);
        Assert.Contains("cpu.usage", result.Value!.Names);
    }

    [Fact]
    public async Task GetNames_Empty_ReturnsEmptyList()
    {
        _metricRepoMock.Setup(r => r.GetDistinctMetricNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateController().GetNames(CancellationToken.None);

        Assert.Empty(result.Value!.Names);
    }

    // -------------------------------------------------------------------------
    // GetPanelData
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPanelData_PanelNotFound_ReturnsNotFound()
    {
        _panelRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardPanel?)null);

        var result = await CreateController().GetPanelData(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPanelData_PanelFound_ReturnsAggregatedData()
    {
        var panel = new DashboardPanel
        {
            MetricName = "cpu.usage",
            Aggregation = MetricAggregation.Average,
            TimeWindowMinutes = 60,
        };
        _panelRepoMock.Setup(r => r.GetByIdAsync(panel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(panel);

        _metricRepoMock.Setup(r => r.GetAggregatedValueAsync("cpu.usage", "Average",
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(67.5);

        _metricRepoMock.Setup(r => r.GetByMetricAsync("cpu.usage",
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MetricDataPoint { MetricName = "cpu.usage", Value = 67.5 }]);

        var result = await CreateController().GetPanelData(panel.Id, CancellationToken.None);

        var response = Assert.IsType<PanelDataResponse>(result.Value);
        Assert.Equal("cpu.usage", response.MetricName);
        Assert.Equal(67.5, response.AggregatedValue);
        Assert.Single(response.DataPoints);
    }

    [Fact]
    public async Task GetPanelData_NoTimeWindow_DefaultsToOneHour()
    {
        var panel = new DashboardPanel
        {
            MetricName = "mem.usage",
            Aggregation = MetricAggregation.Last,
            TimeWindowMinutes = null,
        };
        _panelRepoMock.Setup(r => r.GetByIdAsync(panel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(panel);

        _metricRepoMock.Setup(r => r.GetAggregatedValueAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((double?)null);

        _metricRepoMock.Setup(r => r.GetByMetricAsync(It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var beforeCall = DateTime.UtcNow;
        await CreateController().GetPanelData(panel.Id, CancellationToken.None);

        _metricRepoMock.Verify(r => r.GetByMetricAsync("mem.usage",
            It.Is<DateTime>(d => d <= beforeCall.AddHours(-1).AddSeconds(1)),
            It.IsAny<DateTime>(), null, 200, It.IsAny<CancellationToken>()), Times.Once);
    }
}
