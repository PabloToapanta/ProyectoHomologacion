import { describe, expect, it, vi } from 'vitest';
import { loginResponse } from '../../test/fixtures';
import { loginRequest } from './login-api';

const credentials = { nombreUsuario: 'admin-prueba', password: 'clave-sintetica' };

describe('cliente HTTP de login', () => {
  it('envía el JSON existente al Gateway sin añadir Authorization', async () => {
    const response = loginResponse();
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(response), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);
    expect(await loginRequest(credentials)).toEqual(response);
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('/api/auth/login');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body)).toEqual(credentials);
    expect(init.headers).toEqual({ 'Content-Type': 'application/json' });
    expect(init.credentials).toBe('omit');
  });

  it.each([
    [400, 'Revisa el usuario y la contraseña'], [401, 'Usuario o contraseña incorrectos'],
    [502, 'El servicio no está disponible'], [500, 'El servicio no está disponible'],
  ])('explica HTTP %i sin mostrar mensajes internos', async (status, message) => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('<html>detalle-interno</html>', { status: Number(status) })));
    await expect(loginRequest(credentials)).rejects.toThrow(String(message));
  });

  it('explica fallos de conexión', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));
    await expect(loginRequest(credentials)).rejects.toThrow('No pudimos conectar');
  });

  it('rechaza respuestas 200 que no cumplen el contrato', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 200 })));
    await expect(loginRequest(credentials)).rejects.toThrow('respuesta no válida');
  });
});
