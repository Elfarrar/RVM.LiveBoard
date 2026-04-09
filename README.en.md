***English** | [Portugues](README.md)*

# RVM.LiveBoard

Real-time monitoring dashboard with metrics, configurable alerts, interactive panels and SignalR.

![build](https://img.shields.io/badge/build-passing-brightgreen)
![tests](https://img.shields.io/badge/tests-48%20passed-brightgreen)
![license](https://img.shields.io/badge/license-MIT-blue)
![dotnet](https://img.shields.io/badge/.NET-10.0-purple)

## About

RVM.LiveBoard is a real-time monitoring dashboard that supports metric ingestion, visualization through configurable panels (LineChart, BarChart, Gauge, Counter, Table, Heatmap), alert rules with conditions (gt, gte, lt, lte, eq), severities (Info, Warning, Critical), and data aggregation (Last, Average, Sum, Min, Max, Count). It uses SignalR for live metric and alert push notifications.

## Technologies

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Web Framework | ASP.NET Core 10 |
| Real-time | SignalR |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL |
| Driver | Npgsql 10.0.1 |
| Logging | Serilog |
| Testing | xUnit 2.9, Moq 4.20 |
| Containerization | Docker Compose |

## Architecture

```
+-----------------+       +------------------+       +-------------------+
|   Client /      | SignalR|   RVM.LiveBoard  |  EF   |    PostgreSQL     |
|   Frontend      |<------>|      .API        |<----->|                   |
|   (any)         | REST   |                  |       |   rvmliveboard    |
+-----------------+       +------------------+       +-------------------+
                            |       |       |
                    +-------+   +---+---+   +--------+
                    |           |       |            |
              Controllers   Services  Hubs     Middleware
              - Metrics      - MetricIngestion  - LiveMetricHub
              - Dashboards   - AlertEvaluation  - CorrelationId
              - Alerts       - MetricCleanup    - RateLimiter

+-----------------+       +------------------+       +-------------------+
| RVM.LiveBoard   |       | RVM.LiveBoard    |       | RVM.LiveBoard     |
|    .Domain      |<------+  .Infrastructure |       |    .Test           |
|                 |       |                  |       |                   |
| Entities        |       | Data/DbContext   |       | Domain            |
| Enums           |       | Repositories     |       | Infrastructure    |
| Interfaces      |       | Configurations   |       | Services          |
+-----------------+       +------------------+       +-------------------+
```

## Project Structure

```text
RVM.LiveBoard/
|- src/
|  |- RVM.LiveBoard.API/
|  |  |- Controllers/
|  |  |  |- MetricsController.cs
|  |  |  |- DashboardsController.cs
|  |  |  `- AlertsController.cs
|  |  |- Dtos/
|  |  |  |- MetricDtos.cs
|  |  |  |- DashboardDtos.cs
|  |  |  |- PanelDtos.cs
|  |  |  `- AlertDtos.cs
|  |  |- Hubs/
|  |  |  `- LiveMetricHub.cs
|  |  |- Services/
|  |  |  |- MetricIngestionService.cs
|  |  |  |- AlertEvaluationWorker.cs
|  |  |  `- MetricCleanupWorker.cs
|  |  |- Auth/
|  |  |  |- ApiKeyAuthHandler.cs
|  |  |  `- ApiKeyAuthOptions.cs
|  |  |- Health/
|  |  |  `- DatabaseHealthCheck.cs
|  |  |- Middleware/
|  |  |  `- CorrelationIdMiddleware.cs
|  |  |- Program.cs
|  |  `- appsettings.json
|  |- RVM.LiveBoard.Domain/
|  |  |- Entities/
|  |  |  |- MetricDataPoint.cs
|  |  |  |- Dashboard.cs
|  |  |  |- DashboardPanel.cs
|  |  |  |- AlertRule.cs
|  |  |  `- Alert.cs
|  |  |- Enums/
|  |  |  |- PanelType.cs
|  |  |  |- MetricAggregation.cs
|  |  |  |- AlertSeverity.cs
|  |  |  `- AlertStatus.cs
|  |  `- Interfaces/
|  |     |- IMetricDataPointRepository.cs
|  |     |- IDashboardRepository.cs
|  |     |- IDashboardPanelRepository.cs
|  |     |- IAlertRuleRepository.cs
|  |     `- IAlertRepository.cs
|  `- RVM.LiveBoard.Infrastructure/
|     |- Data/
|     |  |- LiveBoardDbContext.cs
|     |  `- Configurations/
|     |     |- MetricDataPointConfiguration.cs
|     |     |- DashboardConfiguration.cs
|     |     |- DashboardPanelConfiguration.cs
|     |     |- AlertConfiguration.cs
|     |     `- AlertRuleConfiguration.cs
|     |- Repositories/
|     |  |- MetricDataPointRepository.cs
|     |  |- DashboardRepository.cs
|     |  |- DashboardPanelRepository.cs
|     |  |- AlertRepository.cs
|     |  `- AlertRuleRepository.cs
|     `- DependencyInjection.cs
|- test/
|  `- RVM.LiveBoard.Test/
|     |- Domain/
|     |  `- EntityTests.cs
|     |- Infrastructure/
|     |  |- AlertRepositoryTests.cs
|     |  |- DashboardRepositoryTests.cs
|     |  `- MetricDataPointRepositoryTests.cs
|     `- Services/
|        `- MetricIngestionServiceTests.cs
|- docker-compose.dev.yml
|- docker-compose.prod.yml
|- global.json
`- RVM.LiveBoard.slnx
```

## Getting Started

### Prerequisites

- .NET SDK 10.0
- PostgreSQL (or Docker)

### Configuration

1. Clone the repository:

```bash
git clone https://github.com/your-user/RVM.LiveBoard.git
cd RVM.LiveBoard
```

2. Configure the connection string in `appsettings.json` or via environment variable:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=rvmliveboard;Username=postgres;Password=your_password"
```

3. Run the application (migrations run automatically on startup):

```bash
dotnet run --project src/RVM.LiveBoard.API
```

### Docker Compose

```bash
docker compose -f docker-compose.dev.yml up -d --build
```

## API Endpoints

All endpoints require API Key authentication (`Authorization` header), except `/health` and the SignalR hub.

### Metrics

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/metrics/ingest` | Ingest batch of metrics |
| `GET` | `/api/metrics/{metricName}` | Query metrics by name |
| `GET` | `/api/metrics/names` | List distinct metric names |
| `GET` | `/api/metrics/panel/{panelId}/data` | Aggregated data for a panel |

### Dashboards

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/dashboards` | List all dashboards |
| `GET` | `/api/dashboards/{id}` | Get dashboard with panels |
| `POST` | `/api/dashboards` | Create dashboard |
| `PUT` | `/api/dashboards/{id}` | Update dashboard |
| `DELETE` | `/api/dashboards/{id}` | Delete dashboard |
| `POST` | `/api/dashboards/{id}/panels` | Add panel to dashboard |
| `PUT` | `/api/dashboards/panels/{panelId}` | Update panel |
| `DELETE` | `/api/dashboards/panels/{panelId}` | Delete panel |

### Alerts

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/alerts` | List alerts (filter by status) |
| `POST` | `/api/alerts/{id}/acknowledge` | Acknowledge alert |
| `POST` | `/api/alerts/{id}/resolve` | Resolve alert |
| `GET` | `/api/alerts/rules` | List alert rules |
| `GET` | `/api/alerts/rules/{id}` | Get alert rule |
| `POST` | `/api/alerts/rules` | Create alert rule |
| `PUT` | `/api/alerts/rules/{id}` | Update alert rule |
| `DELETE` | `/api/alerts/rules/{id}` | Delete alert rule |

### SignalR

| Hub | Route | Methods |
|---|---|---|
| `LiveMetricHub` | `/hubs/live-metrics` | `JoinDashboard`, `LeaveDashboard`, `SubscribeMetric`, `UnsubscribeMetric` |

Push events: `MetricReceived`, `AlertFired`

### Health Check

| Method | Route | Description |
|---|---|---|
| `GET` | `/health` | Health check (includes database verification) |

## Tests

The project has **48 tests** covering domain, infrastructure, and services.

```bash
dotnet test test/RVM.LiveBoard.Test
```

| File | Tests | Type |
|---|---|---|
| `EntityTests.cs` | 18 | Domain (entities, enums, lifecycle) |
| `MetricDataPointRepositoryTests.cs` | 13 | Infrastructure (CRUD, aggregation, cleanup) |
| `AlertRepositoryTests.cs` | 6 | Infrastructure (rules, alerts, status) |
| `DashboardRepositoryTests.cs` | 6 | Infrastructure (CRUD, panels, ordering) |
| `MetricIngestionServiceTests.cs` | 5 | Service (ingestion, SignalR push, timestamps) |
| **Total** | **48** | |

## Features

- **Configurable dashboards**: panels organized in a grid with customizable position and size
- **6 panel types**: LineChart, BarChart, Gauge, Counter, Table, Heatmap
- **Metric ingestion**: batch endpoint with name, value, unit, source and tags
- **Data aggregation**: Last, Average, Sum, Min, Max, Count per time window
- **Alert rules**: conditions gt, gte, lt, lte, eq with severity Info, Warning, Critical
- **AlertEvaluationWorker**: evaluates rules every 30s and fires alerts automatically
- **MetricCleanupWorker**: purges expired metrics (7-day retention, 60min cycle)
- **SignalR Hub with groups**: metric push per metric group and alert broadcast
- **Acknowledge and Resolve**: alert lifecycle (Active -> Acknowledged -> Resolved)
- **API Key authentication**: authentication scheme with configurable keys
- **Rate Limiting**: 60 requests/minute per IP
- **Correlation ID**: request tracing via middleware
- **Health Check**: endpoint with database verification
- **Auto-migration**: migrations run automatically on startup

---

<p align="center">
  <strong>RVM.LiveBoard</strong> &mdash; Part of the <a href="https://github.com/your-user">RVM Tech</a> ecosystem
</p>
