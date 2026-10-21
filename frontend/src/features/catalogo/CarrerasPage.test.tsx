import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { CarrerasPage } from './CarrerasPage';

const carrera = { id: '1', nombre: 'Carrera sintética', facultad: 'FISEI', vigencia: true, modalidad: null };
const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), { status });

it('conserva registros anteriores y exige revisar su facultad antes de editar', async () => {
  const user = userEvent.setup();
  const anterior = { ...carrera, facultad: 'Facultad sintética anterior' };
  const request = vi.fn().mockResolvedValue(json([anterior]));
  render(<CarrerasPage request={request} />);
  await user.click(await screen.findByRole('button', { name: `Editar ${carrera.nombre}` }));
  expect(screen.getByLabelText('Facultad')).toHaveValue(anterior.facultad);
  expect(screen.getByLabelText('Facultad')).toHaveAttribute('readonly');
  expect(screen.getByText(/requiere revisión antes de editar/i)).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Guardar carrera' })).toBeDisabled();
  expect(request).toHaveBeenCalledTimes(1);
});

it('lista carreras con su vigencia', async () => {
  render(<CarrerasPage request={vi.fn().mockResolvedValue(json([carrera]))} />);
  expect(await screen.findByText(carrera.nombre)).toBeInTheDocument();
  expect(screen.getByText('Vigente')).toBeInTheDocument();
});

it('crea, muestra en listado y permite cancelar edición', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValueOnce(json([])).mockResolvedValueOnce(json(carrera, 201));
  render(<CarrerasPage request={request} />);
  await screen.findByText('No hay carreras registradas.');
  await user.click(screen.getByRole('button', { name: 'Nueva carrera' }));
  await user.type(screen.getByLabelText('Nombre'), carrera.nombre);
  expect(screen.getByLabelText('Facultad')).toHaveValue('FISEI');
  expect(screen.getByLabelText('Facultad')).toHaveAttribute('readonly');
  await user.click(screen.getByRole('button', { name: 'Guardar carrera' }));
  expect(await screen.findByText(carrera.nombre)).toBeInTheDocument();
  expect(JSON.parse(request.mock.calls[1][1].body)).toMatchObject({ facultad: 'FISEI' });
  await user.click(screen.getByRole('button', { name: `Editar ${carrera.nombre}` }));
  expect(screen.getByLabelText('Nombre')).toHaveValue(carrera.nombre);
  await user.click(screen.getByRole('button', { name: 'Cancelar' }));
  expect(screen.queryByLabelText('Nombre')).not.toBeInTheDocument();
});

it('conserva FISEI fija al editar y actualiza vigencia', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValueOnce(json([carrera])).mockResolvedValueOnce(json({ ...carrera, vigencia: false }));
  render(<CarrerasPage request={request} />);
  await user.click(await screen.findByRole('button', { name: `Editar ${carrera.nombre}` }));
  expect(screen.getByLabelText('Facultad')).toHaveAttribute('readonly');
  expect(screen.getByLabelText('Facultad')).toHaveValue('FISEI');
  await user.click(screen.getByLabelText('Vigente'));
  await user.click(screen.getByRole('button', { name: 'Guardar carrera' }));
  expect(await screen.findByText('No vigente')).toBeInTheDocument();
  expect(JSON.parse(request.mock.calls[1][1].body)).toMatchObject({ facultad: 'FISEI', vigencia: false });
});

it('valida espacios en blanco antes de enviar', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValue(json([]));
  render(<CarrerasPage request={request} />);
  await screen.findByText('No hay carreras registradas.');
  await user.click(screen.getByRole('button', { name: 'Nueva carrera' }));
  await user.type(screen.getByLabelText('Nombre'), '   ');
  await user.click(screen.getByRole('button', { name: 'Guardar carrera' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Nombre es obligatorio');
  expect(request).toHaveBeenCalledTimes(1);
});

it.each(['Ya existe una carrera con ese nombre en la facultad.', 'La carrera tiene asignaturas relacionadas.'])('presenta conflicto sin quitar la fila: %s', async detail => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValueOnce(json([carrera])).mockResolvedValueOnce(json({ detail }, 409));
  render(<CarrerasPage request={request} />);
  await user.click(await screen.findByRole('button', { name: `Eliminar ${carrera.nombre}` }));
  await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirmar eliminación' }));
  expect(await screen.findByRole('alert')).toHaveTextContent(detail);
  expect(screen.getByRole('cell', { name: carrera.nombre })).toBeInTheDocument();
});

it('confirma eliminación y quita la fila', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValueOnce(json([carrera])).mockResolvedValueOnce(new Response(null, { status: 204 }));
  render(<CarrerasPage request={request} />);
  await user.click(await screen.findByRole('button', { name: `Eliminar ${carrera.nombre}` }));
  await user.click(screen.getByRole('button', { name: 'Confirmar eliminación' }));
  expect(await screen.findByText('No hay carreras registradas.')).toBeInTheDocument();
});

it('permite reintentar un listado fallido', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockRejectedValueOnce(new Error('sin red')).mockResolvedValueOnce(json([carrera]));
  render(<CarrerasPage request={request} />);
  await screen.findByRole('alert');
  await user.click(screen.getByRole('button', { name: 'Reintentar' }));
  await waitFor(() => expect(screen.getByText(carrera.nombre)).toBeInTheDocument());
});

it('conserva formulario y datos ante nombre duplicado', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValueOnce(json([])).mockResolvedValueOnce(json({ detail: 'Ya existe una carrera con ese nombre en la facultad.' }, 409));
  render(<CarrerasPage request={request} />);
  await screen.findByText('No hay carreras registradas.');
  await user.click(screen.getByRole('button', { name: 'Nueva carrera' }));
  await user.type(screen.getByLabelText('Nombre'), carrera.nombre);
  await user.click(screen.getByRole('button', { name: 'Guardar carrera' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe una carrera');
  expect(screen.getByLabelText('Nombre')).toHaveValue(carrera.nombre);
  expect(screen.getByText('No hay carreras registradas.')).toBeInTheDocument();
});

it('cancelar eliminación no envía DELETE', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValueOnce(json([carrera]));
  render(<CarrerasPage request={request} />);
  await user.click(await screen.findByRole('button', { name: `Eliminar ${carrera.nombre}` }));
  await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancelar' }));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(request).toHaveBeenCalledTimes(1);
});

it('deshabilita guardado mientras la solicitud está pendiente', async () => {
  const user = userEvent.setup();
  let resolver!: (response: Response) => void;
  const request = vi.fn().mockResolvedValueOnce(json([])).mockImplementationOnce(() => new Promise<Response>(resolve => { resolver = resolve; }));
  render(<CarrerasPage request={request} />);
  await screen.findByText('No hay carreras registradas.');
  await user.click(screen.getByRole('button', { name: 'Nueva carrera' }));
  await user.type(screen.getByLabelText('Nombre'), carrera.nombre);
  await user.click(screen.getByRole('button', { name: 'Guardar carrera' }));
  expect(screen.getByRole('button', { name: 'Guardando…' })).toBeDisabled();
  expect(screen.getByLabelText('Nombre')).toBeDisabled();
  resolver(json(carrera, 201));
  expect(await screen.findByText(carrera.nombre)).toBeInTheDocument();
});

it('busca carreras sin modificar el listado y permite limpiar el filtro', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValue(json([carrera, { ...carrera, id: '2', nombre: 'Electrónica' }]));
  render(<CarrerasPage request={request} />);
  await screen.findByText('Electrónica');
  await user.type(screen.getByRole('searchbox', { name: 'Buscar carreras' }), ' ELECTRÓNICA ');
  expect(screen.getByRole('cell', { name: 'Electrónica' })).toBeInTheDocument();
  expect(screen.queryByRole('cell', { name: carrera.nombre })).not.toBeInTheDocument();
  await user.clear(screen.getByRole('searchbox', { name: 'Buscar carreras' }));
  expect(screen.getByRole('cell', { name: carrera.nombre })).toBeInTheDocument();
  expect(request).toHaveBeenCalledTimes(1);
});

it('presenta duplicados dentro del modal sin perder los datos', async () => {
  const user = userEvent.setup();
  const request = vi.fn().mockResolvedValueOnce(json([])).mockResolvedValueOnce(json({ detail: 'Nombre duplicado.' }, 409));
  render(<CarrerasPage request={request} />);
  await screen.findByText('No hay carreras registradas.');
  await user.click(screen.getByRole('button', { name: 'Nueva carrera' }));
  const modal = within(screen.getByRole('dialog', { name: 'Nueva carrera' }));
  await user.type(modal.getByLabelText('Nombre'), carrera.nombre);
  await user.click(modal.getByRole('button', { name: 'Guardar carrera' }));
  expect(await modal.findByRole('alert')).toHaveTextContent('Nombre duplicado.');
  expect(modal.getByLabelText('Nombre')).toHaveValue(carrera.nombre);
});
