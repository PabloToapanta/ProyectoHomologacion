import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { UsuariosPage } from './UsuariosPage';

const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), { status });
const docente = { id: 'u1', nombreUsuario: 'docente.ana', rol: 'Docente', activo: true };

it('lista cuentas y crea un docente sin mostrar la contraseña tras guardar', async () => {
  const user = userEvent.setup();
  const cuentas = [docente];
  const request = vi.fn(async (path: string, init?: RequestInit) => {
    if (path === '/api/usuarios' && init?.method === 'POST') {
      const input = JSON.parse(String(init.body));
      cuentas.push({ id: 'u2', nombreUsuario: input.nombreUsuario, rol: input.rol, activo: true });
      return json(cuentas[1], 201);
    }
    if (path === '/api/usuarios') return json(cuentas);
    throw new Error(path);
  });
  render(<UsuariosPage request={request} />);
  expect(await screen.findByText('docente.ana')).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Nuevo usuario' }));
  await user.type(screen.getByLabelText('Nombre de usuario'), 'coord.nuevo');
  await user.selectOptions(screen.getByLabelText('Rol'), 'Coordinador');
  await user.type(screen.getByLabelText('Contraseña inicial'), 'Sintetica-123!');
  await user.click(screen.getByRole('button', { name: 'Crear usuario' }));
  expect(await screen.findByText('coord.nuevo')).toBeInTheDocument();
  expect(screen.queryByDisplayValue('Sintetica-123!')).not.toBeInTheDocument();
  expect(screen.getByText(/entregar la contraseña cara a cara/i)).toBeInTheDocument();
  expect(JSON.parse(String(request.mock.calls.find(c => c[1]?.method === 'POST')![1]?.body))).toEqual({
    nombreUsuario: 'coord.nuevo', rol: 'Coordinador', contrasena: 'Sintetica-123!',
  });
});

it('cambia rol y estado, y limpia la contraseña nueva', async () => {
  const user = userEvent.setup();
  const cuentas = [{ ...docente }];
  const request = vi.fn(async (path: string, init?: RequestInit) => {
    if (path === '/api/usuarios') return json(cuentas);
    if (path.endsWith('/estado')) cuentas[0].activo = JSON.parse(String(init?.body)).activo;
    if (path.endsWith('/rol')) cuentas[0].rol = JSON.parse(String(init?.body)).rol;
    return json({});
  });
  render(<UsuariosPage request={request} />);
  const fila = await screen.findByRole('row', { name: /docente.ana/ });
  await user.click(within(fila).getByRole('button', { name: 'Cambiar rol de docente.ana' }));
  await user.selectOptions(screen.getByLabelText('Nuevo rol'), 'Coordinador');
  await user.click(screen.getByRole('button', { name: 'Guardar rol' }));
  expect(await screen.findByRole('row', { name: /docente.ana Coordinador/ })).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Desactivar docente.ana' }));
  expect(await screen.findByText('Inactivo')).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Activar docente.ana' }));
  expect(await screen.findByText('Activo')).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Cambiar contraseña de docente.ana' }));
  await user.type(screen.getByLabelText('Contraseña nueva'), 'Nueva-Sintetica-123!');
  await user.click(screen.getByRole('button', { name: 'Guardar contraseña' }));
  expect(screen.queryByDisplayValue('Nueva-Sintetica-123!')).not.toBeInTheDocument();
  expect(request.mock.calls.some(c => c[0] === '/api/usuarios/u1/contrasena' && c[1]?.method === 'PUT')).toBe(true);
});

it('muestra duplicado y conserva el formulario para corregirlo', async () => {
  const user = userEvent.setup();
  const request = vi.fn(async (_path: string, init?: RequestInit) => init?.method === 'POST' ? json({}, 409) : json([]));
  render(<UsuariosPage request={request} />);
  await screen.findByText('No hay usuarios registrados.');
  await user.click(screen.getByRole('button', { name: 'Nuevo usuario' }));
  await user.type(screen.getByLabelText('Nombre de usuario'), 'duplicado');
  await user.type(screen.getByLabelText('Contraseña inicial'), 'Sintetica-123!');
  await user.click(screen.getByRole('button', { name: 'Crear usuario' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('ya existe');
  expect(screen.getByLabelText('Nombre de usuario')).toHaveValue('duplicado');
  expect(screen.getByLabelText('Contraseña inicial')).toHaveValue('');
});
