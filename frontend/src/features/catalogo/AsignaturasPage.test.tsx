import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { AsignaturasPage } from './AsignaturasPage';
import type { CatalogoRequest } from './carreras-api';

const carreras = [{ id: 'c1', nombre: 'Software de prueba', facultad: 'Facultad sintética', vigencia: true, modalidad: null }];
const algebra = { id: 'a1', carreraId: 'c1', codigo: 'DEMO-ALG', nombre: 'Álgebra Lineal', nivel: 1, creditos: 3, horas: 144 };
const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), { status });
const user = () => userEvent.setup();

it('solicita registrar carrera cuando no hay ninguna', async () => {
  render(<AsignaturasPage request={vi.fn().mockResolvedValue(json([]))} />);
  expect(await screen.findByText('Registra una carrera antes de agregar asignaturas.')).toBeInTheDocument();
});

it('lista por carrera y ordena por nivel', async () => {
  const u = user();
  const request = vi.fn().mockResolvedValueOnce(json(carreras)).mockResolvedValueOnce(json([
    { ...algebra, id: 'a2', codigo: 'DEMO-2', nombre: 'Segundo nivel', nivel: 2 }, algebra,
  ]));
  render(<AsignaturasPage request={request} />);
  await u.selectOptions(await screen.findByLabelText('Carrera'), 'c1');
  await screen.findByText('Álgebra Lineal');
  const rows = within(screen.getByRole('table')).getAllByRole('row');
  expect(rows[1]).toHaveTextContent('Álgebra Lineal');
  expect(rows[2]).toHaveTextContent('Segundo nivel');
});

async function abrirFormulario(request: CatalogoRequest) {
  const u = user();
  render(<AsignaturasPage request={request} />);
  await u.selectOptions(await screen.findByLabelText('Carrera'), 'c1');
  await screen.findByText('No hay asignaturas registradas en esta carrera.');
  await u.click(screen.getByRole('button', { name: 'Nueva asignatura' }));
  return u;
}

async function llenar(u: ReturnType<typeof userEvent.setup>) {
  await u.type(screen.getByLabelText('Código'), algebra.codigo);
  await u.type(screen.getByLabelText('Nombre'), algebra.nombre);
  await u.type(screen.getByLabelText('Nivel'), '1');
  await u.type(screen.getByLabelText('Créditos'), '3');
  await u.type(screen.getByLabelText('Horas'), '144');
}

it('registra y muestra la asignatura con números en la solicitud', async () => {
  const request = vi.fn().mockResolvedValueOnce(json(carreras)).mockResolvedValueOnce(json([])).mockResolvedValueOnce(json(algebra, 201));
  const u = await abrirFormulario(request);
  await llenar(u);
  await u.click(screen.getByRole('button', { name: 'Guardar asignatura' }));
  expect(await screen.findByText('Álgebra Lineal')).toBeInTheDocument();
  const body = JSON.parse(request.mock.calls[2][1].body);
  expect(body).toMatchObject({ carreraId: 'c1', nivel: 1, creditos: 3, horas: 144 });
});

it.each(['Nivel', 'Créditos', 'Horas'])('rechaza %s en cero sin enviar', async campo => {
  const request = vi.fn().mockResolvedValueOnce(json(carreras)).mockResolvedValueOnce(json([]));
  const u = await abrirFormulario(request);
  await llenar(u);
  await u.clear(screen.getByLabelText(campo));
  await u.type(screen.getByLabelText(campo), '0');
  await u.click(screen.getByRole('button', { name: 'Guardar asignatura' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('positivos');
  expect(request).toHaveBeenCalledTimes(2);
});

it('conserva formulario ante código duplicado y permite cancelar', async () => {
  const request = vi.fn().mockResolvedValueOnce(json(carreras)).mockResolvedValueOnce(json([])).mockResolvedValueOnce(json({ detail: 'Ya existe ese código en la carrera.' }, 409));
  const u = await abrirFormulario(request);
  await llenar(u);
  await u.click(screen.getByRole('button', { name: 'Guardar asignatura' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe ese código');
  expect(screen.getByLabelText('Nombre')).toHaveValue(algebra.nombre);
  await u.click(screen.getByRole('button', { name: 'Cancelar' }));
  expect(screen.queryByLabelText('Código')).not.toBeInTheDocument();
});

it('permite reintentar un fallo de listado', async () => {
  const u = user();
  const request = vi.fn().mockResolvedValueOnce(json(carreras)).mockRejectedValueOnce(new Error('sin conexión')).mockResolvedValueOnce(json([algebra]));
  render(<AsignaturasPage request={request} />);
  await u.selectOptions(await screen.findByLabelText('Carrera'), 'c1');
  await screen.findByRole('alert');
  await u.click(screen.getByRole('button', { name: 'Reintentar asignaturas' }));
  expect(await screen.findByText(algebra.nombre)).toBeInTheDocument();
});

it('bloquea guardado y cambio de carrera mientras registra', async () => {
  let resolver!: (response: Response) => void;
  const request = vi.fn().mockResolvedValueOnce(json(carreras)).mockResolvedValueOnce(json([])).mockImplementationOnce(() => new Promise<Response>(r => { resolver = r; }));
  const u = await abrirFormulario(request);
  await llenar(u);
  await u.click(screen.getByRole('button', { name: 'Guardar asignatura' }));
  expect(screen.getByRole('button', { name: 'Guardando…' })).toBeDisabled();
  expect(screen.getByLabelText('Carrera')).toBeDisabled();
  resolver(json(algebra, 201));
  await waitFor(() => expect(screen.getByText(algebra.nombre)).toBeInTheDocument());
});

it('ignora la respuesta tardía de una carrera al seleccionar otra', async () => {
  const u = user();
  let resolver!: (response: Response) => void;
  let signal: AbortSignal | undefined;
  const request = vi.fn<CatalogoRequest>().mockResolvedValueOnce(json([...carreras, { ...carreras[0], id: 'c2', nombre: 'Otra carrera' }]))
    .mockImplementationOnce((_path, init) => { signal = init?.signal as AbortSignal; return new Promise<Response>(r => { resolver = r; }); })
    .mockResolvedValueOnce(json([]));
  render(<AsignaturasPage request={request} />);
  await u.selectOptions(await screen.findByLabelText('Carrera'), 'c1');
  await u.selectOptions(screen.getByLabelText('Carrera'), 'c2');
  await screen.findByText('No hay asignaturas registradas en esta carrera.');
  expect(signal?.aborted).toBe(true);
  await act(async () => { resolver(json([algebra])); });
  expect(screen.queryByText(algebra.nombre)).not.toBeInTheDocument();
});
