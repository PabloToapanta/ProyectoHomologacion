import { expect, test } from '@playwright/test';
import type { Page } from '@playwright/test';

// Datos explícitamente sintéticos: estas pruebas verifican UI, no la integración real.
const carrera = { id: 'c1', nombre: 'Software sintético', facultad: 'FISEI', modalidad: 'Presencial', vigencia: true };
const asignaturas = [
  { id: 'a1', carreraId: 'c1', codigo: 'DEMO-01', nombre: 'Asignatura sintética I', nivel: 1, creditos: 3, horas: 144 },
  { id: 'a2', carreraId: 'c1', codigo: 'DEMO-02', nombre: 'Asignatura sintética II', nivel: 2, creditos: 3, horas: 144 },
];
async function servicios(page: Page, role = 'Administrador') {
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    const method = route.request().method();
    const json = (body: unknown, status = 200) => route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });
    if (path === '/api/auth/login') {
      const part = (value: unknown) => Buffer.from(JSON.stringify(value)).toString('base64url');
      const exp = Math.floor(Date.now() / 1000) + 3600;
      return json({ accessToken: `${part({ alg: 'HS256' })}.${part({ sub: 'demo', name: 'Usuario sintético', role, exp })}.sintetico`, tokenType: 'Bearer', expiresAtUtc: new Date(exp * 1000).toISOString() });
    }
    if (path === '/api/carreras' && method === 'GET') return json([carrera]);
    if (path === '/api/carreras' && method === 'POST') return json({ detail: 'Ya existe una carrera con ese nombre en la facultad.' }, 409);
    if (path === '/api/carreras/c1' && method === 'DELETE') return json({ detail: 'La carrera tiene asignaturas relacionadas.' }, 409);
    if (path === '/api/carreras/c1/asignaturas') return json(asignaturas);
    if (path === '/api/asignaturas/a1') return json({ ...asignaturas[0], unidades: [] });
    throw new Error(`Petición no prevista: ${method} ${path}`);
  });
  await page.goto('/login');
  await page.getByLabel('Usuario', { exact: true }).fill('demo');
  await page.getByLabel('Contraseña', { exact: true }).fill('clave-sintetica');
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
}
async function sinDesbordamiento(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
}

test('mockups: búsqueda, modales, foco, malla contextual, filtros y unidades', async ({ page }, testInfo) => {
  const errors: string[] = [];
  page.on('pageerror', e => errors.push(e.message));
  await servicios(page);
  await page.getByRole('link', { name: 'Gestionar carreras y asignaturas' }).click();
  await expect(page.getByRole('cell', { name: carrera.nombre, exact: true })).toBeVisible();
  await sinDesbordamiento(page);
  await page.screenshot({ path: testInfo.outputPath('carreras.png'), fullPage: true });
  await page.getByRole('searchbox').fill('sin coincidencias');
  await expect(page.getByText('No hay carreras que coincidan con la búsqueda.')).toBeVisible();
  await page.getByRole('searchbox').clear();
  const nuevo = page.getByRole('button', { name: 'Nueva carrera', exact: true });
  await nuevo.click();
  const dialog = page.getByRole('dialog', { name: 'Nueva carrera' });
  await expect(dialog.getByLabel('Nombre', { exact: true })).toBeFocused();
  await dialog.getByLabel('Nombre', { exact: true }).fill(carrera.nombre);
  await dialog.getByRole('button', { name: 'Guardar carrera' }).click();
  await expect(dialog.getByRole('alert')).toContainText('Ya existe');
  await page.screenshot({ path: testInfo.outputPath('duplicado.png'), fullPage: true });
  for (let i = 0; i < 10; i++) {
    await page.keyboard.press('Tab');
    expect(await dialog.evaluate(el => el.contains(document.activeElement))).toBe(true);
  }
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(nuevo).toBeFocused();
  await page.getByRole('button', { name: `Eliminar ${carrera.nombre}`, exact: true }).click();
  await page.getByRole('button', { name: 'Confirmar eliminación' }).click();
  await expect(page.getByRole('dialog')).toContainText('asignaturas relacionadas');
  await page.getByRole('button', { name: 'Entendido' }).click();
  await page.getByRole('button', { name: `Ver malla de ${carrera.nombre}`, exact: true }).click();
  await expect(page.getByRole('combobox', { name: 'Carrera', exact: true })).toHaveValue('c1');
  await expect(page).toHaveURL(/carrera=c1/);
  await expect(page.getByRole('cell', { name: asignaturas[0].nombre, exact: true })).toBeVisible();
  await page.getByLabel('Filtrar por nivel').selectOption('2');
  await expect(page.getByRole('cell', { name: asignaturas[0].nombre, exact: true })).toHaveCount(0);
  await expect(page.getByRole('cell', { name: asignaturas[1].nombre, exact: true })).toBeVisible();
  await sinDesbordamiento(page);
  await page.screenshot({ path: testInfo.outputPath('malla.png'), fullPage: true });
  await page.getByRole('button', { name: 'Nueva asignatura', exact: true }).click();
  await expect(page.getByLabel('Código', { exact: true })).toBeFocused();
  await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
  await page.getByLabel('Filtrar por nivel').selectOption('');
  await page.getByRole('button', { name: `Ver unidades de ${asignaturas[0].nombre}`, exact: true }).click();
  await expect(page.getByText('No hay unidades registradas.')).toBeVisible();
  await sinDesbordamiento(page);
  await page.goBack();
  await expect(page.getByRole('heading', { name: 'Gestión de carreras' })).toBeVisible();
  await page.getByRole('button', { name: 'Cerrar sesión' }).click();
  await expect(page).toHaveURL(/\/login$/);
  expect(errors).toEqual([]);
});

test('el docente conserva acceso a validaciones sin enlaces administrativos', async ({ page }) => {
  await servicios(page, 'Docente');
  await page.getByRole('link', { name: 'Ir a validaciones académicas' }).click();
  const nav = page.getByRole('navigation', { name: 'Navegación principal' });
  await expect(nav.getByRole('link', { name: 'Carreras', exact: true })).toHaveCount(0);
  await expect(nav.getByRole('link', { name: 'Panel', exact: true })).toHaveCount(0);
  await expect(page.getByLabel('Fecha de evaluación')).toBeVisible();
  await sinDesbordamiento(page);
});
