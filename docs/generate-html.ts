/**
 * RVM.LiveBoard — Gerador de Manual HTML
 *
 * Le os screenshots gerados pelo Playwright e produz um manual HTML standalone.
 *
 * Uso:
 *   cd docs && npx tsx generate-html.ts
 *
 * Saida:
 *   docs/manual-usuario.html
 *   docs/manual-usuario.md
 */
import fs from 'fs';
import path from 'path';

const SCREENSHOTS_DIR = path.resolve(__dirname, 'screenshots');
const OUTPUT_HTML = path.resolve(__dirname, 'manual-usuario.html');
const OUTPUT_MD = path.resolve(__dirname, 'manual-usuario.md');

interface Section {
  id: string;
  title: string;
  description: string;
  screenshot: string;
  features: string[];
  tips?: string[];
}

const sections: Section[] = [
  {
    id: 'dashboard',
    title: '1. Dashboard Principal',
    description:
      'Painel de monitoramento em tempo real com metricas de negocio, infraestrutura e performance. ' +
      'Todos os dados sao atualizados automaticamente via SignalR sem necessidade de reload.',
    screenshot: '01-dashboard',
    features: [
      'Widgets de metricas em tempo real (numeros, graficos, gauges)',
      'Atualizacao automatica via SignalR (WebSocket)',
      'Filtro por periodo: ultimos 5min, 15min, 1h, 6h, 24h',
      'Exportacao de snapshot (PNG ou PDF)',
      'Modo fullscreen para TVs e monitores de operacao',
      'Tema claro e escuro',
    ],
    tips: [
      'Pressione F11 para modo fullscreen — ideal para TVs de NOC e sala de operacoes.',
      'Use o filtro de periodo para comparar metricas em diferentes janelas de tempo.',
    ],
  },
  {
    id: 'dashboards',
    title: '2. Dashboards Personalizados',
    description:
      'Criacao e gerenciamento de dashboards customizados. ' +
      'Cada dashboard pode ter um conjunto diferente de widgets, ' +
      'fontes de dados e layout de grade.',
    screenshot: '02-dashboards',
    features: [
      'Criacao de dashboards com nome e descricao',
      'Layout de grade configuravel (colunas e linhas)',
      'Adicionar widgets: linha, area, barra, gauge, numero, tabela',
      'Configurar fonte de dados por widget (Redis, PostgreSQL, API)',
      'Duplicar e excluir dashboards',
      'Compartilhar dashboard por URL publica',
    ],
    tips: [
      'Crie dashboards separados por time ou dominio (infraestrutura, negocio, produto).',
      'URLs publicas permitem compartilhar o estado atual sem exigir login.',
    ],
  },
  {
    id: 'alerts',
    title: '3. Alertas',
    description:
      'Sistema de alertas baseado em thresholds de metricas. ' +
      'Alertas sao disparados quando uma metrica ultrapassa o limite configurado ' +
      'e resolvidos automaticamente quando volta ao normal.',
    screenshot: '03-alerts',
    features: [
      'Alertas ativos e historico de alertas resolvidos',
      'Severidades: INFO, WARNING, CRITICAL',
      'Configuracao de threshold por metrica',
      'Canais de notificacao: e-mail e webhook',
      'Silenciar alertas por periodo (manutencao)',
      'Anotacoes de contexto por alerta',
    ],
    tips: [
      'Use o modo "silenciar" durante janelas de manutencao para evitar falsos positivos.',
      'Alertas CRITICAL devem ter canal de notificacao imediata (webhook Slack/Teams).',
    ],
  },
  {
    id: 'metrics',
    title: '4. API de Metricas',
    description:
      'Endpoint REST para ingestao e consulta de metricas. ' +
      'Servicos externos enviam metricas via POST e o dashboard ' +
      'as visualiza em tempo real.',
    screenshot: '04-metrics',
    features: [
      'POST /api/metrics — ingestao de metricas com timestamp e tags',
      'GET /api/metrics — consulta com filtros de nome, periodo e tags',
      'Suporte a tipos: counter, gauge, histogram',
      'Autenticacao via API Key',
      'Rate limiting: 1000 req/min por chave',
    ],
    tips: [
      'Use tags para segmentar metricas por ambiente (prod, staging) ou regiao.',
    ],
  },
  {
    id: 'settings',
    title: '5. Configuracoes',
    description:
      'Configuracoes globais do LiveBoard: intervalos de retencao de dados, ' +
      'canais de notificacao padrao e fontes de dados externas.',
    screenshot: '05-settings',
    features: [
      'Retencao de dados configuravel (7, 30, 90 dias)',
      'Fontes de dados: Redis, PostgreSQL, Prometheus, API customizada',
      'Canais de notificacao: e-mail (SMTP) e webhook',
      'API Keys para integracao de servicos externos',
      'Fuso horario do dashboard',
    ],
  },
];

// ---------------------------------------------------------------------------
// Utilitarios
// ---------------------------------------------------------------------------
function imageToBase64(filePath: string): string | null {
  if (!fs.existsSync(filePath)) return null;
  const buffer = fs.readFileSync(filePath);
  return `data:image/png;base64,${buffer.toString('base64')}`;
}

function generateHTML(): string {
  const now = new Date().toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });

  let sectionsHtml = '';
  for (const s of sections) {
    const desktopPath = path.join(SCREENSHOTS_DIR, `${s.screenshot}--desktop.png`);
    const mobilePath = path.join(SCREENSHOTS_DIR, `${s.screenshot}--mobile.png`);
    const desktopImg = imageToBase64(desktopPath);
    const mobileImg = imageToBase64(mobilePath);

    const featuresHtml = s.features.map((f) => `<li>${f}</li>`).join('\n            ');
    const tipsHtml = s.tips
      ? `<div class="tips">
          <strong>Dicas:</strong>
          <ul>${s.tips.map((t) => `<li>${t}</li>`).join('\n            ')}</ul>
        </div>`
      : '';

    const screenshotsHtml = desktopImg
      ? `<div class="screenshots">
          <div class="screenshot-group">
            <span class="badge">Desktop</span>
            <img src="${desktopImg}" alt="${s.title} - Desktop" />
          </div>
          ${
            mobileImg
              ? `<div class="screenshot-group mobile">
              <span class="badge">Mobile</span>
              <img src="${mobileImg}" alt="${s.title} - Mobile" />
            </div>`
              : ''
          }
        </div>`
      : '<p class="no-screenshot"><em>Screenshot nao disponivel. Execute o script Playwright para gerar.</em></p>';

    sectionsHtml += `
    <section id="${s.id}">
      <h2>${s.title}</h2>
      <p class="description">${s.description}</p>
      <div class="features">
        <strong>Funcionalidades:</strong>
        <ul>
            ${featuresHtml}
        </ul>
      </div>
      ${tipsHtml}
      ${screenshotsHtml}
    </section>`;
  }

  return `<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>RVM.LiveBoard - Manual do Usuario</title>
  <style>
    :root { --primary: #7c3aed; --surface: #ffffff; --bg: #f4f6fa; --text: #1e293b; --text-muted: #64748b; --border: #e2e8f0; --sidebar-bg: #0f172a; --accent: #8b5cf6; }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: var(--bg); color: var(--text); line-height: 1.6; }
    .container { max-width: 1100px; margin: 0 auto; padding: 2rem 1.5rem; }
    header { background: var(--sidebar-bg); color: white; padding: 3rem 1.5rem; text-align: center; }
    header h1 { font-size: 2rem; margin-bottom: 0.5rem; }
    header p { color: #94a3b8; font-size: 1rem; }
    header .version { color: #64748b; font-size: 0.85rem; margin-top: 0.5rem; }
    nav { background: var(--surface); border-bottom: 1px solid var(--border); padding: 1rem 1.5rem; position: sticky; top: 0; z-index: 100; }
    nav .container { padding: 0; }
    nav ul { list-style: none; display: flex; flex-wrap: wrap; gap: 0.5rem; }
    nav a { display: inline-block; padding: 0.35rem 0.75rem; border-radius: 0.5rem; font-size: 0.85rem; color: var(--text); text-decoration: none; background: var(--bg); transition: background 0.2s; }
    nav a:hover { background: var(--primary); color: white; }
    section { background: var(--surface); border: 1px solid var(--border); border-radius: 1rem; padding: 2rem; margin-bottom: 2rem; }
    section h2 { font-size: 1.5rem; color: var(--primary); margin-bottom: 1rem; padding-bottom: 0.5rem; border-bottom: 2px solid var(--border); }
    .description { font-size: 1.05rem; margin-bottom: 1.25rem; color: var(--text); }
    .features, .tips { background: var(--bg); border-radius: 0.75rem; padding: 1rem 1.25rem; margin-bottom: 1.25rem; }
    .features ul, .tips ul { margin-top: 0.5rem; padding-left: 1.25rem; }
    .features li, .tips li { margin-bottom: 0.35rem; }
    .tips { background: #faf5ff; border-left: 4px solid var(--accent); }
    .tips strong { color: var(--primary); }
    .screenshots { display: flex; gap: 1.5rem; margin-top: 1rem; align-items: flex-start; }
    .screenshot-group { position: relative; flex: 1; border: 1px solid var(--border); border-radius: 0.75rem; overflow: hidden; }
    .screenshot-group.mobile { flex: 0 0 200px; max-width: 200px; }
    .screenshot-group img { width: 100%; display: block; }
    .badge { position: absolute; top: 0.5rem; right: 0.5rem; background: var(--sidebar-bg); color: white; font-size: 0.7rem; padding: 0.2rem 0.5rem; border-radius: 0.35rem; font-weight: 600; text-transform: uppercase; }
    .no-screenshot { background: var(--bg); padding: 2rem; border-radius: 0.75rem; text-align: center; color: var(--text-muted); }
    footer { text-align: center; padding: 2rem 1rem; color: var(--text-muted); font-size: 0.85rem; }
    @media (max-width: 768px) { .screenshots { flex-direction: column; } .screenshot-group.mobile { max-width: 100%; flex: 1; } section { padding: 1.25rem; } }
    @media print { nav { display: none; } section { break-inside: avoid; page-break-inside: avoid; } .screenshots { flex-direction: column; } .screenshot-group.mobile { max-width: 250px; } }
  </style>
</head>
<body>
  <header>
    <h1>RVM.LiveBoard - Manual do Usuario</h1>
    <p>Dashboard Real-Time com SignalR e Redis — Guia Completo de Funcionalidades</p>
    <div class="version">Gerado em ${now} | RVM Tech</div>
  </header>

  <nav>
    <div class="container">
      <ul>
        ${sections.map((s) => `<li><a href="#${s.id}">${s.title}</a></li>`).join('\n        ')}
      </ul>
    </div>
  </nav>

  <div class="container">
    <section id="visao-geral">
      <h2>Visao Geral</h2>
      <p class="description">
        O <strong>RVM.LiveBoard</strong> e um sistema de dashboard em tempo real para monitoramento
        de metricas de negocio e infraestrutura. Utiliza SignalR para atualizacoes instantaneas
        e Redis para cache de alta performance.
      </p>
      <div class="features">
        <strong>Recursos principais:</strong>
        <ul>
          <li><strong>Dashboard real-time</strong> — atualizacoes via SignalR sem reload</li>
          <li><strong>Dashboards personalizados</strong> — widgets configurados por usuario</li>
          <li><strong>Alertas por threshold</strong> — notificacoes automaticas ao ultrapassar limites</li>
          <li><strong>API de ingestao</strong> — endpoints REST para envio de metricas</li>
          <li><strong>OpenTelemetry</strong> — traces e metricas exportados para Jaeger/Prometheus</li>
          <li><strong>Redis como buffer</strong> — metricas recentes em memoria para baixa latencia</li>
        </ul>
      </div>
    </section>

    ${sectionsHtml}
  </div>

  <footer>
    <p>RVM Tech &mdash; Dashboard Real-Time</p>
    <p>Documento gerado automaticamente com Playwright + TypeScript</p>
  </footer>
</body>
</html>`;
}

function generateMarkdown(): string {
  const now = new Date().toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });

  let md = `# RVM.LiveBoard - Manual do Usuario

> Dashboard Real-Time com SignalR e Redis — Guia Completo de Funcionalidades
>
> Gerado em ${now} | RVM Tech

---

## Visao Geral

O **RVM.LiveBoard** e um sistema de dashboard em tempo real com SignalR, Redis e OpenTelemetry.

**Recursos principais:**
- **Dashboard real-time** — atualizacoes via SignalR sem reload
- **Dashboards personalizados** — widgets configurados por usuario
- **Alertas por threshold** — notificacoes automaticas
- **API de ingestao** — endpoints REST para envio de metricas
- **OpenTelemetry** — traces e metricas exportados

---

`;

  for (const s of sections) {
    const desktopExists = fs.existsSync(path.join(SCREENSHOTS_DIR, `${s.screenshot}--desktop.png`));

    md += `## ${s.title}\n\n`;
    md += `${s.description}\n\n`;
    md += `**Funcionalidades:**\n`;
    for (const f of s.features) md += `- ${f}\n`;
    md += '\n';

    if (s.tips) {
      md += `> **Dicas:**\n`;
      for (const t of s.tips) md += `> - ${t}\n`;
      md += '\n';
    }

    if (desktopExists) {
      md += `| Desktop | Mobile |\n|---------|--------|\n`;
      md += `| ![${s.title} - Desktop](screenshots/${s.screenshot}--desktop.png) | ![${s.title} - Mobile](screenshots/${s.screenshot}--mobile.png) |\n`;
    } else {
      md += `*Screenshot nao disponivel. Execute o script Playwright para gerar.*\n`;
    }
    md += '\n---\n\n';
  }

  md += `## Informacoes Tecnicas

| Item | Detalhe |
|------|---------|
| **Tecnologia** | ASP.NET Core + Blazor Server |
| **Tempo real** | SignalR (WebSocket) |
| **Cache** | Redis |
| **Observabilidade** | OpenTelemetry (traces + metricas) |
| **Banco de dados** | PostgreSQL 16 |

---

*Documento gerado automaticamente com Playwright + TypeScript — RVM Tech*
`;

  return md;
}

const html = generateHTML();
fs.writeFileSync(OUTPUT_HTML, html, 'utf-8');
console.log(`HTML gerado: ${OUTPUT_HTML}`);

const md = generateMarkdown();
fs.writeFileSync(OUTPUT_MD, md, 'utf-8');
console.log(`Markdown gerado: ${OUTPUT_MD}`);
