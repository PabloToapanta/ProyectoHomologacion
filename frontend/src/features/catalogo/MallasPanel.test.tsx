import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { MallasPanel } from './MallasPanel';

const inicial = { id: 'm1', carreraId: 'c1', nombre: 'Inicial', vigente: true };
const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), { status });

it('lista versiones y permite filtrar sin confundir vigencia de carrera y malla', async () => {
  const seleccionar = vi.fn();
  render(<MallasPanel carreraId="c1" request={vi.fn().mockResolvedValue(json([inicial]))} onSeleccion={seleccionar} onCambio={vi.fn()} />);
  expect(await screen.findByText('Inicial · Vigente')).toBeInTheDocument();
  await userEvent.selectOptions(screen.getByLabelText('Versión de malla'), 'm1');
  expect(seleccionar).toHaveBeenLastCalledWith(inicial);
  await userEvent.selectOptions(screen.getByLabelText('Versión de malla'), '');
  expect(seleccionar).toHaveBeenLastCalledWith(null);
});

it('valida nombre y crea una versión sin activar ni copiar asignaturas por defecto', async () => {
  const request = vi.fn().mockImplementation((_path: string, init?: RequestInit) => Promise.resolve(
    init?.method === 'POST' ? json({ ...inicial, id: 'm2', nombre: 'Plan nuevo', vigente: false }, 201) : json([inicial])));
  const cambio = vi.fn();
  render(<MallasPanel carreraId="c1" request={request} onSeleccion={vi.fn()} onCambio={cambio} />);
  await screen.findByText('Inicial · Vigente');
  await userEvent.click(screen.getByRole('button', { name: 'Crear versión' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('nombre');
  expect(request).toHaveBeenCalledTimes(1);
  await userEvent.type(screen.getByLabelText('Nombre de la versión'), 'Plan nuevo');
  await userEvent.click(screen.getByRole('button', { name: 'Crear versión' }));
  await waitFor(() => expect(cambio).toHaveBeenCalled());
  expect(request).toHaveBeenCalledWith('/api/carreras/c1/mallas', expect.objectContaining({ method: 'POST', body: JSON.stringify({ nombre: 'Plan nuevo', vigente: false }) }));
});

it('muestra conflicto al activar y recarga sin confirmar un cambio fallido', async () => {
  const anterior = { ...inicial, id: 'm2', nombre: 'Anterior', vigente: false };
  const request = vi.fn().mockImplementation((_path: string, init?: RequestInit) => Promise.resolve(
    init?.method === 'PUT' ? json({ detail: 'Conflicto de versión' }, 409) : json([inicial, anterior])));
  const cambio = vi.fn();
  render(<MallasPanel carreraId="c1" request={request} onSeleccion={vi.fn()} onCambio={cambio} />);
  await userEvent.click(await screen.findByRole('button', { name: 'Activar Anterior' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Conflicto de versión');
  expect(cambio).not.toHaveBeenCalled();
  expect(screen.queryByText('Versión activada.')).not.toBeInTheDocument();
  await waitFor(() => expect(request).toHaveBeenCalledTimes(3));
});
