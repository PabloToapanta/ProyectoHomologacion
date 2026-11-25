import { render, screen } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { evidenciaLegible } from './SolicitudesDocentePage';
import { AsignaturasAprobadas } from './AsignaturasAprobadas';
import { InformePanel } from './InformePanel';

it('presenta la evidencia con sus ubicaciones y sin identificadores internos', () => {
  expect(evidenciaLegible('C1: ae43-o/3a76-c [plan-SW-101/U1/C1] → c7c4-r [plan-TI-101/U1/C1]')).toBe('C1 · plan-SW-101/U1/C1 → plan-TI-101/U1/C1');
  expect(evidenciaLegible('Formato desconocido')).toBe('Formato desconocido');
  expect(evidenciaLegible('C1 · Contenido: «Matrices» → «Matrices y determinantes» | Ubicación: U1 → U1'))
    .toBe('C1 · Contenido: «Matrices» → «Matrices y determinantes» (U1 → U1)');
});

it('una solicitud finalizada no ofrece registrar asignaturas', () => {
  render(<AsignaturasAprobadas finalizada tipoCambio="Interno" asignaturas={[]} ocupado={false} cargarOrigen={vi.fn()} registrar={vi.fn()} />);
  expect(screen.queryByRole('button', { name: 'Agregar asignatura aprobada' })).not.toBeInTheDocument();
  expect(screen.getByText('Solicitud finalizada: no admite registrar asignaturas.')).toBeInTheDocument();
});

it('el informe muestra el número Quipux y no el identificador interno de la solicitud', async () => {
  const informe = { solicitudId: 'sol-interna-123', tipoCambio: 'Interno', fecha: '2026-11-25T14:58:00', generadoPor: 'docente.ana', parejas: [], aprobadasSinAnalisis: [],
    convalidar: 0, noConvalidar: 0, sinAcuerdo: 0, advertencias: [] };
  render(<InformePanel request={vi.fn(async () => new Response(JSON.stringify(informe), { status: 200 }))} solicitudId="sol-interna-123" aprobadas={[]} puedeGenerar={false} numeroQuipux="UTA-2026-0001" />);
  expect(await screen.findByText(/Trámite Quipux: UTA-2026-0001/)).toBeInTheDocument();
  expect(screen.queryByText(/sol-interna-123/)).not.toBeInTheDocument();
  expect(screen.getByText('Generado por docente.ana el 25/11/2026 14:58.')).toBeInTheDocument();
});

it('con la solicitud remitida el docente solo consulta los análisis', async () => {
  const { AnalisisInternoPanel } = await import('./AnalisisInternoPanel');
  const { default: userEvent } = await import('@testing-library/user-event');
  const json = (v: unknown) => new Response(JSON.stringify(v), { status: 200 });
  const request = vi.fn(async (path: string) => json(path.endsWith('/opciones') ? [{ id: 'r1', codigo: 'R1', nombre: 'Receptora', nivel: 1 }] : []));
  render(<AnalisisInternoPanel soloLectura request={request} solicitudId="sol-a" aprobadas={[{ id: 'a1', nombreAsignatura: 'Origen' }]} inicial={{ estado: 'Pendiente', mensaje: '', resultado: null }} />);
  await userEvent.setup().click(screen.getByRole('button', { name: 'Consultar análisis interno' }));
  expect(await screen.findByText(/quedan solo en consulta/)).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Ejecutar análisis y guardar' })).not.toBeInTheDocument();
  expect(screen.queryByLabelText('Forzar un análisis nuevo')).not.toBeInTheDocument();
});
