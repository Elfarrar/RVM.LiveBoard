using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;
using RVM.LiveBoard.Infrastructure.Data;
using RVM.LiveBoard.Infrastructure.Repositories;

namespace RVM.LiveBoard.Test.Infrastructure;

public class AlertRepositoryTests : IDisposable
{
    private readonly LiveBoardDbContext _db;
    private readonly AlertRuleRepository _ruleRepo;
    private readonly AlertRepository _alertRepo;

    public AlertRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<LiveBoardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new LiveBoardDbContext(options);
        _ruleRepo = new AlertRuleRepository(_db);
        _alertRepo = new AlertRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddRule_And_GetEnabled()
    {
        await _ruleRepo.AddAsync(new AlertRule { Name = "High CPU", MetricName = "cpu", IsEnabled = true });
        await _ruleRepo.AddAsync(new AlertRule { Name = "Disabled", MetricName = "mem", IsEnabled = false });

        var enabled = await _ruleRepo.GetEnabledAsync();

        Assert.Single(enabled);
        Assert.Equal("High CPU", enabled[0].Name);
    }

    [Fact]
    public async Task UpdateRule_SetsUpdatedAt()
    {
        var rule = new AlertRule { Name = "Test", MetricName = "cpu" };
        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync();
        Assert.Null(rule.UpdatedAt);

        rule.Threshold = 90;
        await _ruleRepo.UpdateAsync(rule);

        var found = await _db.AlertRules.FirstAsync();
        Assert.NotNull(found.UpdatedAt);
    }

    [Fact]
    public async Task DeleteRule_RemovesRule()
    {
        var rule = new AlertRule { Name = "Delete", MetricName = "cpu" };
        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync();

        await _ruleRepo.DeleteAsync(rule.Id);

        Assert.Equal(0, await _db.AlertRules.CountAsync());
    }

    [Fact]
    public async Task AddAlert_And_GetByStatus()
    {
        var rule = new AlertRule { Name = "CPU Rule", MetricName = "cpu" };
        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync();

        await _alertRepo.AddAsync(new Alert { AlertRuleId = rule.Id, Status = AlertStatus.Active, TriggerValue = 95 });
        await _alertRepo.AddAsync(new Alert { AlertRuleId = rule.Id, Status = AlertStatus.Resolved, TriggerValue = 80 });

        var active = await _alertRepo.GetByStatusAsync(AlertStatus.Active, 0, 50);
        var all = await _alertRepo.GetByStatusAsync(null, 0, 50);

        Assert.Single(active);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task CountByStatus_ReturnsCorrectCount()
    {
        var rule = new AlertRule { Name = "Rule", MetricName = "cpu" };
        _db.AlertRules.Add(rule);
        _db.Alerts.AddRange(
            new Alert { AlertRuleId = rule.Id, Status = AlertStatus.Active },
            new Alert { AlertRuleId = rule.Id, Status = AlertStatus.Active },
            new Alert { AlertRuleId = rule.Id, Status = AlertStatus.Resolved });
        await _db.SaveChangesAsync();

        var activeCount = await _alertRepo.CountByStatusAsync(AlertStatus.Active);
        var totalCount = await _alertRepo.CountByStatusAsync(null);

        Assert.Equal(2, activeCount);
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task UpdateAlert_ChangesStatus()
    {
        var rule = new AlertRule { Name = "Rule", MetricName = "cpu" };
        _db.AlertRules.Add(rule);
        var alert = new Alert { AlertRuleId = rule.Id, TriggerValue = 95 };
        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync();

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAt = DateTime.UtcNow;
        await _alertRepo.UpdateAsync(alert);

        var found = await _db.Alerts.FirstAsync();
        Assert.Equal(AlertStatus.Acknowledged, found.Status);
    }
}
