export type Request = (path: string, init?: RequestInit) => Promise<Response>;
export type EstadoNormativa = 'Vigente' | 'Reformada' | 'Derogada';
export type Referencia = { id: string; codigo: string };
export type Normativa = {
  id: string; codigo: string; titulo: string; emisor: string; fechaEmision: string; vigenteDesde: string;
  estado: EstadoNormativa; reformaA: Referencia | null; reformadaPor: Referencia | null;
  motivoDerogacion: string | null; fechaDerogacion: string | null; nombreArchivo: string; tamano: number; sha256: string;
  fechaRegistro: string; registradaPor: string;
};
export type EventoNormativa = { id: string; tipo: string; fecha: string; usuario: string; detalle: string | null };

export const base = '/api/normativas';

/** Mensaje del servidor (ProblemDetails) o uno por defecto, sin exponer detalles técnicos. */
export async function leer<T>(r: Response, defecto: string): Promise<T> {
  if (!r.ok) {
    let texto = defecto;
    try { const p = await r.json(); texto = p.detail ?? p.title ?? texto; } catch { /* sin cuerpo */ }
    if (r.status === 401) texto = 'La sesión expiró. Vuelve a iniciar sesión.';
    if (r.status === 403) texto = 'Tu rol no permite esta acción.';
    throw new Error(texto);
  }
  return (r.status === 204 ? undefined : await r.json()) as T;
}

/** «RES-1 — Título»: formato con el que Configuración guarda la normativa de respaldo. */
export const respaldoDe = (n: Pick<Normativa, 'codigo' | 'titulo'>) => `${n.codigo} — ${n.titulo}`;

/** Busca la normativa citada al inicio de un respaldo guardado; los respaldos antiguos en texto libre no coinciden. */
export function normativaDelRespaldo(respaldo: string | undefined, normativas: Normativa[]) {
  if (!respaldo) return null;
  const texto = respaldo.trim().toUpperCase();
  return normativas.filter(n => texto === n.codigo.toUpperCase() || texto.startsWith(`${n.codigo.toUpperCase()} — `))
    .sort((a, b) => b.codigo.length - a.codigo.length)[0] ?? null;
}
