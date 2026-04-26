# RVM.LiveBoard

## Visao Geral
Dashboard de metricas em tempo real com ingestao de dados via API, avaliacao de alertas por threshold e visualizacao ao vivo via SignalR. Inclui cleanup automatico de metricas antigas via worker background. Interface Blazor Server com atualizacoes push sem polling.

Projeto portfolio demonstrando ingestao de metricas, alertas dinamicos, cleanup de dados e streaming em tempo real com SignalR.

## Stack
- .NET 10, ASP.NET Core, Blazor Server
- SignalR (`LiveMetricHub` em `/hubs/live-metrics`)
- Entity Framework Core + PostgreSQL (metricas, alertas, configuracoes)
- Redis (cache de metricas recentes — via Infrastructure)
- Autenticacao via API Key
- Rate limiting: 60 req/min global
- Serilog + Seq, RVM.Common.Security
- xUnit 101 testes, Playwright E2E

## Estrutura do Projeto
```
src/
  RVM.LiveBoard.API/
    Auth/                     # ApiKeyAuthHandler
    Components/               # Blazor pages (dashboard, widgets, alertas)
    Controllers/              # REST: ingestao de metricas, CRUD alertas
    Health/                   # DatabaseHealthCheck
    Hubs/                     # LiveMetricHub (SignalR)
    Middleware/               # CorrelationIdMiddleware
    Services/
      MetricIngestionService  # Valida e persiste metricas recebidas
      AlertEvaluationWorker   # BackgroundService: avalia thresholds
      MetricCleanupWorker     # BackgroundService: remove metricas antigas
  RVM.LiveBoard.Domain/       # Entidades (Metric, Alert, Dashboard, Widget)
  RVM.LiveBoard.Infrastructure/
    Data/                     # LiveBoardDbContext
    Repositories/             # IMetricRepository, IAlertRepository
test/
  RVM.LiveBoard.Test/         # xUnit (101 testes)
  playwright/                 # Testes E2E
```

## Convencoes
- `AlertEvaluationWorker` e `MetricCleanupWorker` sao BackgroundServices — nao bloqueiam requests
- `LiveMetricHub` anonimo (`AllowAnonymous`) — dashboard publico
- `MetricIngestionService` e scoped — um contexto de DB por request de ingestao
- Metricas com TTL configuravel; `MetricCleanupWorker` roda periodicamente
- `EnsureCreated` em dev, migration EF Core em producao

## Como Rodar
### Dev
```bash
docker compose -f docker-compose.dev.yml up -d
cd src/RVM.LiveBoard.API
dotnet run
```

### Testes
```bash
dotnet test test/RVM.LiveBoard.Test/
```

## Decisoes Arquiteturais
- **Dois workers separados (Alert + Cleanup)**: responsabilidades distintas com intervalos diferentes — alertas em segundos, cleanup em horas
- **SignalR hub anonimo**: dashboard de metricas e informacional e publico, sem necessidade de auth por widget
- **MetricIngestionService scoped**: ingestao e um request HTTP normal — nao precisa de singleton
- **101 testes**: maior suite do portfolio — metricas e alertas tem muitas combinacoes de edge case
