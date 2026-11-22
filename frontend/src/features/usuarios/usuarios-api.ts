export type RolGestionable = 'Docente' | 'Coordinador';
export interface Usuario { id: string; nombreUsuario: string; rol: RolGestionable; activo: boolean }
export interface CrearUsuario { nombreUsuario: string; rol: RolGestionable; contrasena: string }
export type UsuariosRequest = (path: string, init?: RequestInit) => Promise<Response>;

class UsuariosError extends Error {}

export function usuariosApi(request: UsuariosRequest) {
  async function enviar<T>(path: string, init?: RequestInit): Promise<T> {
    let response: Response;
    try { response = await request(path, init); }
    catch (error) {
      if (init?.signal?.aborted || error instanceof DOMException && error.name === 'AbortError') throw error;
      throw new UsuariosError('No pudimos conectar con el servicio de usuarios. Revisa tu conexión e inténtalo de nuevo.');
    }
    if (!response.ok) {
      const mensajes: Record<number, string> = {
        400: 'Revisa los datos del usuario. El nombre y rol son obligatorios; la contraseña debe tener entre 12 y 1024 caracteres.',
        401: 'Tu sesión ha finalizado. Inicia sesión de nuevo.',
        403: 'No tienes permiso para gestionar usuarios.',
        404: 'El usuario no existe. Actualiza el listado.',
        409: 'El nombre de usuario ya existe.',
      };
      throw new UsuariosError(mensajes[response.status] ?? 'El servicio de usuarios no está disponible. Inténtalo de nuevo.');
    }
    if (response.status === 204 || response.headers.get('content-length') === '0') return undefined as T;
    const text = await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }

  const json = (method: string, value: unknown): RequestInit => ({ method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) });
  const ruta = (id: string) => `/api/usuarios/${encodeURIComponent(id)}`;
  return {
    listar: (signal?: AbortSignal) => enviar<Usuario[]>('/api/usuarios', { signal }),
    obtener: (id: string) => enviar<Usuario>(ruta(id)),
    crear: (input: CrearUsuario) => enviar<Usuario>('/api/usuarios', json('POST', input)),
    cambiarEstado: (id: string, activo: boolean) => enviar<void>(`${ruta(id)}/estado`, json('PATCH', { activo })),
    cambiarRol: (id: string, rol: RolGestionable) => enviar<void>(`${ruta(id)}/rol`, json('PUT', { rol })),
    cambiarContrasena: (id: string, contrasena: string) => enviar<void>(`${ruta(id)}/contrasena`, json('PUT', { contrasena })),
  };
}
