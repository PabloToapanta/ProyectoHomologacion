import { act, fireEvent, render, screen } from '@testing-library/react';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { loginResponse } from '../../test/fixtures';
import { AuthProvider, useAuth } from './AuthProvider';

function Probe() {
  const { session, login, logout, request } = useAuth();
  const [error, setError] = useState('');
  const handle = (operation: Promise<unknown>) => { void operation.catch((failure: Error) => setError(failure.message)); };
  return <><span data-testid="session">{session?.name ?? 'Sin sesión'}</span><output data-testid="error">{error}</output>
    <button onClick={() => handle(login({ nombreUsuario: 'admin-prueba', password: 'clave-sintetica' }))}>Autenticar</button>
    <button onClick={logout}>Salir</button>
    <button onClick={() => handle(request('/api/carreras'))}>Consultar</button>
    <button onClick={() => handle(request('https://destino-no-permitido.test'))}>Consultar externo</button>
  </>;
}
function mount() { render(<AuthProvider><Probe /></AuthProvider>); }
async function click(name: string) { await act(async () => { fireEvent.click(screen.getByRole('button', { name })); }); }
function backend(status = 200) {
  const mock = vi.fn().mockImplementation((path: string) => Promise.resolve(path === '/api/auth/login'
    ? new Response(JSON.stringify(loginResponse()), { status: 200 }) : new Response('{}', { status })));
  vi.stubGlobal('fetch', mock);
  return mock;
}

describe('ciclo de vida de sesión en memoria', () => {
  it('cierra la sesión automáticamente al vencer', async () => {
    vi.useFakeTimers();
    backend();
    mount();
    await click('Autenticar');
    expect(screen.getByTestId('session')).toHaveTextContent('admin-prueba');
    await act(async () => { await vi.advanceTimersByTimeAsync(900_001); });
    expect(screen.getByTestId('session')).toHaveTextContent('Sin sesión');
  });

  it('detecta caducidad al volver a una pestaña suspendida', async () => {
    backend();
    mount();
    await click('Autenticar');
    const later = Date.now() + 900_001;
    vi.spyOn(Date, 'now').mockReturnValue(later);
    await act(async () => { window.dispatchEvent(new Event('focus')); });
    expect(screen.getByTestId('session')).toHaveTextContent('Sin sesión');
  });

  it('adjunta Bearer al Gateway y elimina la sesión ante HTTP 401', async () => {
    const fetchMock = backend(401);
    mount();
    await click('Autenticar');
    await click('Consultar');
    expect(fetchMock.mock.calls[1][1].headers.get('Authorization')).toMatch(/^Bearer /);
    expect(screen.getByTestId('session')).toHaveTextContent('Sin sesión');
  });

  it('conserva una sesión válida ante HTTP 403', async () => {
    backend(403);
    mount();
    await click('Autenticar');
    await click('Consultar');
    expect(screen.getByTestId('session')).toHaveTextContent('admin-prueba');
  });

  it('no envía JWT a un origen ajeno al Gateway', async () => {
    const fetchMock = backend();
    mount();
    await click('Autenticar');
    await click('Consultar externo');
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('error')).toHaveTextContent('Gateway');
  });

  it('no realiza peticiones autenticadas después de cerrar sesión', async () => {
    const fetchMock = backend();
    mount();
    await click('Autenticar');
    await click('Salir');
    await click('Consultar');
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('error')).toHaveTextContent('Tu sesión ha finalizado');
  });

  it('una respuesta tardía no restablece una sesión cerrada', async () => {
    let resolve!: (response: Response) => void;
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() => new Promise<Response>((done) => { resolve = done; })));
    mount();
    await click('Autenticar');
    await click('Salir');
    await act(async () => { resolve(new Response(JSON.stringify(loginResponse()), { status: 200 })); });
    expect(screen.getByTestId('session')).toHaveTextContent('Sin sesión');
  });
});
