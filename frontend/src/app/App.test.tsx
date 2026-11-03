import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { loginResponse } from '../test/fixtures';
import { App } from './App';

function mount(path = '/login') {
  return render(<MemoryRouter initialEntries={[path]}><App /></MemoryRouter>);
}
async function fillLogin() {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Usuario'), 'admin-prueba');
  await user.type(screen.getByLabelText('Contraseña', { exact: true }), 'clave-sintetica');
  return user;
}
function mockLogin(role = 'Administrador') {
  const mock = vi.fn().mockResolvedValue(new Response(JSON.stringify(loginResponse({ role })), { status: 200 }));
  vi.stubGlobal('fetch', mock);
  return mock;
}

describe('HU-01 / T3 — acceso al panel', () => {
  it('redirige acceso directo sin sesión a login', () => {
    mount('/panel');
    expect(screen.getByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Panel de administración' })).not.toBeInTheDocument();
  });

  it('autentica con el contrato existente y permite cerrar sesión', async () => {
    const fetchMock = mockLogin();
    mount();
    const user = await fillLogin();
    await user.click(screen.getByRole('button', { name: 'Ingresar' }));
    expect(await screen.findByRole('heading', { name: 'Panel de administración' })).toBeInTheDocument();
    expect(screen.getByText('admin-prueba')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
    await user.click(screen.getByRole('button', { name: 'Cerrar sesión' }));
    expect(await screen.findByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
  });

  it.each([400, 401, 502])('muestra un error %i y conserva el acceso protegido', async (status) => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status })));
    mount();
    const user = await fillLogin();
    await user.click(screen.getByRole('button', { name: 'Ingresar' }));
    expect(await screen.findByRole('alert')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Panel de administración' })).not.toBeInTheDocument();
    expect(screen.getByLabelText('Contraseña', { exact: true })).toHaveValue('');
  });

  it('muestra un fallo de conexión y permite reintentar', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));
    mount();
    const user = await fillLogin();
    await user.click(screen.getByRole('button', { name: 'Ingresar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('No pudimos conectar');
    expect(screen.getByRole('button', { name: 'Ingresar' })).toBeEnabled();
  });

  it('muestra carga y evita envíos duplicados', async () => {
    let resolve!: (response: Response) => void;
    const fetchMock = vi.fn().mockImplementation(() => new Promise<Response>((done) => { resolve = done; }));
    vi.stubGlobal('fetch', fetchMock);
    mount();
    const user = await fillLogin();
    await user.click(screen.getByRole('button', { name: 'Ingresar' }));
    expect(screen.getByRole('button', { name: /Validando/ })).toBeDisabled();
    await user.click(screen.getByRole('button', { name: /Validando/ }));
    expect(fetchMock).toHaveBeenCalledTimes(1);
    await act(async () => resolve(new Response(JSON.stringify(loginResponse()), { status: 200 })));
    expect(await screen.findByRole('heading', { name: 'Panel de administración' })).toBeInTheDocument();
  });

  it.each(['Docente'])('el rol %s no accede al panel de administrador', async (role) => {
    mockLogin(role);
    mount();
    const user = await fillLogin();
    await user.click(screen.getByRole('button', { name: 'Ingresar' }));
    expect(await screen.findByRole('heading', { name: 'Acceso restringido' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Panel de administración' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Cerrar sesión' }));
    expect(await screen.findByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
  });

  it('coordinador entra a solicitudes y conserva acceso restringido al panel administrador', async () => {
    const user = userEvent.setup();
    vi.stubGlobal('fetch', vi.fn(async (path: string) => new Response(JSON.stringify(path === '/api/auth/login' ? loginResponse({ role: 'Coordinador' }) : []), {status:200})));
    mount();await user.type(screen.getByLabelText('Usuario'), 'coordinador-sintetico');await user.type(screen.getByLabelText('Contraseña', {exact:true}), 'clave-sintetica');
    await user.click(screen.getByRole('button', {name:'Ingresar'}));
    expect(await screen.findByRole('heading', {name:'Solicitudes de homologación'})).toBeInTheDocument();
    expect(screen.queryByRole('heading', {name:'Panel de administración'})).not.toBeInTheDocument();
    expect(screen.getByRole('link', {name:/Solicitudes/})).toBeInTheDocument();
  });

  it('rechaza una respuesta con JWT vencido sin abrir el panel', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(loginResponse({ exp: 0 })), { status: 200 })));
    mount();
    const user = await fillLogin();
    await user.click(screen.getByRole('button', { name: 'Ingresar' }));
    expect(await screen.findByRole('alert')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Panel de administración' })).not.toBeInTheDocument();
  });

  it('pierde la sesión al desmontar y volver a cargar la aplicación', async () => {
    mockLogin();
    const view = mount();
    const user = await fillLogin();
    await user.click(screen.getByRole('button', { name: 'Ingresar' }));
    await screen.findByRole('heading', { name: 'Panel de administración' });
    view.unmount();
    mount('/panel');
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument());
  });
});
