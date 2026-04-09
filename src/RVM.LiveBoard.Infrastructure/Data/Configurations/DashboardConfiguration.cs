using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Infrastructure.Data.Configurations;

public class DashboardConfiguration : IEntityTypeConfiguration<Dashboard>
{
    public void Configure(EntityTypeBuilder<Dashboard> builder)
    {
        builder.ToTable("dashboards");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);

        builder.HasIndex(e => e.Name).IsUnique();

        builder.HasMany(e => e.Panels)
            .WithOne(p => p.Dashboard)
            .HasForeignKey(p => p.DashboardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
