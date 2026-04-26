# Testes — RVM.LiveBoard

## Testes Unitarios
- **Framework:** xUnit + Moq
- **Localizacao:** `test/RVM.LiveBoard.Test/`
- **Total:** 101 testes
- **Foco:** MetricIngestionService (validacao), AlertEvaluationWorker (thresholds), MetricCleanupWorker (retencao), logica de dashboard

```bash
dotnet test test/RVM.LiveBoard.Test/
```

## Testes E2E (Playwright)
- **Localizacao:** `test/playwright/`
- **Cobertura:** dashboard em tempo real, configuracao de alertas, widgets, atualizacoes SignalR

```bash
cd test/playwright
npm install
npx playwright install --with-deps
npx playwright test
```

Variaveis de ambiente necessarias:
```
LIVEBOARD_BASE_URL=http://localhost:5000
LIVEBOARD_API_KEY=<api-key-dev>
```

## CI
- **Arquivo:** `.github/workflows/ci.yml`
- Pipeline: build → testes unitarios → Playwright
- Workers desativados em testes via `IHostedService` mock ou `ASPNETCORE_ENVIRONMENT=Test`
