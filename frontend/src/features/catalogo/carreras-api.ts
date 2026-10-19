export interface CarreraInput { nombre: string; facultad: string; vigencia: boolean; modalidad: string | null }
export interface Carrera extends CarreraInput { id: string }
export type CatalogoRequest = (path: string, init?: RequestInit) => Promise<Response>;

class CatalogoError extends Error {}

export function carrerasApi(request: CatalogoRequest) {
  async function enviar<T>(path: string, init?: RequestInit): Promise<T> {
    try {
      const response = await request(path, init);
      if (!response.ok) {
        let message = response.status === 401 ? 'Tu sesión ha finalizado. Inicia sesión de nuevo.'
          : response.status === 403 ? 'No tienes permiso para gestionar carreras.'
          : response.status === 404 ? 'La carrera ya no está disponible. Actualiza el listado.'
          : 'El servicio no está disponible. Inténtalo de nuevo.';
        if (response.status === 400 || response.status === 409) {
          const problem = await response.json().catch(() => null) as { detail?: string; errors?: Record<string, string[]> } | null;
          message = problem?.detail || (problem?.errors && Object.values(problem.errors).flat().join(' ')) || 'Revisa los datos de la carrera.';
        }
        throw new CatalogoError(message);
      }
      if (response.status === 204) return undefined as T;
      return await response.json() as T;
    } catch (error) {
      if (init?.signal?.aborted || error instanceof CatalogoError) throw error;
      throw new CatalogoError('No pudimos conectar con el catálogo. Revisa tu conexión e inténtalo de nuevo.');
    }
  }
  const json = (method: string, input: CarreraInput): RequestInit => ({ method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) });
  return {
    listar: (signal?: AbortSignal) => enviar<Carrera[]>('/api/carreras', { signal }),
    crear: (input: CarreraInput) => enviar<Carrera>('/api/carreras', json('POST', input)),
    actualizar: (id: string, input: CarreraInput) => enviar<Carrera>(`/api/carreras/${encodeURIComponent(id)}`, json('PUT', input)),
    eliminar: (id: string) => enviar<void>(`/api/carreras/${encodeURIComponent(id)}`, { method: 'DELETE' }),
  };
}
