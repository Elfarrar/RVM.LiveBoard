namespace RVM.LiveBoard.API.Dtos;

public record IngestMetricRequest(
    string MetricName,
    double Value,
    string? Unit = null,
    string? Source = null,
    string? Tags = null,
    DateTime? Timestamp = null);

public record IngestMetricBatchRequest(List<IngestMetricRequest> Metrics);

public record IngestMetricBatchResponse(int Accepted);

public record MetricDataPointResponse(
    Guid Id,
    string MetricName,
    double Value,
    string? Unit,
    string? Source,
    string? Tags,
    DateTime Timestamp);

public record MetricQueryResponse(
    string MetricName,
    List<MetricDataPointResponse> DataPoints,
    int TotalCount);

public record MetricNamesResponse(List<string> Names);

public record PanelDataResponse(
    Guid PanelId,
    string MetricName,
    string Aggregation,
    double? AggregatedValue,
    List<MetricDataPointResponse> DataPoints);
