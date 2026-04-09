using RVM.LiveBoard.Domain.Interfaces;

namespace RVM.LiveBoard.API.Services;

public class MetricCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MetricCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MetricCleanupWorker started");

        var intervalMinutes = configuration.GetValue("MetricCleanup:IntervalMinutes", 60);
        var retentionDays = configuration.GetValue("MetricCleanup:RetentionDays", 7);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var metricRepo = scope.ServiceProvider.GetRequiredService<IMetricDataPointRepository>();

                var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                var deleted = await metricRepo.DeleteOlderThanAsync(cutoff, stoppingToken);

                if (deleted > 0)
                    logger.LogInformation("MetricCleanup: purged {Count} data points older than {Days}d", deleted, retentionDays);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in MetricCleanupWorker");
            }
        }

        logger.LogInformation("MetricCleanupWorker stopped");
    }
}
