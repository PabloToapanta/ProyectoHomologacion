import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e-mockups',
  outputDir: './test-results/mockups',
  workers: 1,
  retries: 0,
  reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:5179', trace: 'off', screenshot: 'only-on-failure' },
  webServer: { command: 'npm run dev -- --port 5179', url: 'http://127.0.0.1:5179', reuseExistingServer: false },
  projects: [
    { name: 'escritorio', use: { browserName: 'chromium', viewport: { width: 1365, height: 900 } } },
    { name: 'movil', use: { browserName: 'chromium', viewport: { width: 390, height: 844 } } },
    { name: 'compacto', use: { browserName: 'chromium', viewport: { width: 320, height: 740 } } },
  ],
});
