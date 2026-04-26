using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RVM.LiveBoard.API.Controllers;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.Test.Controllers;

public class DashboardsControllerTests
{
    private readonly Mock<IDashboardRepository> _dashRepoMock = new();
    private readonly Mock<IDashboardPanelRepository> _panelRepoMock = new();
    private readonly Mock<ILogger<DashboardsController>> _loggerMock = new();

    private DashboardsController CreateController() =>
        new(_dashRepoMock.Object, _panelRepoMock.Object, _loggerMock.Object);

    private static Dashboard CreateDashboard(string name = "My Board", bool isDefault = false) => new()
    {
        Name = name,
        Description = "Test dashboard",
        IsDefault = isDefault,
        RefreshIntervalSeconds = 10,
    };

    private static DashboardPanel CreatePanel(Guid dashboardId, string title = "CPU Panel") => new()
    {
        DashboardId = dashboardId,
        Title = title,
        PanelType = PanelType.LineChart,
        MetricName = "cpu.usage",
        Aggregation = MetricAggregation.Average,
        TimeWindowMinutes = 60,
        SortOrder = 0,
        GridColumn = 0,
        GridRow = 0,
        GridWidth = 6,
        GridHeight = 4,
    };

    // -------------------------------------------------------------------------
    // GetAll
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsDashboards()
    {
        _dashRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateDashboard("A"), CreateDashboard("B")]);

        var result = await CreateController().GetAll(CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        _dashRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateController().GetAll(CancellationToken.None);

        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAll_MapsFieldsCorrectly()
    {
        var db = CreateDashboard("Main Board", true);
        _dashRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([db]);

        var result = await CreateController().GetAll(CancellationToken.None);

        var dto = Assert.Single(result.Value!);
        Assert.Equal("Main Board", dto.Name);
        Assert.True(dto.IsDefault);
        Assert.Null(dto.Panels); // GetAll nao inclui paineis
    }

    // -------------------------------------------------------------------------
    // GetById
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        _dashRepoMock.Setup(r => r.GetByIdWithPanelsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard?)null);

        var result = await CreateController().GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDashboardWithPanels()
    {
        var db = CreateDashboard("My Board");
        db.Panels.Add(CreatePanel(db.Id, "CPU"));
        db.Panels.Add(CreatePanel(db.Id, "Mem"));

        _dashRepoMock.Setup(r => r.GetByIdWithPanelsAsync(db.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        var result = await CreateController().GetById(db.Id, CancellationToken.None);

        var dto = Assert.IsType<DashboardResponse>(result.Value);
        Assert.Equal("My Board", dto.Name);
        Assert.Equal(2, dto.Panels!.Count);
    }

    [Fact]
    public async Task GetById_NoPanels_ReturnsNullPanels()
    {
        var db = CreateDashboard();
        _dashRepoMock.Setup(r => r.GetByIdWithPanelsAsync(db.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        var result = await CreateController().GetById(db.Id, CancellationToken.None);

        var dto = Assert.IsType<DashboardResponse>(result.Value);
        Assert.Null(dto.Panels);
    }

    // -------------------------------------------------------------------------
    // Create
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAt()
    {
        var request = new CreateDashboardRequest("New Board", "Desc", false, 30);

        var result = await CreateController().Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<DashboardResponse>(created.Value);
        Assert.Equal("New Board", dto.Name);
        Assert.Equal("Desc", dto.Description);
        Assert.Equal(30, dto.RefreshIntervalSeconds);
        _dashRepoMock.Verify(r => r.AddAsync(It.IsAny<Dashboard>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Update
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        _dashRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard?)null);

        var result = await CreateController().Update(Guid.NewGuid(),
            new UpdateDashboardRequest(null, null, null, null), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_Found_UpdatesFieldsAndReturns()
    {
        var db = CreateDashboard("Old");
        _dashRepoMock.Setup(r => r.GetByIdAsync(db.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        var result = await CreateController().Update(db.Id,
            new UpdateDashboardRequest("New Name", "New Desc", true, 60),
            CancellationToken.None);

        var dto = Assert.IsType<DashboardResponse>(result.Value);
        Assert.Equal("New Name", dto.Name);
        Assert.Equal("New Desc", dto.Description);
        Assert.True(dto.IsDefault);
        Assert.Equal(60, dto.RefreshIntervalSeconds);
        _dashRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Dashboard>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_NullFields_PreservesValues()
    {
        var db = CreateDashboard("Keep Name");
        _dashRepoMock.Setup(r => r.GetByIdAsync(db.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        var result = await CreateController().Update(db.Id,
            new UpdateDashboardRequest(null, null, null, null), CancellationToken.None);

        var dto = Assert.IsType<DashboardResponse>(result.Value);
        Assert.Equal("Keep Name", dto.Name);
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        _dashRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard?)null);

        var result = await CreateController().Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Found_ReturnsNoContent()
    {
        var db = CreateDashboard();
        _dashRepoMock.Setup(r => r.GetByIdAsync(db.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        var result = await CreateController().Delete(db.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _dashRepoMock.Verify(r => r.DeleteAsync(db.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // AddPanel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddPanel_DashboardNotFound_ReturnsNotFound()
    {
        _dashRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard?)null);

        var request = new CreatePanelRequest("CPU", "LineChart", "cpu.usage");
        var result = await CreateController().AddPanel(Guid.NewGuid(), request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddPanel_ValidRequest_ReturnsCreatedAt()
    {
        var db = CreateDashboard();
        _dashRepoMock.Setup(r => r.GetByIdAsync(db.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        var request = new CreatePanelRequest("CPU Panel", "LineChart", "cpu.usage", "Avg", 60);
        var result = await CreateController().AddPanel(db.Id, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<PanelResponse>(created.Value);
        Assert.Equal("CPU Panel", dto.Title);
        Assert.Equal("cpu.usage", dto.MetricName);
        _panelRepoMock.Verify(r => r.AddAsync(It.IsAny<DashboardPanel>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddPanel_InvalidPanelType_FallsBackToLineChart()
    {
        var db = CreateDashboard();
        _dashRepoMock.Setup(r => r.GetByIdAsync(db.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        var request = new CreatePanelRequest("CPU", "NotAType", "cpu.usage");
        var result = await CreateController().AddPanel(db.Id, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<PanelResponse>(created.Value);
        Assert.Equal("LineChart", dto.PanelType);
    }

    // -------------------------------------------------------------------------
    // UpdatePanel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdatePanel_NotFound_ReturnsNotFound()
    {
        _panelRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardPanel?)null);

        var result = await CreateController().UpdatePanel(Guid.NewGuid(),
            new UpdatePanelRequest(null, null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePanel_Found_UpdatesFields()
    {
        var db = CreateDashboard();
        var panel = CreatePanel(db.Id, "Old Title");
        _panelRepoMock.Setup(r => r.GetByIdAsync(panel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(panel);

        var result = await CreateController().UpdatePanel(panel.Id,
            new UpdatePanelRequest("New Title", "Gauge", "mem.usage", "Max", 30, 1, 2, 3, 4, 5),
            CancellationToken.None);

        var dto = Assert.IsType<PanelResponse>(result.Value);
        Assert.Equal("New Title", dto.Title);
        Assert.Equal("Gauge", dto.PanelType);
        Assert.Equal("mem.usage", dto.MetricName);
        Assert.Equal("Max", dto.Aggregation);
        _panelRepoMock.Verify(r => r.UpdateAsync(It.IsAny<DashboardPanel>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeletePanel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeletePanel_NotFound_ReturnsNotFound()
    {
        _panelRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardPanel?)null);

        var result = await CreateController().DeletePanel(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeletePanel_Found_ReturnsNoContent()
    {
        var db = CreateDashboard();
        var panel = CreatePanel(db.Id);
        _panelRepoMock.Setup(r => r.GetByIdAsync(panel.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(panel);

        var result = await CreateController().DeletePanel(panel.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _panelRepoMock.Verify(r => r.DeleteAsync(panel.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
