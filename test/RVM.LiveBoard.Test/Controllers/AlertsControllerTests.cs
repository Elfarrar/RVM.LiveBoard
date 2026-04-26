using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RVM.LiveBoard.API.Controllers;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.Test.Controllers;

public class AlertsControllerTests
{
    private readonly Mock<IAlertRuleRepository> _ruleRepoMock = new();
    private readonly Mock<IAlertRepository> _alertRepoMock = new();
    private readonly Mock<ILogger<AlertsController>> _loggerMock = new();

    private AlertsController CreateController() =>
        new(_ruleRepoMock.Object, _alertRepoMock.Object, _loggerMock.Object);

    private static AlertRule CreateRule(string name = "High CPU", string metric = "cpu.usage",
        bool enabled = true) => new()
        {
            Name = name,
            MetricName = metric,
            Condition = "gt",
            Threshold = 80,
            EvaluationWindowMinutes = 5,
            Severity = AlertSeverity.Warning,
            IsEnabled = enabled,
        };

    private static Alert CreateAlert(Guid? ruleId = null, AlertStatus status = AlertStatus.Active) => new()
    {
        AlertRuleId = ruleId ?? Guid.NewGuid(),
        Status = status,
        TriggerValue = 95.0,
        Message = "CPU too high",
        FiredAt = DateTime.UtcNow,
    };

    // -------------------------------------------------------------------------
    // GetAllRules
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllRules_ReturnsAllRules()
    {
        _ruleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateRule("CPU"), CreateRule("Mem", "mem.usage")]);

        var result = await CreateController().GetAllRules(CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetAllRules_Empty_ReturnsEmptyList()
    {
        _ruleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateController().GetAllRules(CancellationToken.None);

        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAllRules_MapsFieldsCorrectly()
    {
        var rule = CreateRule("Test Rule", "cpu", true);
        _ruleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);

        var result = await CreateController().GetAllRules(CancellationToken.None);

        var dto = Assert.Single(result.Value!);
        Assert.Equal(rule.Id, dto.Id);
        Assert.Equal("Test Rule", dto.Name);
        Assert.Equal("cpu", dto.MetricName);
        Assert.Equal("Warning", dto.Severity);
        Assert.True(dto.IsEnabled);
    }

    // -------------------------------------------------------------------------
    // GetRule
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetRule_ExistingRule_ReturnsOk()
    {
        var rule = CreateRule();
        _ruleRepoMock.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var result = await CreateController().GetRule(rule.Id, CancellationToken.None);

        Assert.Equal(rule.Name, result.Value!.Name);
    }

    [Fact]
    public async Task GetRule_NotFound_ReturnsNotFound()
    {
        _ruleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertRule?)null);

        var result = await CreateController().GetRule(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -------------------------------------------------------------------------
    // CreateRule
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateRule_ValidRequest_ReturnsCreatedAt()
    {
        var request = new CreateAlertRuleRequest("High CPU", "cpu.usage", "gt", 80, 5, "Warning", true);

        var result = await CreateController().CreateRule(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<AlertRuleResponse>(created.Value);
        Assert.Equal("High CPU", dto.Name);
        _ruleRepoMock.Verify(r => r.AddAsync(It.IsAny<AlertRule>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRule_InvalidSeverity_FallsBackToWarning()
    {
        var request = new CreateAlertRuleRequest("Test", "cpu", "gt", 80, 5, "InvalidSeverity", true);

        var result = await CreateController().CreateRule(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<AlertRuleResponse>(created.Value);
        Assert.Equal("Warning", dto.Severity);
    }

    [Fact]
    public async Task CreateRule_CriticalSeverity_ParsesCorrectly()
    {
        var request = new CreateAlertRuleRequest("Test", "cpu", "gt", 80, 5, "Critical", true);

        var result = await CreateController().CreateRule(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<AlertRuleResponse>(created.Value);
        Assert.Equal("Critical", dto.Severity);
    }

    // -------------------------------------------------------------------------
    // UpdateRule
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateRule_NotFound_ReturnsNotFound()
    {
        _ruleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertRule?)null);

        var result = await CreateController().UpdateRule(Guid.NewGuid(),
            new UpdateAlertRuleRequest(null, null, null, null, null, null, null), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateRule_Found_UpdatesFields()
    {
        var rule = CreateRule("Old Name");
        _ruleRepoMock.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var result = await CreateController().UpdateRule(rule.Id,
            new UpdateAlertRuleRequest("New Name", "mem.usage", "lt", 50, 10, "Critical", false),
            CancellationToken.None);

        var dto = Assert.IsType<AlertRuleResponse>(result.Value);
        Assert.Equal("New Name", dto.Name);
        Assert.Equal("mem.usage", dto.MetricName);
        Assert.Equal("lt", dto.Condition);
        Assert.Equal(50, dto.Threshold);
        Assert.Equal("Critical", dto.Severity);
        Assert.False(dto.IsEnabled);
        _ruleRepoMock.Verify(r => r.UpdateAsync(It.IsAny<AlertRule>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRule_NullFields_PreservesExistingValues()
    {
        var rule = CreateRule("Keep Name");
        rule.Threshold = 99;
        _ruleRepoMock.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var result = await CreateController().UpdateRule(rule.Id,
            new UpdateAlertRuleRequest(null, null, null, null, null, null, null),
            CancellationToken.None);

        var dto = Assert.IsType<AlertRuleResponse>(result.Value);
        Assert.Equal("Keep Name", dto.Name);
        Assert.Equal(99, dto.Threshold);
    }

    // -------------------------------------------------------------------------
    // DeleteRule
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteRule_NotFound_ReturnsNotFound()
    {
        _ruleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertRule?)null);

        var result = await CreateController().DeleteRule(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteRule_Found_ReturnsNoContent()
    {
        var rule = CreateRule();
        _ruleRepoMock.Setup(r => r.GetByIdAsync(rule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var result = await CreateController().DeleteRule(rule.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _ruleRepoMock.Verify(r => r.DeleteAsync(rule.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetAlerts
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAlerts_NoStatus_ReturnsAll()
    {
        var ruleId = Guid.NewGuid();
        _alertRepoMock.Setup(r => r.GetByStatusAsync(null, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateAlert(ruleId, AlertStatus.Active), CreateAlert(ruleId, AlertStatus.Resolved)]);
        _alertRepoMock.Setup(r => r.CountByStatusAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await CreateController().GetAlerts(null, 0, 50, CancellationToken.None);

        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAlerts_WithActiveStatus_FiltersCorrectly()
    {
        _alertRepoMock.Setup(r => r.GetByStatusAsync(AlertStatus.Active, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateAlert(status: AlertStatus.Active)]);
        _alertRepoMock.Setup(r => r.CountByStatusAsync(AlertStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await CreateController().GetAlerts("active", 0, 50, CancellationToken.None);

        Assert.Single(result.Value!.Items);
        Assert.Equal("Active", result.Value!.Items[0].Status);
    }

    [Fact]
    public async Task GetAlerts_InvalidStatus_TreatsAsNull()
    {
        _alertRepoMock.Setup(r => r.GetByStatusAsync(null, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _alertRepoMock.Setup(r => r.CountByStatusAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await CreateController().GetAlerts("invalid_status", 0, 50, CancellationToken.None);

        _alertRepoMock.Verify(r => r.GetByStatusAsync(null, 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAlerts_Pagination_PassesOffsetAndLimit()
    {
        _alertRepoMock.Setup(r => r.GetByStatusAsync(null, 10, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _alertRepoMock.Setup(r => r.CountByStatusAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await CreateController().GetAlerts(null, 10, 25, CancellationToken.None);

        Assert.Equal(10, result.Value!.Offset);
        Assert.Equal(25, result.Value!.Limit);
    }

    // -------------------------------------------------------------------------
    // Acknowledge
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Acknowledge_NotFound_ReturnsNotFound()
    {
        _alertRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);

        var result = await CreateController().Acknowledge(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Acknowledge_Found_SetsStatusAndTimestamp()
    {
        var alert = CreateAlert();
        _alertRepoMock.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var result = await CreateController().Acknowledge(alert.Id, CancellationToken.None);

        var dto = Assert.IsType<AlertResponse>(result.Value);
        Assert.Equal("Acknowledged", dto.Status);
        Assert.NotNull(dto.AcknowledgedAt);
        _alertRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Resolve
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Resolve_NotFound_ReturnsNotFound()
    {
        _alertRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Alert?)null);

        var result = await CreateController().Resolve(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Resolve_Found_SetsStatusAndTimestamp()
    {
        var alert = CreateAlert();
        _alertRepoMock.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var result = await CreateController().Resolve(alert.Id, CancellationToken.None);

        var dto = Assert.IsType<AlertResponse>(result.Value);
        Assert.Equal("Resolved", dto.Status);
        Assert.NotNull(dto.ResolvedAt);
    }
}
