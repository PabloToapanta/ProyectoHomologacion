import { defineConfig, devices } from '@playwright/test';

if (!process.env.E2E_ADMIN_USERNAME || !process.env.E2E_ADMIN_PASSWORD) {
  throw new Error('Las pruebas E2E requieren E2E_ADMIN_USERNAME y E2E_ADMIN_PASSWORD por entorno, con el stack local iniciado.');
}
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: process.env.E2E_BASE_URL || 'http://127.0.0.1:5173',
    trace: 'off',
    screenshot: 'off',
    video: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], viewport: { width: 1365, height: 900 } } }],
});
