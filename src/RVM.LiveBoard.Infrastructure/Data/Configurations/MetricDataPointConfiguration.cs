using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Infrastructure.Data.Configurations;

public class MetricDataPointConfiguration : IEntityTypeConfiguration<MetricDataPoint>
{
    public void Configure(EntityTypeBuilder<MetricDataPoint> builder)
    {
        builder.ToTable("metric_data_points");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MetricName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Unit).HasMaxLength(50);
        builder.Property(e => e.Source).HasMaxLength(200);
        builder.Property(e => e.Tags).HasMaxLength(2000);

        builder.HasIndex(e => new { e.MetricName, e.Timestamp });
        builder.HasIndex(e => e.Timestamp);
    }
}
