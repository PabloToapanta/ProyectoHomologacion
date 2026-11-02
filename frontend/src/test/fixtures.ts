import type { LoginResponse } from '../features/auth/types';

export function loginResponse(overrides: Record<string, unknown> = {}, expiresAt = Date.now() + 900_000): LoginResponse {
  return {
    accessToken: 'jwe.sintetico.cifrado.sin-claims.visibles', tokenType: 'Bearer',
    expiresAtUtc: new Date(expiresAt).toISOString(), name: 'admin-prueba', role: 'Administrador',
    ...overrides,
  } as LoginResponse;
}
