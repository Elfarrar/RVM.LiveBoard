import { expect, test } from '@playwright/test';

const defaultBaseUrl = process.env.LIVEBOARD_BASE_URL ?? 'https://liveboard.lab.rvmtech.com.br';

test.describe('LiveBoard — API Dashboards', () => {
  test.skip(
    process.env.LIVEBOARD_RUN_SMOKE !== '1',
    'Defina LIVEBOARD_RUN_SMOKE=1 para rodar o smoke contra um ambiente real.',
  );

  test('GET /api/dashboards — retorna lista ou exige autenticação', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.get(`${currentBaseUrl}/api/dashboards`);
    expect([200, 401]).toContain(response.status());
  });

  test('GET /api/metrics — retorna lista ou exige autenticação', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.get(`${currentBaseUrl}/api/metrics`);
    expect([200, 401]).toContain(response.status());
  });

  test('GET /api/alerts — retorna lista ou exige autenticação', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.get(`${currentBaseUrl}/api/alerts`);
    expect([200, 401]).toContain(response.status());
  });

  test('POST /api/dashboards sem body — retorna 400 ou 401', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.post(`${currentBaseUrl}/api/dashboards`);
    expect([400, 401]).toContain(response.status());
  });

  test('POST /api/alerts sem body — retorna 400 ou 401', async ({ request, baseURL }) => {
    const currentBaseUrl = baseURL ?? defaultBaseUrl;
    const response = await request.post(`${currentBaseUrl}/api/alerts`);
    expect([400, 401]).toContain(response.status());
  });
});
