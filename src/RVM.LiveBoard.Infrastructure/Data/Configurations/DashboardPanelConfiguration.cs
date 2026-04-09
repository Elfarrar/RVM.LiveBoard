using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Infrastructure.Data.Configurations;

public class DashboardPanelConfiguration : IEntityTypeConfiguration<DashboardPanel>
{
    public void Configure(EntityTypeBuilder<DashboardPanel> builder)
    {
        builder.ToTable("dashboard_panels");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.MetricName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.PanelType).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Aggregation).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(e => e.DashboardId);
    }
}
