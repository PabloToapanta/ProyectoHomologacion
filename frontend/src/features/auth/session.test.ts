import { describe, expect, it } from 'vitest';
import { loginResponse } from '../../test/fixtures';
import { readSession } from './session';

describe('sesión de UI basada en la respuesta de Auth', () => {
  it('extrae nombre, rol y caducidad sin almacenar el JWT', () => {
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

  it.each([
    { role: 'Estudiante' }, { role: 'administrador' }, { role: ['Administrador'] }, { name: '' }, { sub: '' },
    { exp: 0 }, { exp: '9999999999' }, { nbf: 9999999999 },
  ])('rechaza claims incompletos o vencidos: %j', (claims) => {
    expect(() => readSession(loginResponse(claims))).toThrow();
  });

  it('respeta la menor fecha entre el JWT y expiresAtUtc', () => {
    const now = Date.now();
    const response = loginResponse({ exp: Math.floor((now + 30_000) / 1000) }, now + 900_000);
    expect(readSession(response, now).expiresAt).toBeLessThanOrEqual(now + 30_000);
  });

  it.each(['invalido', 'a.b.', 'a.b.c.extra'])('rechaza tokens malformados %s', (accessToken) => {
    expect(() => readSession({ ...loginResponse(), accessToken })).toThrow();
  });

  it.each(['no-es-fecha', new Date(0).toISOString()])('rechaza una fecha de respuesta inválida: %s', (expiresAtUtc) => {
    expect(() => readSession({ ...loginResponse(), expiresAtUtc })).toThrow();
  });
});
