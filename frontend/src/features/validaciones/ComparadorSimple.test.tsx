import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import { EquivalenciaForm } from './EquivalenciaForm';
const materia = (id: string) => ({ id, carreraId: 'carrera', codigo: id, nombre: `Materia ${id}`, nivel: 1, horas: 144, creditos: 3,
  unidades: [{ id: `u-${id}`, numero: 1, resultadoAprendizaje: `Resultado ${id}`, contenidos: [{ id: `c-${id}`, tema: `Tema ${id}` }] }] });
function catalogo({ vigente = true, registrada = true } = {}) {
  return vi.fn(async (path: string, _init?: RequestInit): Promise<Response> => {
    if (path === '/api/carreras') return Response.json([{ id: 'carrera', nombre: 'Software', facultad: 'FISEI', vigencia: true }]);
    if (path.endsWith('/asignaturas')) return Response.json([materia('O'), materia('R')]);
    if (path.endsWith('/perfil-comparacion')) return Response.json({ planAnalitico: 'Plan DEMO', unidadHoras: 'hora-reloj', sistemaCreditos: 'DEMO', esSintetico: true });
    if (path.includes('/correspondencias/')) return Response.json({ registrada, vigente, contenidos: [{ elementoOrigen: 'c-O', elementoReceptor: 'c-R', ubicacionOrigen: 'U1', ubicacionReceptora: 'U1' }], resultados: [] });
    if (path.startsWith('/api/asignaturas/')) return Response.json(materia(path.split('/').at(-1)!));
    return Response.json({ estado: vigente && registrada ? 'Homologable' : 'InformacionIncompleta', compatibilidadAcademicaPct: vigente && registrada ? 100 : null, compatibilidadHorariaPct: 100, compatibilidadCreditosPct: 100, umbralPct: 80, justificacion: 'Evaluación de prueba', informacionFaltante: [], contenidosSinCobertura: [], resultadosAprendizajeSinCobertura: [] });
  });
}
async function elegir(user: ReturnType<typeof userEvent.setup>) {
  await screen.findAllByRole('option', { name: /O · Materia O/ });
  await user.selectOptions(screen.getByLabelText('Asignatura origen'), 'O');
  await user.selectOptions(screen.getByLabelText('Asignatura receptora'), 'R');
}
test('dos selectores comparan usando perfiles y correspondencias del catálogo', async () => {
  const request = catalogo(), user = userEvent.setup(); render(<EquivalenciaForm request={request} />);
  await elegir(user);
  expect(screen.queryByLabelText(/Unidad de horas/)).not.toBeInTheDocument();
  expect(screen.queryByLabelText(/Referencia al plan/)).not.toBeInTheDocument();
  expect(screen.getAllByRole('combobox')).toHaveLength(2);
  await user.click(screen.getByRole('button', { name: 'Comparar' }));
  await screen.findByText('Se sugiere homologable');
  const datos = JSON.parse(request.mock.calls.find(([p]) => p === '/api/homologaciones/evaluar')![1]!.body as string);
  expect(datos.origenes[0]).toMatchObject({ id: 'O', horas: 144, unidadHoras: 'hora-reloj', creditos: 3, sistemaCreditos: 'DEMO', planAnalitico: 'Plan DEMO', contenidos: ['c-O'], resultadosAprendizaje: ['u-O'], aprobada: true });
  expect(datos.correspondenciasContenidos[0]).toMatchObject({ asignaturaOrigen: 'O', elementoOrigen: 'c-O', elementoReceptor: 'c-R', evidencia: { ubicacionOrigen: 'U1', ubicacionReceptora: 'U1' } });
  expect(screen.getAllByRole('progressbar')).toHaveLength(3);
  await user.selectOptions(screen.getByLabelText('Asignatura origen'), '');
  expect(screen.queryByText('Se sugiere homologable')).not.toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Comparar' })).toBeDisabled();
});
test.each([{ registrada: false, vigente: false }, { registrada: true, vigente: false }])('sin revisión vigente no inventa correspondencias: %o', async flags => {
  const request = catalogo(flags), user = userEvent.setup(); render(<EquivalenciaForm request={request} />);
  await elegir(user); await user.click(screen.getByRole('button', { name: 'Comparar' }));
  await screen.findByText('Información incompleta');
  const datos = JSON.parse(request.mock.calls.find(([p]) => p === '/api/homologaciones/evaluar')![1]!.body as string);
  expect(datos.correspondenciasContenidos).toBeNull();
  expect(screen.getByText(/Revisa las correspondencias/)).toBeVisible();
});
test('impide comparar una materia consigo misma', async () => {
  const request = catalogo(), user = userEvent.setup(); render(<EquivalenciaForm request={request} />);
  await elegir(user); await user.selectOptions(screen.getByLabelText('Asignatura receptora'), 'O');
  await user.click(screen.getByRole('button', { name: 'Comparar' }));
  await screen.findByText('Selecciona dos asignaturas distintas.');
  expect(request.mock.calls.some(([p]) => p === '/api/homologaciones/evaluar')).toBe(false);
});
test('errores de carga se pueden reintentar', async () => {
  const request = catalogo(); request.mockResolvedValueOnce(new Response(null, { status: 503 }));
  render(<EquivalenciaForm request={request} />); await screen.findByRole('alert');
  await userEvent.click(screen.getByRole('button', { name: 'Reintentar catálogo' }));
  await waitFor(() => expect(screen.getAllByRole('option', { name: /O · Materia O/ })).toHaveLength(2));
});
