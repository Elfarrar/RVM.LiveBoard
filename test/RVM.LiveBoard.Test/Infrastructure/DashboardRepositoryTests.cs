using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;
using RVM.LiveBoard.Infrastructure.Data;
using RVM.LiveBoard.Infrastructure.Repositories;

namespace RVM.LiveBoard.Test.Infrastructure;

public class DashboardRepositoryTests : IDisposable
{
    private readonly LiveBoardDbContext _db;
    private readonly DashboardRepository _repo;

    public DashboardRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<LiveBoardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new LiveBoardDbContext(options);
        _repo = new DashboardRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddAsync_PersistsDashboard()
    {
        var dashboard = new Dashboard { Name = "Main Dashboard" };
        await _repo.AddAsync(dashboard);

        Assert.Equal(1, await _db.Dashboards.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDashboard()
    {
        var dashboard = new Dashboard { Name = "Test" };
        _db.Dashboards.Add(dashboard);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(dashboard.Id);

        Assert.NotNull(found);
        Assert.Equal("Test", found.Name);
    }

    [Fact]
    public async Task GetByIdWithPanelsAsync_IncludesPanels()
    {
        var dashboard = new Dashboard { Name = "With Panels" };
        dashboard.Panels.Add(new DashboardPanel
        {
            Title = "CPU Usage",
            MetricName = "cpu.usage",
            PanelType = PanelType.LineChart,
            SortOrder = 1,
        });
        dashboard.Panels.Add(new DashboardPanel
        {
            Title = "Memory",
            MetricName = "memory.usage",
            PanelType = PanelType.Gauge,
            SortOrder = 0,
        });
        _db.Dashboards.Add(dashboard);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdWithPanelsAsync(dashboard.Id);

        Assert.NotNull(found);
        Assert.Equal(2, found.Panels.Count);
        Assert.Contains(found.Panels, p => p.Title == "CPU Usage");
        Assert.Contains(found.Panels, p => p.Title == "Memory");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByName()
    {
        _db.Dashboards.Add(new Dashboard { Name = "Zebra" });
        _db.Dashboards.Add(new Dashboard { Name = "Alpha" });
        await _db.SaveChangesAsync();

        var all = await _repo.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal("Alpha", all[0].Name);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var dashboard = new Dashboard { Name = "Original" };
        _db.Dashboards.Add(dashboard);
        await _db.SaveChangesAsync();
        Assert.Null(dashboard.UpdatedAt);

        dashboard.Name = "Updated";
        await _repo.UpdateAsync(dashboard);

        var found = await _db.Dashboards.FirstAsync();
        Assert.Equal("Updated", found.Name);
        Assert.NotNull(found.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDashboard()
    {
        var dashboard = new Dashboard { Name = "Delete Me" };
        _db.Dashboards.Add(dashboard);
        await _db.SaveChangesAsync();

        await _repo.DeleteAsync(dashboard.Id);

        Assert.Equal(0, await _db.Dashboards.CountAsync());
    }
}
