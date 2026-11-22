import { expect, test, type Browser, type Page } from '@playwright/test';

const env = (name: string) => {
  const value = process.env[name];
  if (!value) throw new Error(`Falta variable ${name}`);
  return value;
};

const baseURL = env('E2E_BASE_URL');
const runId = env('E2E_RUN_ID');

async function login(browser: Browser, username: string, password: string): Promise<Page> {
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto(baseURL);
  await page.getByLabel('Usuario', { exact: true }).fill(username);
  await page.getByLabel('Contraseña', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  await expect(page).not.toHaveURL(/\/login/);
  return page;
}

async function close(page: Page) {
  await page.context().close();
}

test('HU-27: dos docentes revisan, remiten y coordinación finaliza con estado persistente', async ({ browser }) => {
  test.setTimeout(180_000);
  const coordinator = await login(browser, env('E2E_COORD_USERNAME'), env('E2E_COORD_PASSWORD'));
  await coordinator.goto(`${baseURL}/solicitudes`);
  await coordinator.getByRole('button', { name: 'Nueva solicitud' }).click();
  const solicitudDialog = coordinator.getByRole('dialog', { name: 'Registrar solicitud' });
  const numero = `HU27-${runId}`;
  await solicitudDialog.getByLabel('Número de trámite Quipux').fill(numero);
  await solicitudDialog.getByLabel('Identificación del estudiante').fill(`SYN-${runId}`);
  await solicitudDialog.getByLabel('Nombre del estudiante').fill('Estudiante sintético HU-27');
  await solicitudDialog.getByLabel('Tipo de cambio').selectOption('Interno');
  await solicitudDialog.getByLabel('Carrera de origen').selectOption(env('E2E_CARRERA_ORIGEN_ID'));
  await solicitudDialog.getByLabel('Carrera receptora').selectOption(env('E2E_CARRERA_RECEPTORA_ID'));
  await solicitudDialog.getByRole('button', { name: 'Registrar solicitud' }).click();
  await expect(coordinator.getByRole('status')).toContainText('Solicitud registrada correctamente');
  await coordinator.getByRole('button', { name: `Ver ${numero}` }).click();
  const detail = coordinator.getByRole('dialog', { name: `Solicitud ${numero}` });

  await detail.getByRole('button', { name: 'Agregar asignatura aprobada' }).click();
  await detail.getByLabel('Asignatura de origen').selectOption(env('E2E_ASIGNATURA_ORIGEN_ID'));
  await detail.getByLabel('Nota').fill('9');
  await detail.getByLabel('Fecha de aprobación').fill('2025-06-01');
  await detail.getByLabel('Número de matrícula').fill(`MAT-${runId}`);
  await detail.getByLabel('Intento de matrícula').selectOption('1');
  await detail.getByRole('button', { name: 'Registrar asignatura' }).click();
  await expect(detail.getByRole('status')).toContainText('Asignatura registrada');

  await detail.getByRole('button', { name: 'Asignar docentes' }).click();
  await detail.getByLabel('Primer docente').selectOption({ label: env('E2E_DOC1_USERNAME') });
  await detail.getByLabel('Segundo docente').selectOption({ label: env('E2E_DOC2_USERNAME') });
  await detail.getByRole('button', { name: 'Guardar asignación' }).click();
  await expect(detail.getByRole('status')).toContainText('Docentes asignados correctamente');
  await close(coordinator);

  for (const username of [env('E2E_DOC1_USERNAME'), env('E2E_DOC2_USERNAME')]) {
    const password = username === env('E2E_DOC1_USERNAME') ? env('E2E_DOC1_PASSWORD') : env('E2E_DOC2_PASSWORD');
    const teacher = await login(browser, username, password);
    await teacher.goto(`${baseURL}/docente/solicitudes`);
    await teacher.getByRole('button', { name: `Ver ${numero}` }).click();
    const assigned = teacher.getByRole('dialog', { name: `Solicitud ${numero}` });
    await assigned.getByRole('button', { name: 'Consultar análisis interno' }).click();
    await assigned.getByLabel('Asignatura aprobada').selectOption({ label: /Asignatura sintética O/ });
    await assigned.getByLabel('Asignatura receptora').selectOption({ label: /Asignatura sintética R/ });
    await assigned.getByRole('button', { name: 'Ejecutar análisis y guardar' }).click();
    await expect(assigned.getByRole('status')).toContainText('Análisis guardado');
    await expect(assigned.getByRole('heading', { name: 'Resultado del análisis automatizado' })).toBeVisible();
    await assigned.getByLabel('De acuerdo con el agente').check();
    await assigned.getByRole('button', { name: 'Guardar mi análisis' }).click();
    await expect(assigned.getByRole('status')).toContainText('Su análisis quedó registrado');
    await teacher.getByRole('button', { name: 'Cerrar detalle' }).click();
    await close(teacher);
  }

  const reporter = await login(browser, env('E2E_DOC1_USERNAME'), env('E2E_DOC1_PASSWORD'));
  await reporter.goto(`${baseURL}/docente/solicitudes`);
  await reporter.getByRole('button', { name: `Ver ${numero}` }).click();
  const reportDetail = reporter.getByRole('dialog', { name: `Solicitud ${numero}` });
  const report = reportDetail.getByRole('region', { name: 'Informe de homologación' });
  await report.getByRole('button', { name: 'Generar informe' }).click();
  const reportTable = report.getByRole('table', { name: 'Informe de homologación por pareja' });
  await expect(reportTable).toBeVisible();
  await expect(reportTable.getByText(/doc1\.hu27/)).toBeVisible();
  await expect(reportTable.getByText(/doc2\.hu27/)).toBeVisible();
  await report.getByRole('button', { name: 'Remitir al coordinador' }).click();
  await expect(report.getByRole('status')).toContainText('Informe remitido al coordinador');
  await close(reporter);

  const finalCoordinator = await login(browser, env('E2E_COORD_USERNAME'), env('E2E_COORD_PASSWORD'));
  await finalCoordinator.goto(`${baseURL}/solicitudes`);
  await finalCoordinator.getByRole('button', { name: `Ver ${numero}` }).click();
  const finalDetail = finalCoordinator.getByRole('dialog', { name: `Solicitud ${numero}` });
  await expect(finalDetail.getByRole('heading', { name: 'Revisión del informe y finalización' })).toBeVisible();
  await finalDetail.getByRole('button', { name: 'Finalizar solicitud' }).click();
  await expect(finalDetail.getByRole('status')).toContainText('Solicitud finalizada');
  await finalCoordinator.getByRole('button', { name: 'Cerrar detalle' }).click();
  await close(finalCoordinator);

  const verifier = await login(browser, env('E2E_COORD_USERNAME'), env('E2E_COORD_PASSWORD'));
  await verifier.goto(`${baseURL}/solicitudes`);
  await verifier.getByRole('button', { name: `Ver ${numero}` }).click();
  const reopened = verifier.getByRole('dialog', { name: `Solicitud ${numero}` });
  await expect(reopened.getByText('Finalizada', { exact: true })).toBeVisible();
  await expect(reopened.getByRole('button', { name: 'Finalizar solicitud' })).toHaveCount(0);
  await close(verifier);
});

