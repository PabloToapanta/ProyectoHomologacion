import type { LoginResponse, Role, Session } from './types';

const roles: Role[] = ['Administrador', 'Docente', 'Coordinador'];

// El token es opaco para React: solo el servidor descifra y valida sus claims.
export function readSession(response: LoginResponse, now = Date.now()): Session {
  const expiresAt = Date.parse(response.expiresAtUtc);
  if (typeof response.accessToken !== 'string' || !response.accessToken.trim()
    || response.tokenType !== 'Bearer'
    || typeof response.name !== 'string' || !response.name.trim()
    || !roles.includes(response.role)
    || !Number.isFinite(expiresAt) || expiresAt <= now) {
    throw new Error('La sesión recibida no es válida. Vuelve a intentarlo.');
  }
  return { accessToken: response.accessToken, name: response.name, role: response.role, expiresAt };
}
