using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Infrastructure.Data;

public class LiveBoardDbContext(DbContextOptions<LiveBoardDbContext> options) : DbContext(options)
{
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<DashboardPanel> DashboardPanels => Set<DashboardPanel>();
    public DbSet<MetricDataPoint> MetricDataPoints => Set<MetricDataPoint>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LiveBoardDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Dashboard>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var entry in ChangeTracker.Entries<AlertRule>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
