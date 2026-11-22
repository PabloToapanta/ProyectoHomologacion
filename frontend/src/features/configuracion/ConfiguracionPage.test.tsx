import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { ConfiguracionPage } from './ConfiguracionPage';

const inicial = { versionId: '11111111-1111-1111-1111-111111111111', umbralPct: 80,
  antiguedadMaximaAnios: 10, periodosMinimosInterno: 1, asignaturasHomologablesMinimasInterno: 1,
  periodosMinimosExterno: 2, asignaturasHomologablesMinimasExterno: 2 };

it('muestra valores, guarda con auditoría y enseña historial', async () => {
  const user = userEvent.setup();
  let vigente = inicial;
  let cambio: Record<string, unknown> | null = null;
  const request = vi.fn(async (path: string, init?: RequestInit) => {
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
  await user.type(screen.getByLabelText('Respaldo'), 'Expediente');
  await user.click(screen.getByRole('button', { name: 'Guardar configuración' }));
  expect(await screen.findByRole('status')).toHaveTextContent('Configuración guardada');
  expect(screen.getByText('Acta')).toBeInTheDocument();
  expect(screen.getByText(/85% · 10 años/)).toBeInTheDocument();
  expect(request.mock.calls.find(c => c[1]?.method === 'PUT')?.[1]?.headers).toEqual({
    'Content-Type': 'application/json', 'If-Match': `"${inicial.versionId}"`
  });
  expect(cambio).toMatchObject({ umbralPct: 85, motivo: 'Acta', respaldo: 'Expediente' });
  await waitFor(() => expect(screen.getByLabelText('Motivo del cambio')).toHaveValue(''));
});

it('recarga la versión vigente al recibir 409', async () => {
  const user = userEvent.setup();
  let vigente = inicial; let escrituras = 0;
  const request = vi.fn(async (path: string, init?: RequestInit) => {
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
  await user.type(screen.getByLabelText('Respaldo'), 'Expediente');
  await user.click(screen.getByRole('button', { name: 'Guardar configuración' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Configuración modificada');
  expect(screen.getByLabelText('Umbral de compatibilidad (%)')).toHaveValue(90);
  expect(escrituras).toBe(1);
});
