import type { LoginResponse } from '../features/auth/types';

export function loginResponse(overrides: Record<string, unknown> = {}, expiresAt = Date.now() + 900_000): LoginResponse {
  const encode = (value: unknown) => btoa(JSON.stringify(value)).replaceAll('+', '-').replaceAll('/', '_').replaceAll('=', '');
  const payload = { sub: 'usuario-sintetico', name: 'admin-prueba', role: 'Administrador', exp: Math.floor(expiresAt / 1000), nbf: Math.floor(Date.now() / 1000) - 1, ...overrides };
  return { accessToken: `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.firma-sintetica`, tokenType: 'Bearer', expiresAtUtc: new Date(expiresAt).toISOString() };
}
