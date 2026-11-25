import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import { NormativasPage } from './NormativasPage';
import { normativaDelRespaldo, respaldoDe } from './normativas-api';
import type { Normativa } from './normativas-api';

const n = (codigo: string, extra: Partial<Normativa> = {}): Normativa => ({
  id: `id-${codigo}`, codigo, titulo: `Instructivo ${codigo}`, emisor: 'Consejo sintético', fechaEmision: '2023-03-01',
  vigenteDesde: '2023-03-02', estado: 'Vigente', reformaA: null, reformadaPor: null, motivoDerogacion: null, fechaDerogacion: null,
  nombreArchivo: 'n.pdf', tamano: 2048, sha256: 'A', fechaRegistro: '2026-11-25T00:00:00Z', registradaPor: 'admin', ...extra });

const json = (v: unknown, status = 200) => new Response(JSON.stringify(v), { status });

test('el Docente consulta y filtra, sin acciones de registro, derogación ni eliminación', async () => {
  const lista = [n('RES-2', { reformaA: { id: 'id-RES-1', codigo: 'RES-1' } }), n('RES-1', { estado: 'Reformada', reformadaPor: { id: 'id-RES-2', codigo: 'RES-2' } })];
  const request = vi.fn(async () => json(lista));
  render(<NormativasPage request={request} rol="Docente" />);
  const tabla = await screen.findByRole('table', { name: 'Normativas registradas' });
  expect(within(tabla).getByText('por RES-2')).toBeInTheDocument();
  expect(within(tabla).getByText('Reforma a RES-1')).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Registrar normativa' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: /Derogar/ })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: /Eliminar/ })).not.toBeInTheDocument();
  await userEvent.setup().selectOptions(screen.getByLabelText('Filtrar por estado'), 'Reformada');
  expect(within(screen.getByRole('table')).getAllByRole('row')).toHaveLength(2);
});

test('el Coordinador registra una reforma con su PDF y no puede eliminar', async () => {
  const user = userEvent.setup();
  let lista = [n('RES-1')];
  const request = vi.fn(async (_path: string, init?: RequestInit) => {
    if (init?.method === 'POST') {
      const form = init.body as FormData;
      expect(form.get('codigo')).toBe('RES-2');
      expect(form.get('reformaAId')).toBe('id-RES-1');
      expect((form.get('archivo') as File).name).toBe('reforma.pdf');
      const nueva = n('RES-2', { reformaA: { id: 'id-RES-1', codigo: 'RES-1' } });
      lista = [nueva, n('RES-1', { estado: 'Reformada', reformadaPor: { id: 'id-RES-2', codigo: 'RES-2' } })];
      return json(nueva, 201);
    }
    return json(lista);
  });
  render(<NormativasPage request={request} rol="Coordinador" />);
  await screen.findByRole('table');
  expect(screen.queryByRole('button', { name: /Eliminar/ })).not.toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Registrar normativa' }));
  const dialogo = screen.getByRole('dialog', { name: 'Registrar normativa' });
  await user.type(within(dialogo).getByLabelText('Código de resolución'), 'RES-2');
  await user.type(within(dialogo).getByLabelText('Título'), 'Reforma sintética');
  await user.type(within(dialogo).getByLabelText('Órgano emisor'), 'Consejo sintético');
  fireEvent.change(within(dialogo).getByLabelText('Fecha de emisión'), { target: { value: '2026-03-01' } });
  fireEvent.change(within(dialogo).getByLabelText('Vigente desde'), { target: { value: '2026-03-02' } });
  await user.selectOptions(within(dialogo).getByLabelText('¿Reforma a otra normativa?'), 'id-RES-1');
  await user.upload(within(dialogo).getByLabelText(/PDF oficial/), new File(['%PDF-1.4'], 'reforma.pdf', { type: 'application/pdf' }));
  await user.click(within(dialogo).getByRole('button', { name: 'Registrar' }));
  expect(await screen.findByRole('status')).toHaveTextContent('RES-1 pasa a «Reformada»');
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});

test('el Administrador elimina con motivo y ve el historial', async () => {
  const user = userEvent.setup();
  let lista = [n('RES-2', { reformaA: { id: 'id-RES-1', codigo: 'RES-1' } }), n('RES-1', { estado: 'Reformada', reformadaPor: { id: 'id-RES-2', codigo: 'RES-2' } })];
  const request = vi.fn(async (path: string, init?: RequestInit) => {
    if (init?.method === 'DELETE') {
      expect(path).toBe('/api/normativas/id-RES-2');
      expect(JSON.parse(String(init.body))).toEqual({ motivo: 'Código equivocado' });
      lista = [n('RES-1')];
      return new Response(null, { status: 204 });
    }
    if (path.endsWith('/historial')) return json([{ id: 'e', tipo: 'Registrada', fecha: '2026-11-25T15:00:00Z', usuario: 'admin', detalle: null }]);
    return json(lista);
  });
  render(<NormativasPage request={request} rol="Administrador" />);
  await screen.findByRole('table');
  // La norma reformada no se elimina mientras exista su reforma.
  expect(screen.queryByRole('button', { name: 'Eliminar RES-1' })).not.toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Historial de RES-1' }));
  expect(await screen.findByRole('dialog', { name: 'Historial de RES-1' })).toHaveTextContent('Registrada');
  await user.click(screen.getByRole('button', { name: 'Cerrar ventana' }));
  await user.click(screen.getByRole('button', { name: 'Eliminar RES-2' }));
  const dialogo = screen.getByRole('dialog', { name: 'Eliminar RES-2' });
  expect(dialogo).toHaveTextContent('RES-1 volverá a estar vigente');
  await user.type(within(dialogo).getByLabelText('Motivo'), 'Código equivocado');
  await user.click(within(dialogo).getByRole('button', { name: 'Eliminar definitivamente' }));
  expect(await screen.findByRole('status')).toHaveTextContent('RES-1 vuelve a estar vigente');
  await waitFor(() => expect(screen.getAllByRole('row')).toHaveLength(2));
});

test('muestra el error del servidor al registrar', async () => {
  const user = userEvent.setup();
  const request = vi.fn(async (_path: string, init?: RequestInit) =>
    init?.method === 'POST' ? json({ detail: 'Ya existe una normativa con ese código de resolución.' }, 409) : json([]));
  render(<NormativasPage request={request} rol="Administrador" />);
  expect(await screen.findByText('No hay normativas registradas.')).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Registrar normativa' }));
  const dialogo = screen.getByRole('dialog', { name: 'Registrar normativa' });
  for (const [campo, valor] of [['Código de resolución', 'RES-1'], ['Título', 'T'], ['Órgano emisor', 'E']])
    await user.type(within(dialogo).getByLabelText(campo), valor);
  for (const campo of ['Fecha de emisión', 'Vigente desde'])
    fireEvent.change(within(dialogo).getByLabelText(campo), { target: { value: '2023-01-01' } });
  await user.upload(within(dialogo).getByLabelText(/PDF oficial/), new File(['%PDF-1.4'], 'a.pdf', { type: 'application/pdf' }));
  await user.click(within(dialogo).getByRole('button', { name: 'Registrar' }));
  expect(await within(dialogo).findByRole('alert')).toHaveTextContent('Ya existe una normativa con ese código');
});

test('el respaldo guardado identifica la normativa por su código; el texto libre antiguo no coincide', () => {
  const lista = [n('RES-1'), n('RES-10')];
  expect(normativaDelRespaldo(respaldoDe(lista[1]) + ' · Art. 5', lista)?.codigo).toBe('RES-10');
  expect(normativaDelRespaldo('RES-1 — Instructivo RES-1', lista)?.codigo).toBe('RES-1');
  expect(normativaDelRespaldo('Acta de la reunión del 19/10', lista)).toBeNull();
  expect(normativaDelRespaldo(undefined, lista)).toBeNull();
});
