import { describe, expect, it } from 'vitest';
import { loginResponse } from '../../test/fixtures';
import { readSession } from './session';

describe('sesión de UI basada en los metadatos de Auth', () => {
  it('usa nombre, rol y caducidad sin decodificar ni almacenar el JWT', () => {
    const response = loginResponse();
    const session = readSession(response);
    expect(session.name).toBe('admin-prueba');
    expect(session.role).toBe('Administrador');
    expect(session.accessToken).toBe(response.accessToken);
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it.each(['Administrador', 'Docente', 'Coordinador'])('reconoce el rol %s', (role) => {
    expect(readSession(loginResponse({ role })).role).toBe(role);
  });

  it.each([{ role: 'Estudiante' }, { role: 'administrador' }, { role: ['Administrador'] },
    { name: '' }, { name: null }, { accessToken: '' }, { tokenType: 'Basic' },
    { expiresAtUtc: 'no-es-fecha' }, { expiresAtUtc: new Date(0).toISOString() }])
  ('rechaza metadatos inválidos: %j', (value) => {
    expect(() => readSession(loginResponse(value))).toThrow();
  });

  it('respeta la fecha de expiración de la respuesta, sin interpretar el token', () => {
    const now = Date.now();
    const response = loginResponse({}, now + 30_000);
    expect(readSession(response, now).expiresAt).toBeLessThanOrEqual(now + 30_000);
  });

  it('acepta el token como valor opaco', () => {
    expect(readSession(loginResponse({ accessToken: 'opaco' })).accessToken).toBe('opaco');
  });
});
