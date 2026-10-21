import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { expect, test, vi } from 'vitest';
import { ValidacionesPage } from './ValidacionesPage';

const { request } = vi.hoisted(() => ({ request: vi.fn() }));
vi.mock('../auth', () => ({ useAuth: () => ({ request, logout: vi.fn(), session: { role: 'Administrador' } }) }));
test('evalúa antigüedad, limpia resultados al cambiar datos y muestra incumplimientos de cambio', async () => {
  request.mockReset();
  request.mockResolvedValueOnce(new Response(JSON.stringify({ estado: 'Elegible', motivo: 'Diez años inclusive.' })));
  render(<MemoryRouter><ValidacionesPage /></MemoryRouter>);
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Fecha de aprobación'), '2016-10-20');
  await user.type(screen.getByLabelText('Fecha de evaluación'), '2026-10-20');
  await user.click(screen.getByRole('button', { name: /^Validar$/ }));
  await screen.findByText('Elegible por aprobación y antigüedad');
  expect(JSON.parse(request.mock.calls[0]![1].body).fechaAprobacion).toBe('2016-10-20');
  await user.selectOptions(screen.getByLabelText('Estado de asignatura'), 'Reprobada');
  expect(screen.queryByText('Diez años inclusive.')).not.toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Requisitos de cambio' }));
  await user.selectOptions(screen.getByLabelText('Tipo de cambio'), 'Externo');
  await user.type(screen.getByLabelText('Períodos cursados'), '2');
  await user.type(screen.getByLabelText('Asignaturas homologables'), '1');
  await user.type(screen.getByLabelText('Cambios previos'), '0');
  request.mockResolvedValueOnce(new Response(JSON.stringify({ cumple: false, incumplimientos: ['Se requieren dos asignaturas.'] })));
  await user.click(screen.getByRole('button', { name: /^Validar$/ }));
  await screen.findByText('No cumple los requisitos evaluados');
  expect(screen.getByText('Se requieren dos asignaturas.')).toBeVisible();
});

test('equivalencia envía inventarios explícitos y presenta información incompleta', async () => {
  request.mockReset();
  render(<MemoryRouter><ValidacionesPage /></MemoryRouter>);
  const user = userEvent.setup();
  await user.click(screen.getByRole('button', { name: 'Equivalencia de asignaturas' }));
  for (const lado of ['origen', 'receptora']) {
    for (const [label, valor] of [['Identificador', lado], ['Referencia al plan analítico', 'plan'], ['Unidad de horas', 'hora-reloj'], ['Sistema de créditos', 'sistema']])
      await user.type(screen.getByLabelText(`${label} (${lado})`), valor!);
    await user.type(screen.getByLabelText(`Horas (${lado})`), '100');
    await user.type(screen.getByLabelText(`Créditos (${lado})`), '3');
    await user.type(screen.getByLabelText(`Contenidos (${lado}, uno por línea)`), lado === 'origen' ? 'Tema A' : 'Tema B');
  }
  await user.selectOptions(screen.getByLabelText('Consta aprobada'), 'si');
  await user.selectOptions(screen.getByLabelText('Elemento de origen para Tema B'), 'Tema A');
  request.mockResolvedValueOnce(new Response(JSON.stringify({ estado: 'InformacionIncompleta', homologable: false, compatibilidadAcademicaPct: null, compatibilidadHorariaPct: 100, compatibilidadCreditosPct: 100, umbralPct: 80, justificacion: 'Falta evidencia.', informacionFaltante: ['Ubicación de evidencia ausente'], contenidosSinCobertura: ['Tema B'], resultadosAprendizajeSinCobertura: [] })));
  await user.click(screen.getByRole('button', { name: 'Evaluar equivalencia' }));
  await screen.findByText('Información incompleta');
  expect(screen.getByText('Sin determinar')).toBeVisible();
  const datos = JSON.parse(request.mock.calls[0]![1].body);
  expect(datos.origenes[0].aprobada).toBe(true);
  expect(datos.correspondenciasContenidos[0].elementoReceptor).toBe('Tema B');
});
