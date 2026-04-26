/**
 * RVM.LiveBoard — Gerador de Manual Visual
 *
 * Playwright script que navega por todas as telas do sistema de dashboard real-time,
 * captura screenshots em desktop e mobile, e gera as imagens para o manual.
 *
 * Uso:
 *   cd test/playwright
 *   npx playwright test tests/generate-manual.spec.ts --reporter=list
 */
import { test, type Page } from '@playwright/test';
import path from 'path';

const BASE_URL = process.env.LIVEBOARD_BASE_URL ?? 'https://liveboard.lab.rvmtech.com.br';
const SCREENSHOTS_DIR = path.resolve(__dirname, '../../../docs/screenshots');

/** Captura desktop (1280x800) + mobile (390x844) */
async function capture(page: Page, name: string, opts?: { fullPage?: boolean }) {
  const fullPage = opts?.fullPage ?? true;
  await page.screenshot({ path: path.join(SCREENSHOTS_DIR, `${name}--desktop.png`), fullPage });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: path.join(SCREENSHOTS_DIR, `${name}--mobile.png`), fullPage });
  await page.setViewportSize({ width: 1280, height: 800 });
}

test.describe('RVM.LiveBoard — Manual Visual', () => {
  test('01 Dashboard Principal', async ({ page }) => {
    await page.goto(`${BASE_URL}/`);
    await page.waitForLoadState('networkidle');
    await capture(page, '01-dashboard');
  });

  test('02 Dashboards Personalizados', async ({ page }) => {
    await page.goto(`${BASE_URL}/dashboards`);
    await page.waitForLoadState('networkidle');
    await capture(page, '02-dashboards');
  });

  test('03 Alertas', async ({ page }) => {
    await page.goto(`${BASE_URL}/alerts`);
    await page.waitForLoadState('networkidle');
    await capture(page, '03-alerts');
  });

  test('04 Metricas (API)', async ({ page }) => {
    await page.goto(`${BASE_URL}/api/metrics`);
    await page.waitForLoadState('networkidle');
    await capture(page, '04-metrics');
  });

  test('05 Configuracoes (API)', async ({ page }) => {
    await page.goto(`${BASE_URL}/api/settings`);
    await page.waitForLoadState('networkidle');
    await capture(page, '05-settings');
  });
});
