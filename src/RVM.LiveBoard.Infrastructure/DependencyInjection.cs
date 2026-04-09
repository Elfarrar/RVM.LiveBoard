using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RVM.LiveBoard.Domain.Interfaces;
using RVM.LiveBoard.Infrastructure.Data;
using RVM.LiveBoard.Infrastructure.Repositories;

namespace RVM.LiveBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LiveBoardDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IDashboardPanelRepository, DashboardPanelRepository>();
        services.AddScoped<IMetricDataPointRepository, MetricDataPointRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        return services;
    }
}
