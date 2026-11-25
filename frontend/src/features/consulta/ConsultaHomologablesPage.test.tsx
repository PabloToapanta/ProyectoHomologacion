import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { ConsultaHomologablesPage } from './ConsultaHomologablesPage';

const json = (v: unknown, status = 200) => new Response(JSON.stringify(v), { status });
const carreras = [{ id: 'c1', nombre: 'Origen sintético', facultad: 'FISEI', vigencia: true }, { id: 'c2', nombre: 'Receptora sintética', facultad: 'FISEI', vigencia: true }];
const a = (id: string, codigo: string) => ({ id, codigo, nombre: 'Asignatura ' + codigo, nivel: 1, malla: { id: 'm', nombre: 'Inicial', vigente: true } });
const resultado = {
  origen: carreras[0], receptora: carreras[1], advertencias: ['Consulta informativa: no es una homologación.'],
  pares: [
    { origen: a('o1', 'O1'), receptora: a('r1', 'R1'), estado: 'Homologable', porcentajeAcademico: 100, porcentajeHoras: 90, porcentajeCreditos: 100, umbralPct: 80, justificacion: '', informacionFaltante: [] },
    { origen: a('o2', 'O2'), receptora: a('r2', 'R2'), estado: 'InformacionIncompleta', porcentajeAcademico: null, porcentajeHoras: null, porcentajeCreditos: null, umbralPct: 80, justificacion: '', informacionFaltante: ['requiere nueva revisión del par'] }
  ]
};
async function elegir(origen: string, receptora: string) {
  const user = userEvent.setup();
  await user.selectOptions(await screen.findByLabelText('Carrera de origen'), origen);
  await user.selectOptions(screen.getByLabelText('Carrera receptora'), receptora);
  await user.click(screen.getByRole('button', { name: 'Consultar' }));
  return user;
}

it('CA1 y CA2: consulta dos carreras y muestra porcentajes, veredicto e información faltante', async () => {
  const request = vi.fn(async (path: string, _init?: RequestInit) => json(path.endsWith('/carreras') ? carreras : resultado));
  render(<ConsultaHomologablesPage request={request} />);
  await elegir('c1', 'c2');
  expect(await screen.findByText('Homologable')).toBeInTheDocument();
  expect(screen.getByText('90 %')).toBeInTheDocument();
  expect(screen.getByText('Información incompleta')).toBeInTheDocument();
  expect(screen.getByText('requiere nueva revisión del par')).toBeInTheDocument();
  expect(request).toHaveBeenLastCalledWith('/api/homologaciones/consulta-homologables?origen=c1&receptora=c2');
  // CA3: la pantalla solo hace lecturas.
  expect(request.mock.calls.every(c => !c[1]?.method || c[1].method === 'GET')).toBe(true);
});

it('valida carreras distintas sin consultar y muestra errores del servicio', async () => {
  const request = vi.fn(async (path: string) => path.endsWith('/carreras') ? json(carreras) : json({ title: 'No fue posible verificar los datos académicos autorizados.' }, 503));
  render(<ConsultaHomologablesPage request={request} />);
  const user = await elegir('c1', 'c1');
  expect(screen.getByRole('alert')).toHaveTextContent('dos carreras distintas');
  expect(request).toHaveBeenCalledTimes(1);
  await user.selectOptions(screen.getByLabelText('Carrera receptora'), 'c2');
  await user.click(screen.getByRole('button', { name: 'Consultar' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('No fue posible verificar');
});

it('sin pares revisados lo indica sin inferir coincidencias', async () => {
  const request = vi.fn(async (path: string) => json(path.endsWith('/carreras') ? carreras : { ...resultado, pares: [] }));
  render(<ConsultaHomologablesPage request={request} />);
  await elegir('c1', 'c2');
  expect(await screen.findByText(/No hay pares con correspondencias revisadas/)).toBeInTheDocument();
});
