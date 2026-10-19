import type { Credentials, LoginResponse } from './types';

class LoginError extends Error {}

export async function loginRequest(credentials: Credentials, signal?: AbortSignal): Promise<LoginResponse> {
  const controller = new AbortController();
  let timedOut = false;
  const cancel = () => controller.abort();
  signal?.addEventListener('abort', cancel, { once: true });
  if (signal?.aborted) cancel();
  const timeout = setTimeout(() => { timedOut = true; controller.abort(); }, 15_000);
  try {
    const response = await fetch('/api/auth/login', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(credentials), credentials: 'omit', cache: 'no-store', signal: controller.signal,
    });
    if (response.status === 400) throw new LoginError('Revisa el usuario y la contraseña e inténtalo de nuevo.');
    if (response.status === 401) throw new LoginError('Usuario o contraseña incorrectos.');
    if (!response.ok) throw new LoginError('El servicio no está disponible. Inténtalo de nuevo en unos minutos.');
    let data: Partial<LoginResponse> | null;
    try { data = await response.json() as Partial<LoginResponse> | null; }
    catch { throw new LoginError('El servicio devolvió una respuesta no válida.'); }
    if (!data || typeof data.accessToken !== 'string' || !data.accessToken
      || data.tokenType !== 'Bearer' || typeof data.expiresAtUtc !== 'string') {
      throw new LoginError('El servicio devolvió una respuesta no válida.');
    }
    return data as LoginResponse;
  } catch (error) {
    if (signal?.aborted) throw new DOMException('Solicitud cancelada', 'AbortError');
    if (error instanceof LoginError) throw error;
    throw new LoginError(timedOut
      ? 'La conexión tardó demasiado. Inténtalo de nuevo.'
      : 'No pudimos conectar con el sistema. Revisa tu conexión e inténtalo de nuevo.');
  } finally {
    clearTimeout(timeout);
    signal?.removeEventListener('abort', cancel);
  }
}
