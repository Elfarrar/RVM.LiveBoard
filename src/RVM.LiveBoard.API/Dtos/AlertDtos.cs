namespace RVM.LiveBoard.API.Dtos;

public record CreateAlertRuleRequest(
    string Name,
    string MetricName,
    string Condition = "gt",
    double Threshold = 0,
    int EvaluationWindowMinutes = 5,
    string Severity = "Warning",
    bool IsEnabled = true);

public record UpdateAlertRuleRequest(
    string? Name,
    string? MetricName,
    string? Condition,
    double? Threshold,
    int? EvaluationWindowMinutes,
    string? Severity,
    bool? IsEnabled);

public record AlertRuleResponse(
    Guid Id,
    string Name,
    string MetricName,
    string Condition,
    double Threshold,
    int EvaluationWindowMinutes,
    string Severity,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record AlertResponse(
    Guid Id,
    Guid AlertRuleId,
    string RuleName,
    string Status,
    double TriggerValue,
    string? Message,
    DateTime FiredAt,
    DateTime? AcknowledgedAt,
    DateTime? ResolvedAt);

public record AlertListResponse(
    List<AlertResponse> Items,
    int TotalCount,
    int Offset,
    int Limit);
