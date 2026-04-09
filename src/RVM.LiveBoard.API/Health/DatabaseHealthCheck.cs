using Microsoft.Extensions.Diagnostics.HealthChecks;
using RVM.LiveBoard.Infrastructure.Data;

namespace RVM.LiveBoard.API.Health;

public class DatabaseHealthCheck(LiveBoardDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await db.Database.CanConnectAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unavailable", ex);
        }
    }
}
