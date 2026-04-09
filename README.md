*[English](README.en.md) | **Portugues***

# RVM.LiveBoard

Dashboard de monitoramento em tempo real com metricas, alertas configuraveis, paineis interativos e SignalR.

![build](https://img.shields.io/badge/build-passing-brightgreen)
![tests](https://img.shields.io/badge/tests-48%20passed-brightgreen)
![license](https://img.shields.io/badge/license-MIT-blue)
![dotnet](https://img.shields.io/badge/.NET-10.0-purple)

## Sobre

RVM.LiveBoard e um dashboard de monitoramento em tempo real que permite ingestao de metricas, visualizacao em paineis configuraveis (LineChart, BarChart, Gauge, Counter, Table, Heatmap), regras de alerta com condicoes (gt, gte, lt, lte, eq), severidades (Info, Warning, Critical) e agregacao de dados (Last, Average, Sum, Min, Max, Count). Utiliza SignalR para push de metricas e alertas ao vivo.

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 |
| Web Framework | ASP.NET Core 10 |
| Real-time | SignalR |
| ORM | Entity Framework Core 10 |
| Banco de Dados | PostgreSQL |
| Driver | Npgsql 10.0.1 |
| Logging | Serilog |
| Testes | xUnit 2.9, Moq 4.20 |
| Containerizacao | Docker Compose |

## Arquitetura

```
+-----------------+       +------------------+       +-------------------+
|   Client /      | SignalR|   RVM.LiveBoard  |  EF   |    PostgreSQL     |
|   Frontend      |<------>|      .API        |<----->|                   |
|   (qualquer)    | REST   |                  |       |   rvmliveboard    |
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

## Estrutura do Projeto

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

## Como Executar

### Pre-requisitos

- .NET SDK 10.0
- PostgreSQL (ou Docker)

### Configuracao

1. Clone o repositorio:

```bash
git clone https://github.com/seu-usuario/RVM.LiveBoard.git
cd RVM.LiveBoard
```

2. Configure a connection string em `appsettings.json` ou via variavel de ambiente:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=rvmliveboard;Username=postgres;Password=sua_senha"
```

3. Execute a aplicacao (as migrations rodam automaticamente na inicializacao):

```bash
dotnet run --project src/RVM.LiveBoard.API
```

### Docker Compose

```bash
docker compose -f docker-compose.dev.yml up -d --build
```

## Endpoints da API

Todos os endpoints requerem autenticacao via API Key (header `Authorization`), exceto `/health` e o hub SignalR.

### Metricas

| Metodo | Rota | Descricao |
|---|---|---|
| `POST` | `/api/metrics/ingest` | Ingerir lote de metricas |
| `GET` | `/api/metrics/{metricName}` | Consultar metricas por nome |
| `GET` | `/api/metrics/names` | Listar nomes de metricas distintos |
| `GET` | `/api/metrics/panel/{panelId}/data` | Dados agregados de um painel |

### Dashboards

| Metodo | Rota | Descricao |
|---|---|---|
| `GET` | `/api/dashboards` | Listar todos os dashboards |
| `GET` | `/api/dashboards/{id}` | Obter dashboard com paineis |
| `POST` | `/api/dashboards` | Criar dashboard |
| `PUT` | `/api/dashboards/{id}` | Atualizar dashboard |
| `DELETE` | `/api/dashboards/{id}` | Remover dashboard |
| `POST` | `/api/dashboards/{id}/panels` | Adicionar painel ao dashboard |
| `PUT` | `/api/dashboards/panels/{panelId}` | Atualizar painel |
| `DELETE` | `/api/dashboards/panels/{panelId}` | Remover painel |

### Alertas

| Metodo | Rota | Descricao |
|---|---|---|
| `GET` | `/api/alerts` | Listar alertas (filtro por status) |
| `POST` | `/api/alerts/{id}/acknowledge` | Acknowledge de alerta |
| `POST` | `/api/alerts/{id}/resolve` | Resolver alerta |
| `GET` | `/api/alerts/rules` | Listar regras de alerta |
| `GET` | `/api/alerts/rules/{id}` | Obter regra de alerta |
| `POST` | `/api/alerts/rules` | Criar regra de alerta |
| `PUT` | `/api/alerts/rules/{id}` | Atualizar regra de alerta |
| `DELETE` | `/api/alerts/rules/{id}` | Remover regra de alerta |

### SignalR

| Hub | Rota | Metodos |
|---|---|---|
| `LiveMetricHub` | `/hubs/live-metrics` | `JoinDashboard`, `LeaveDashboard`, `SubscribeMetric`, `UnsubscribeMetric` |

Eventos push: `MetricReceived`, `AlertFired`

### Health Check

| Metodo | Rota | Descricao |
|---|---|---|
| `GET` | `/health` | Health check (inclui verificacao do banco) |

## Testes

O projeto possui **48 testes** cobrindo dominio, infraestrutura e servicos.

```bash
dotnet test test/RVM.LiveBoard.Test
```

| Arquivo | Testes | Tipo |
|---|---|---|
| `EntityTests.cs` | 18 | Dominio (entidades, enums, lifecycle) |
| `MetricDataPointRepositoryTests.cs` | 13 | Infraestrutura (CRUD, agregacao, cleanup) |
| `AlertRepositoryTests.cs` | 6 | Infraestrutura (regras, alertas, status) |
| `DashboardRepositoryTests.cs` | 6 | Infraestrutura (CRUD, paineis, ordenacao) |
| `MetricIngestionServiceTests.cs` | 5 | Servico (ingestao, SignalR push, timestamps) |
| **Total** | **48** | |

## Funcionalidades

- **Dashboards configuraveis**: paineis organizados em grid com posicao e tamanho customizaveis
- **6 tipos de painel**: LineChart, BarChart, Gauge, Counter, Table, Heatmap
- **Ingestao de metricas**: endpoint de lote com nome, valor, unidade, source e tags
- **Agregacao de dados**: Last, Average, Sum, Min, Max, Count por janela de tempo
- **Regras de alerta**: condicoes gt, gte, lt, lte, eq com severidade Info, Warning, Critical
- **AlertEvaluationWorker**: avalia regras a cada 30s e dispara alertas automaticamente
- **MetricCleanupWorker**: limpa metricas expiradas (retencao de 7 dias, ciclo de 60min)
- **SignalR Hub com grupos**: push de metricas por grupo de metrica e broadcast de alertas
- **Acknowledge e Resolve**: ciclo de vida de alertas (Active -> Acknowledged -> Resolved)
- **Autenticacao por API Key**: esquema de autenticacao com keys configuradas
- **Rate Limiting**: 60 requests/minuto por IP
- **Correlation ID**: rastreamento de requisicoes via middleware
- **Health Check**: endpoint com verificacao do banco de dados
- **Auto-migration**: migrations executadas automaticamente na inicializacao

---

<p align="center">
  <strong>RVM.LiveBoard</strong> &mdash; Parte do ecossistema <a href="https://github.com/seu-usuario">RVM Tech</a>
</p>
