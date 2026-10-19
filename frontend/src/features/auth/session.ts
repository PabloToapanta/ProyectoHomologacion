import type { LoginResponse, Role, Session } from './types';

function decodePart(part: string): Record<string, unknown> {
  const base64 = part.replaceAll('-', '+').replaceAll('_', '/');
  const bytes = Uint8Array.from(atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')), (character) => character.charCodeAt(0));
  const value: unknown = JSON.parse(new TextDecoder().decode(bytes));
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error();
  return value as Record<string, unknown>;
}

// Solo interpreta claims para la UI. La firma/autorización se validan en el Gateway.
export function readSession(response: LoginResponse, now = Date.now()): Session {
  try {
    const parts = response.accessToken.split('.');
    if (parts.length !== 3 || parts.some((part) => !part) || response.tokenType !== 'Bearer') throw new Error();
    const header = decodePart(parts[0]);
    const claims = decodePart(parts[1]);
    const responseExpiry = Date.parse(response.expiresAtUtc);
    if (header.alg !== 'HS256' || typeof claims.exp !== 'number' || !Number.isFinite(claims.exp)
      || typeof claims.sub !== 'string' || !claims.sub.trim()
      || typeof claims.name !== 'string' || !claims.name.trim()
      || typeof claims.role !== 'string' || !['Administrador', 'Docente', 'Coordinador'].includes(claims.role)
      || (claims.nbf !== undefined && (typeof claims.nbf !== 'number' || !Number.isFinite(claims.nbf) || claims.nbf * 1000 > now))) throw new Error();
    const expiresAt = Math.min(claims.exp * 1000, responseExpiry);
    if (!Number.isFinite(expiresAt) || expiresAt <= now) throw new Error();
    return { accessToken: response.accessToken, name: claims.name, role: claims.role as Role, expiresAt };
  } catch {
    throw new Error('La sesión recibida no es válida. Vuelve a intentarlo.');
  }
}
