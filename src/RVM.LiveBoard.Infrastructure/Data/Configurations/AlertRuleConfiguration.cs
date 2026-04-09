using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Infrastructure.Data.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("alert_rules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.MetricName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Condition).IsRequired().HasMaxLength(10);
        builder.Property(e => e.Severity).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(e => e.MetricName);

        builder.HasMany(e => e.Alerts)
            .WithOne(a => a.Rule)
            .HasForeignKey(a => a.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
