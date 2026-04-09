using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;

namespace RVM.LiveBoard.Test.Domain;

public class EntityTests
{
    [Fact]
    public void Dashboard_Defaults_AreCorrect()
    {
        var dashboard = new Dashboard();

        Assert.NotEqual(Guid.Empty, dashboard.Id);
        Assert.Equal(string.Empty, dashboard.Name);
        Assert.Null(dashboard.Description);
        Assert.False(dashboard.IsDefault);
        Assert.Equal(10, dashboard.RefreshIntervalSeconds);
        Assert.True(dashboard.CreatedAt <= DateTime.UtcNow);
        Assert.Null(dashboard.UpdatedAt);
        Assert.Empty(dashboard.Panels);
    }

    [Fact]
    public void DashboardPanel_Defaults_AreCorrect()
    {
        var panel = new DashboardPanel();

        Assert.NotEqual(Guid.Empty, panel.Id);
        Assert.Equal(PanelType.LineChart, default);
        Assert.Equal(MetricAggregation.Last, panel.Aggregation);
        Assert.Equal(6, panel.GridWidth);
        Assert.Equal(4, panel.GridHeight);
        Assert.Equal(0, panel.SortOrder);
        Assert.Null(panel.TimeWindowMinutes);
    }

    [Fact]
    public void MetricDataPoint_Defaults_AreCorrect()
    {
        var point = new MetricDataPoint();

        Assert.NotEqual(Guid.Empty, point.Id);
        Assert.Equal(string.Empty, point.MetricName);
        Assert.Equal(0, point.Value);
        Assert.Null(point.Unit);
        Assert.Null(point.Source);
        Assert.Null(point.Tags);
        Assert.True(point.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void AlertRule_Defaults_AreCorrect()
    {
        var rule = new AlertRule();

        Assert.NotEqual(Guid.Empty, rule.Id);
        Assert.Equal(string.Empty, rule.Name);
        Assert.Equal("gt", rule.Condition);
        Assert.Equal(0, rule.Threshold);
        Assert.Equal(5, rule.EvaluationWindowMinutes);
        Assert.Equal(AlertSeverity.Warning, rule.Severity);
        Assert.True(rule.IsEnabled);
        Assert.Null(rule.UpdatedAt);
        Assert.Empty(rule.Alerts);
    }

    [Fact]
    public void Alert_Defaults_AreCorrect()
    {
        var alert = new Alert();

        Assert.NotEqual(Guid.Empty, alert.Id);
        Assert.Equal(AlertStatus.Active, alert.Status);
        Assert.Equal(0, alert.TriggerValue);
        Assert.Null(alert.Message);
        Assert.True(alert.FiredAt <= DateTime.UtcNow);
        Assert.Null(alert.AcknowledgedAt);
        Assert.Null(alert.ResolvedAt);
    }

    [Fact]
    public void Alert_Lifecycle_Transitions()
    {
        var alert = new Alert
        {
            TriggerValue = 95.5,
            Message = "CPU high",
        };

        Assert.Equal(AlertStatus.Active, alert.Status);

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAt = DateTime.UtcNow;
        Assert.Equal(AlertStatus.Acknowledged, alert.Status);
        Assert.NotNull(alert.AcknowledgedAt);

        alert.Status = AlertStatus.Resolved;
        alert.ResolvedAt = DateTime.UtcNow;
        Assert.Equal(AlertStatus.Resolved, alert.Status);
        Assert.NotNull(alert.ResolvedAt);
    }

    [Theory]
    [InlineData(PanelType.LineChart, 0)]
    [InlineData(PanelType.BarChart, 1)]
    [InlineData(PanelType.Gauge, 2)]
    [InlineData(PanelType.Counter, 3)]
    [InlineData(PanelType.Table, 4)]
    [InlineData(PanelType.Heatmap, 5)]
    public void PanelType_HasExpectedValues(PanelType type, int expected)
    {
        Assert.Equal(expected, (int)type);
    }

    [Theory]
    [InlineData(MetricAggregation.Last, 0)]
    [InlineData(MetricAggregation.Average, 1)]
    [InlineData(MetricAggregation.Sum, 2)]
    [InlineData(MetricAggregation.Min, 3)]
    [InlineData(MetricAggregation.Max, 4)]
    [InlineData(MetricAggregation.Count, 5)]
    public void MetricAggregation_HasExpectedValues(MetricAggregation agg, int expected)
    {
        Assert.Equal(expected, (int)agg);
    }
}
