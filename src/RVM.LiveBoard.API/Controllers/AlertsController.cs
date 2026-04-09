using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertsController(
    IAlertRuleRepository ruleRepo,
    IAlertRepository alertRepo,
    ILogger<AlertsController> logger) : ControllerBase
{
    // --- Alert Rules ---

    [HttpGet("rules")]
    public async Task<ActionResult<List<AlertRuleResponse>>> GetAllRules(CancellationToken ct)
    {
        var rules = await ruleRepo.GetAllAsync(ct);
        return rules.Select(MapRule).ToList();
    }

    [HttpGet("rules/{id:guid}")]
    public async Task<ActionResult<AlertRuleResponse>> GetRule(Guid id, CancellationToken ct)
    {
        var rule = await ruleRepo.GetByIdAsync(id, ct);
        if (rule is null) return NotFound();
        return MapRule(rule);
    }

    [HttpPost("rules")]
    public async Task<ActionResult<AlertRuleResponse>> CreateRule(CreateAlertRuleRequest request, CancellationToken ct)
    {
        var rule = new AlertRule
        {
            Name = request.Name,
            MetricName = request.MetricName,
            Condition = request.Condition,
            Threshold = request.Threshold,
            EvaluationWindowMinutes = request.EvaluationWindowMinutes,
            Severity = Enum.TryParse<AlertSeverity>(request.Severity, true, out var sev) ? sev : AlertSeverity.Warning,
            IsEnabled = request.IsEnabled,
        };

        await ruleRepo.AddAsync(rule, ct);
        logger.LogInformation("Created alert rule '{Name}' for metric '{Metric}'", rule.Name, rule.MetricName);
        return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, MapRule(rule));
    }

    [HttpPut("rules/{id:guid}")]
    public async Task<ActionResult<AlertRuleResponse>> UpdateRule(Guid id, UpdateAlertRuleRequest request, CancellationToken ct)
    {
        var rule = await ruleRepo.GetByIdAsync(id, ct);
        if (rule is null) return NotFound();

        if (request.Name is not null) rule.Name = request.Name;
        if (request.MetricName is not null) rule.MetricName = request.MetricName;
        if (request.Condition is not null) rule.Condition = request.Condition;
        if (request.Threshold.HasValue) rule.Threshold = request.Threshold.Value;
        if (request.EvaluationWindowMinutes.HasValue) rule.EvaluationWindowMinutes = request.EvaluationWindowMinutes.Value;
        if (request.Severity is not null && Enum.TryParse<AlertSeverity>(request.Severity, true, out var sev)) rule.Severity = sev;
        if (request.IsEnabled.HasValue) rule.IsEnabled = request.IsEnabled.Value;

        await ruleRepo.UpdateAsync(rule, ct);
        return MapRule(rule);
    }

    [HttpDelete("rules/{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken ct)
    {
        var rule = await ruleRepo.GetByIdAsync(id, ct);
        if (rule is null) return NotFound();

        await ruleRepo.DeleteAsync(id, ct);
        return NoContent();
    }

    // --- Alerts ---

    [HttpGet]
    public async Task<ActionResult<AlertListResponse>> GetAlerts(
        [FromQuery] string? status,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        AlertStatus? parsedStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AlertStatus>(status, true, out var s))
            parsedStatus = s;

        var alerts = await alertRepo.GetByStatusAsync(parsedStatus, offset, limit, ct);
        var total = await alertRepo.CountByStatusAsync(parsedStatus, ct);

        var items = alerts.Select(MapAlert).ToList();
        return new AlertListResponse(items, total, offset, limit);
    }

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<ActionResult<AlertResponse>> Acknowledge(Guid id, CancellationToken ct)
    {
        var alert = await alertRepo.GetByIdAsync(id, ct);
        if (alert is null) return NotFound();

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAt = DateTime.UtcNow;
        await alertRepo.UpdateAsync(alert, ct);
        return MapAlert(alert);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<AlertResponse>> Resolve(Guid id, CancellationToken ct)
    {
        var alert = await alertRepo.GetByIdAsync(id, ct);
        if (alert is null) return NotFound();

        alert.Status = AlertStatus.Resolved;
        alert.ResolvedAt = DateTime.UtcNow;
        await alertRepo.UpdateAsync(alert, ct);
        return MapAlert(alert);
    }

    private static AlertRuleResponse MapRule(AlertRule r) => new(
        r.Id, r.Name, r.MetricName, r.Condition, r.Threshold,
        r.EvaluationWindowMinutes, r.Severity.ToString(), r.IsEnabled,
        r.CreatedAt, r.UpdatedAt);

    private static AlertResponse MapAlert(Alert a) => new(
        a.Id, a.AlertRuleId, a.Rule?.Name ?? "Unknown", a.Status.ToString(),
        a.TriggerValue, a.Message, a.FiredAt, a.AcknowledgedAt, a.ResolvedAt);
}
