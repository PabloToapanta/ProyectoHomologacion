import type { CatalogoRequest } from './carreras-api';

export interface AsignaturaInput { carreraId: string; codigo: string; nombre: string; nivel: number; creditos: number; horas: number }
export interface Asignatura extends AsignaturaInput { id: string }

class AsignaturasError extends Error {}

export function asignaturasApi(request: CatalogoRequest) {
  async function enviar<T>(path: string, init?: RequestInit): Promise<T> {
    try {
      const response = await request(path, init);
      if (!response.ok) {
        let message = response.status === 401 ? 'Tu sesión ha finalizado. Inicia sesión de nuevo.'
          : response.status === 403 ? 'No tienes permiso para gestionar asignaturas.'
          : response.status === 404 ? 'No se encontró la carrera o asignatura. Actualiza el listado.'
          : 'El servicio no está disponible. Inténtalo de nuevo.';
        if (response.status === 400 || response.status === 409) {
          const problem = await response.json().catch(() => null) as { detail?: string; errors?: Record<string, string[]> } | null;
          message = problem?.detail || (problem?.errors && Object.values(problem.errors).flat().join(' ')) || 'Revisa los datos de la asignatura.';
        }
        throw new AsignaturasError(message);
      }
      return await response.json() as T;
    } catch (error) {
      if (init?.signal?.aborted || error instanceof AsignaturasError) throw error;
      throw new AsignaturasError('No pudimos conectar con el catálogo. Revisa tu conexión e inténtalo de nuevo.');
    }
  }
  return {
    listar: (carreraId: string, signal?: AbortSignal) => enviar<Asignatura[]>(`/api/carreras/${encodeURIComponent(carreraId)}/asignaturas`, { signal }),
    obtener: (id: string) => enviar<Asignatura>(`/api/asignaturas/${encodeURIComponent(id)}`),
    crear: (input: AsignaturaInput) => enviar<Asignatura>('/api/asignaturas', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) }),
  };
}
