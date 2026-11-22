export interface ParametrosConfiguracion {
  versionId: string;
  umbralPct: number;
  antiguedadMaximaAnios: number;
  periodosMinimosInterno: number;
  asignaturasHomologablesMinimasInterno: number;
  periodosMinimosExterno: number;
  asignaturasHomologablesMinimasExterno: number;
}
export type CambioConfiguracion = Omit<ParametrosConfiguracion, 'versionId'> & { motivo: string; respaldo: string };
export interface EntradaHistorial {
  versionId: string;
  versionAnteriorId: string | null;
  fechaHoraUtc: string;
  actorId: string;
  anterior: ParametrosConfiguracion | null;
  nueva: ParametrosConfiguracion;
  motivo: string;
  respaldo: string;
}
export interface PaginaHistorial { entradas: EntradaHistorial[]; cursorSiguiente: string | null }
export type ConfiguracionRequest = (path: string, init?: RequestInit) => Promise<Response>;

export class ConfiguracionApiError extends Error {
  constructor(public readonly status: number, message: string) { super(message); }
}

const ruta = '/api/homologaciones/configuracion';
async function revisar(response: Response): Promise<Response> {
  if (response.ok) return response;
  let detail = '';
  try { const body = await response.json() as { detail?: string; title?: string }; detail = body.detail || body.title || ''; }
  catch { /* La respuesta puede venir vacía del Gateway. */ }
  const fallback: Record<number, string> = {
    400: 'Revisa los parámetros, el motivo y el respaldo.',
    401: 'Tu sesión ha finalizado.', 403: 'Solo el Coordinador puede gestionar la configuración.',
    409: 'La configuración cambió. Revisa los valores vigentes antes de guardar de nuevo.',
    503: 'La configuración no está disponible en este momento.'
  };
  throw new ConfiguracionApiError(response.status, detail || fallback[response.status] || 'No se pudo completar la operación.');
}

export function configuracionApi(request: ConfiguracionRequest) {
  return {
    async obtener(signal?: AbortSignal) {
      const response = await revisar(await request(ruta, { signal }));
      const etag = response.headers.get('ETag');
      if (!etag) throw new ConfiguracionApiError(503, 'La respuesta no incluyó la versión de configuración.');
      return { parametros: await response.json() as ParametrosConfiguracion, etag };
    },
    async actualizar(cambio: CambioConfiguracion, etag: string) {
      const response = await revisar(await request(ruta, {
        method: 'PUT', headers: { 'Content-Type': 'application/json', 'If-Match': etag }, body: JSON.stringify(cambio)
      }));
      const nuevoEtag = response.headers.get('ETag');
      if (!nuevoEtag) throw new ConfiguracionApiError(503, 'La respuesta no incluyó la nueva versión.');
      return { parametros: await response.json() as ParametrosConfiguracion, etag: nuevoEtag };
    },
    async historial(cursor?: string | null, signal?: AbortSignal) {
      const query = new URLSearchParams({ limit: '50' });
      if (cursor) query.set('cursor', cursor);
      const response = await revisar(await request(`${ruta}/historial?${query}`, { signal }));
      return await response.json() as PaginaHistorial;
    }
  };
}
