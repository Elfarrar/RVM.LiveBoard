using Microsoft.AspNetCore.SignalR;

namespace RVM.LiveBoard.API.Hubs;

public class LiveMetricHub : Hub
{
    public async Task JoinDashboard(string dashboardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard-{dashboardId}");
    }

    public async Task LeaveDashboard(string dashboardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dashboard-{dashboardId}");
    }

    public async Task SubscribeMetric(string metricName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"metric-{metricName}");
    }

    public async Task UnsubscribeMetric(string metricName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"metric-{metricName}");
    }
}
