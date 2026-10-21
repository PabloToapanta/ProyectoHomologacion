import { expect, test } from '@playwright/test';

// Solo lee el catálogo DEMO cargado explícitamente por scripts/cargar-demo-equivalencias.py.
test.use({ channel: process.env.E2E_BROWSER_CHANNEL });
test('comparador simple con datos persistidos: suficiente, insuficiente e incompleto', async ({ page }, testInfo) => {
  const errores: string[] = []; page.on('pageerror', e => errores.push(e.message));
  await page.goto('/login');
  await page.getByLabel('Usuario', { exact: true }).fill(process.env.E2E_ADMIN_USERNAME!);
  await page.getByLabel('Contraseña', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  await page.getByRole('link', { name: 'Abrir validaciones', exact: true }).click();
  await page.getByRole('button', { name: 'Equivalencia de asignaturas', exact: true }).click();
  const origen = page.getByLabel('Asignatura origen', { exact: true }), receptora = page.getByLabel('Asignatura receptora', { exact: true });
  await expect(origen).toBeEnabled();
  async function seleccionar(selector: typeof origen, codigo: string) {
    const opcion = selector.locator('option').filter({ hasText: new RegExp(`^${codigo} ·`) });
    await selector.selectOption((await opcion.getAttribute('value'))!);
  }
  await seleccionar(origen, 'DEMO-SW-ALG'); await seleccionar(receptora, 'DEMO-ROB-ALG');
  await page.getByRole('button', { name: 'Comparar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Se sugiere homologable' })).toBeVisible();
  await expect(page.getByRole('progressbar')).toHaveCount(3);
  await expect(page.getByRole('combobox')).toHaveCount(2);
  await expect(page.getByLabel('Unidad de horas', { exact: true })).toHaveCount(0);
  await page.screenshot({ path: testInfo.outputPath('comparador-escritorio.png'), fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await seleccionar(receptora, 'DEMO-TI-ALG');
  await page.getByRole('button', { name: 'Comparar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'No alcanza la equivalencia requerida' })).toBeVisible();
  await expect(page.getByRole('row', { name: /Académica combinada/ })).toContainText('75 %');
  await page.setViewportSize({ width: 390, height: 844 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: testInfo.outputPath('comparador-movil.png'), fullPage: true });
  await seleccionar(origen, 'DEMO-ROB-ESP'); await seleccionar(receptora, 'DEMO-SW-PROG');
  await page.getByRole('button', { name: 'Comparar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Información incompleta', exact: true })).toBeVisible();
  await expect(page.getByText(/Revisa las correspondencias/)).toBeVisible();
  expect(errores).toEqual([]);
});
