import { expect, test } from '@playwright/test';

async function login(page: import('@playwright/test').Page) {
  await page.getByLabel('Usuario', { exact: true }).fill(process.env.E2E_ADMIN_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Panel de administración' })).toBeVisible();
}

test('React → Gateway → Auth: protección, errores, login, recarga y logout', async ({ page }, testInfo) => {
  const consoleErrors: string[] = [];
  page.on('pageerror', (error) => consoleErrors.push(error.message));
  await page.goto('/panel');
  await expect(page).toHaveURL(/\/login$/);
  await page.screenshot({ path: testInfo.outputPath('login-escritorio.png'), fullPage: true });
  await page.getByLabel('Usuario', { exact: true }).fill(process.env.E2E_ADMIN_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill('incorrecta-sintetica');
  const rejectedLogin = page.waitForResponse((response) => response.url().endsWith('/api/auth/login') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  const rejection = await rejectedLogin;
  expect(rejection.status()).toBe(401);
  const apiRejection = await page.request.post('/api/auth/login', { data: { nombreUsuario: process.env.E2E_ADMIN_USERNAME!, password: 'incorrecta-sintetica' } });
  expect(apiRejection.status()).toBe(401);
  expect(await apiRejection.json()).not.toHaveProperty('accessToken');
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expect(page.getByLabel('Contraseña', { exact: true })).toHaveValue('');
  await login(page);
  await expect(page).toHaveURL(/\/panel$/);
  await expect(page.getByText(process.env.E2E_ADMIN_USERNAME!, { exact: true })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('panel-escritorio.png'), fullPage: true });
  expect(await page.evaluate(() => [localStorage.length, sessionStorage.length])).toEqual([0, 0]);
  await page.reload();
  await expect(page).toHaveURL(/\/login$/);
  await login(page);
  await page.getByRole('button', { name: 'Cerrar sesión' }).click();
  await expect(page).toHaveURL(/\/login$/);
  await page.goBack();
  await expect(page.getByRole('heading', { name: 'Panel de administración' })).not.toBeVisible();
  await page.goto('/panel');
  await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
  expect((await page.request.get('/api/carreras')).status()).toBe(401);
  expect(consoleErrors).toEqual([]);
});

test('login y panel utilizables en móvil sin desbordamiento horizontal', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/login');
  await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('login-movil.png'), fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await login(page);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: testInfo.outputPath('panel-movil.png'), fullPage: true });
  await page.getByRole('button', { name: 'Cerrar sesión' }).click();
  await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
});
