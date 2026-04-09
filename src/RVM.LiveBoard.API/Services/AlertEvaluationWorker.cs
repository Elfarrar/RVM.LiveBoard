using Microsoft.AspNetCore.SignalR;
using RVM.LiveBoard.API.Dtos;
using RVM.LiveBoard.API.Hubs;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.API.Services;

public class AlertEvaluationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AlertEvaluationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AlertEvaluationWorker started");

        var intervalSeconds = configuration.GetValue("AlertEvaluation:IntervalSeconds", 30);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var ruleRepo = scope.ServiceProvider.GetRequiredService<IAlertRuleRepository>();
                var alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
                var metricRepo = scope.ServiceProvider.GetRequiredService<IMetricDataPointRepository>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LiveMetricHub>>();

                var rules = await ruleRepo.GetEnabledAsync(stoppingToken);

                foreach (var rule in rules)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    var to = DateTime.UtcNow;
                    var from = to.AddMinutes(-rule.EvaluationWindowMinutes);
                    var value = await metricRepo.GetAggregatedValueAsync(rule.MetricName, "avg", from, to, stoppingToken);

                    if (value is null) continue;

                    var breached = rule.Condition.ToLowerInvariant() switch
                    {
                        "gt" => value > rule.Threshold,
                        "gte" => value >= rule.Threshold,
                        "lt" => value < rule.Threshold,
                        "lte" => value <= rule.Threshold,
                        "eq" => Math.Abs(value.Value - rule.Threshold) < 0.0001,
                        _ => false,
                    };

                    if (breached)
                    {
                        var alert = new Alert
                        {
                            AlertRuleId = rule.Id,
                            TriggerValue = value.Value,
                            Message = $"Rule '{rule.Name}': {rule.MetricName} {rule.Condition} {rule.Threshold} (actual: {value.Value:F2})",
                        };
                        await alertRepo.AddAsync(alert, stoppingToken);

                        var dto = new AlertResponse(
                            alert.Id, alert.AlertRuleId, rule.Name,
                            alert.Status.ToString(), alert.TriggerValue,
                            alert.Message, alert.FiredAt, null, null);

                        await hubContext.Clients.All.SendAsync("AlertFired", dto, stoppingToken);

                        logger.LogWarning("Alert fired: {RuleName} — {Message}", rule.Name, alert.Message);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AlertEvaluationWorker");
            }
        }

        logger.LogInformation("AlertEvaluationWorker stopped");
    }
}
