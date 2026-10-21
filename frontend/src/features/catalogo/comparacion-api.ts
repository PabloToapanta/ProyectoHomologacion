import type { CatalogoRequest } from './carreras-api';
import type { Asignatura } from './asignaturas-api';
export interface PerfilComparacion { planAnalitico: string; unidadHoras: string; sistemaCreditos: string; esSintetico: boolean }
export interface Vinculo { elementoOrigen: string; elementoReceptor: string; ubicacionOrigen: string; ubicacionReceptora: string }
export interface Revision { registrada: boolean; vigente: boolean; contenidos: Vinculo[]; resultados: Vinculo[] }
export interface DetalleAsignatura extends Asignatura { unidades: { id: string; numero: number; resultadoAprendizaje: string; contenidos: { id: string; tema: string }[] }[] }
export interface OpcionAsignatura extends Asignatura { carrera: string }
export async function jsonCatalogo<T>(request: CatalogoRequest, ruta: string, init?: RequestInit): Promise<T> {
  const response = await request(ruta, init);
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(response.status === 403 ? 'No tienes permiso para consultar o editar el catálogo.' : problem?.detail || 'No se pudo consultar o guardar el catálogo. Vuelve a intentarlo.');
  }
  return response.status === 204 ? null as T : response.json() as Promise<T>;
}
export const rutaPerfil = (id: string) => `/api/asignaturas/${encodeURIComponent(id)}/perfil-comparacion`;
export const rutaRevision = (o: string, r: string) => `/api/asignaturas/${encodeURIComponent(o)}/correspondencias/${encodeURIComponent(r)}`;
export const guardarJson = (datos: unknown): RequestInit => ({ method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(datos) });
export function elementos(a: DetalleAsignatura | null, tipo: 'contenidos' | 'resultados') {
  return a?.unidades.flatMap(u => tipo === 'contenidos' ? u.contenidos.map(c => ({ id: c.id, texto: c.tema }))
    : [{ id: u.id, texto: `U.${u.numero} · ${u.resultadoAprendizaje}` }]) ?? [];
}
