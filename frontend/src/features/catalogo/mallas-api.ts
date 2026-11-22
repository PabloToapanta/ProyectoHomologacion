import type { CatalogoRequest } from './carreras-api';

export interface Malla { id: string; carreraId: string; nombre: string; vigente: boolean }

export function mallasApi(request: CatalogoRequest) {
  async function enviar<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await request(path, init);
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { detail?: string } | null;
      throw new Error(response.status === 401 ? 'Tu sesión ha finalizado.' : response.status === 403
        ? 'Solo el Administrador gestiona versiones de malla.' : problem?.detail || 'No se pudieron consultar o guardar las mallas. Reintenta la operación.');
    }
    return response.json() as Promise<T>;
  }
  return {
    listar: (carreraId: string, signal?: AbortSignal) => enviar<Malla[]>(`/api/carreras/${encodeURIComponent(carreraId)}/mallas`, { signal }),
    crear: (carreraId: string, nombre: string, vigente: boolean) => enviar<Malla>(`/api/carreras/${encodeURIComponent(carreraId)}/mallas`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ nombre, vigente }),
    }),
    activar: (id: string) => enviar<Malla>(`/api/mallas/${encodeURIComponent(id)}/vigencia`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ vigente: true }),
    }),
  };
}
