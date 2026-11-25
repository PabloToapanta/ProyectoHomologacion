import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { ConfiguracionPage } from './ConfiguracionPage';

const inicial = { versionId: '11111111-1111-1111-1111-111111111111', umbralPct: 80,
  antiguedadMaximaAnios: 10, periodosMinimosInterno: 1, asignaturasHomologablesMinimasInterno: 1,
  periodosMinimosExterno: 2, asignaturasHomologablesMinimasExterno: 2 };
const normativa = (codigo: string, estado = 'Vigente', reformadaPor: { id: string; codigo: string } | null = null) => ({
  id: `id-${codigo}`, codigo, titulo: 'Instructivo sintético', emisor: 'Consejo sintético', fechaEmision: '2023-03-01',
  vigenteDesde: '2023-03-02', estado, reformaA: null, reformadaPor, motivoDerogacion: null, fechaDerogacion: null,
  nombreArchivo: 'n.pdf', tamano: 10, sha256: 'A', fechaRegistro: '2026-11-25T00:00:00Z', registradaPor: 'admin' });
const catalogo = [normativa('RES-SINT-1')];

it('muestra valores, guarda con auditoría y enseña historial', async () => {
  const user = userEvent.setup();
  let vigente = inicial;
  let cambio: Record<string, unknown> | null = null;
  const request = vi.fn(async (path: string, init?: RequestInit) => {
    if (path.startsWith('/api/normativas')) return new Response(JSON.stringify(catalogo));
    if (path.includes('/historial')) return new Response(JSON.stringify({ entradas: cambio ? [{
      versionId: vigente.versionId, fechaHoraUtc: '2026-11-21T00:00:00Z', actorId: '22222222-2222-2222-2222-222222222222',
      anterior: inicial, nueva: vigente, motivo: cambio.motivo, respaldo: cambio.respaldo
    }] : [], cursorSiguiente: null }));
    if (init?.method === 'PUT') {
      cambio = JSON.parse(String(init.body));
      vigente = { ...inicial, umbralPct: Number(cambio?.umbralPct), versionId: '33333333-3333-3333-3333-333333333333' };
      return new Response(JSON.stringify(vigente), { headers: { ETag: `"${vigente.versionId}"` } });
    }
    return new Response(JSON.stringify(vigente), { headers: { ETag: `"${vigente.versionId}"` } });
  });
  render(<ConfiguracionPage request={request} />);
  expect(await screen.findByDisplayValue('80')).toBeInTheDocument();
  await user.clear(screen.getByLabelText('Umbral de compatibilidad (%)'));
  await user.type(screen.getByLabelText('Umbral de compatibilidad (%)'), '85');
  await user.type(screen.getByLabelText('Motivo del cambio'), 'Acta');
  await user.selectOptions(screen.getByLabelText('Normativa que respalda el cambio'), 'id-RES-SINT-1');
  await user.type(screen.getByLabelText(/Detalle del respaldo/), 'Art. 5');
  await user.click(screen.getByRole('button', { name: 'Guardar configuración' }));
  expect(await screen.findByRole('status')).toHaveTextContent('Configuración guardada');
  expect(screen.getByText('Acta')).toBeInTheDocument();
  expect(screen.getByText(/85% · 10 años/)).toBeInTheDocument();
  expect(request.mock.calls.find(c => c[1]?.method === 'PUT')?.[1]?.headers).toEqual({
    'Content-Type': 'application/json', 'If-Match': `"${inicial.versionId}"`
  });
  expect(cambio).toMatchObject({ umbralPct: 85, motivo: 'Acta', respaldo: 'RES-SINT-1 — Instructivo sintético · Art. 5' });
  await waitFor(() => expect(screen.getByLabelText('Motivo del cambio')).toHaveValue(''));
});

it('recarga la versión vigente al recibir 409', async () => {
  const user = userEvent.setup();
  let vigente = inicial; let escrituras = 0;
  const request = vi.fn(async (path: string, init?: RequestInit) => {
    if (path.startsWith('/api/normativas')) return new Response(JSON.stringify(catalogo));
    if (path.includes('/historial')) return new Response(JSON.stringify({ entradas: [], cursorSiguiente: null }));
    if (init?.method === 'PUT') {
      escrituras++;
      vigente = { ...inicial, umbralPct: 90, versionId: '44444444-4444-4444-4444-444444444444' };
      return new Response(JSON.stringify({ detail: 'Configuración modificada' }), { status: 409 });
    }
    return new Response(JSON.stringify(vigente), { headers: { ETag: `"${vigente.versionId}"` } });
  });
  render(<ConfiguracionPage request={request} />);
  await screen.findByDisplayValue('80');
  await user.type(screen.getByLabelText('Motivo del cambio'), 'Acta');
  await user.selectOptions(await screen.findByLabelText('Normativa que respalda el cambio'), 'id-RES-SINT-1');
  await user.click(screen.getByRole('button', { name: 'Guardar configuración' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Configuración modificada');
  expect(screen.getByLabelText('Umbral de compatibilidad (%)')).toHaveValue(90);
  expect(escrituras).toBe(1);
});

it('HU-38: sin normativa elegida no guarda y avisa si la normativa citada fue reformada', async () => {
  const user = userEvent.setup();
  const reformada = normativa('RES-SINT-1', 'Reformada', { id: 'id-RES-SINT-2', codigo: 'RES-SINT-2' });
  const request = vi.fn(async (path: string, init?: RequestInit) => {
    if (path.startsWith('/api/normativas')) return new Response(JSON.stringify([reformada, normativa('RES-SINT-2')]));
    if (path.includes('/historial')) return new Response(JSON.stringify({ entradas: [{
      versionId: inicial.versionId, fechaHoraUtc: '2026-11-21T00:00:00Z', actorId: '22222222-2222-2222-2222-222222222222',
      anterior: null, nueva: inicial, motivo: 'Inicial', respaldo: 'RES-SINT-1 — Instructivo sintético' }], cursorSiguiente: null }));
    if (init?.method === 'PUT') throw new Error('no debe guardar');
    return new Response(JSON.stringify(inicial), { headers: { ETag: `"${inicial.versionId}"` } });
  });
  render(<ConfiguracionPage request={request} />);
  expect(await screen.findByText(/se apoya en RES-SINT-1, que está «Reformada» por RES-SINT-2/)).toBeInTheDocument();
  const opciones = within(screen.getByLabelText('Normativa que respalda el cambio')).getAllByRole('option').map(o => o.textContent);
  expect(opciones).toEqual(['Selecciona una normativa vigente', 'RES-SINT-2 — Instructivo sintético']);
  await user.type(screen.getByLabelText('Motivo del cambio'), 'Acta');
  await user.click(screen.getByRole('button', { name: 'Guardar configuración' }));
  expect(request.mock.calls.some(c => c[1]?.method === 'PUT')).toBe(false);
});
