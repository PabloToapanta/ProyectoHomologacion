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

// Sesión de la pestaña: sobrevive a recargar y se borra al cerrar la pestaña, al cerrar sesión, al vencer o ante 401.
// Nunca en localStorage. Lo guardado se valida igual que la respuesta del login; si no es válido, se descarta.
const CLAVE = 'fisei.sesion';

export function guardarSesion(session: Session): void {
  try { sessionStorage.setItem(CLAVE, JSON.stringify(session)); } catch { /* almacenamiento bloqueado: la sesión sigue en memoria */ }
}

export function borrarSesion(): void {
  try { sessionStorage.removeItem(CLAVE); } catch { /* sin almacenamiento disponible */ }
}

export function restaurarSesion(now = Date.now()): Session | null {
  let guardada: unknown;
  try {
    const texto = sessionStorage.getItem(CLAVE);
    if (texto === null) return null;
    guardada = JSON.parse(texto);
  } catch { borrarSesion(); return null; }
  const s = guardada as Partial<Session> | null;
  if (!s || typeof s.accessToken !== 'string' || !s.accessToken.trim() || typeof s.name !== 'string' || !s.name.trim()
    || !roles.includes(s.role as Role) || typeof s.expiresAt !== 'number' || !Number.isFinite(s.expiresAt) || s.expiresAt <= now) {
    borrarSesion();
    return null;
  }
  return { accessToken: s.accessToken, name: s.name, role: s.role as Role, expiresAt: s.expiresAt };
}
