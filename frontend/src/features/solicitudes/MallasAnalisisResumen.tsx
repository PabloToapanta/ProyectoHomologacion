export type MallaGuardada = { id: string; carreraId: string; nombre: string; vigenteAlAnalizar: boolean };
type AsignaturaMalla = { asignaturaAprobadaId: string | null; asignaturaCatalogoId: string | null; malla: MallaGuardada | null };
export type MallasGuardadas = { origenes: AsignaturaMalla[]; receptoras: AsignaturaMalla[] };

const describir = (a: AsignaturaMalla) => a.malla
  ? `${a.malla.nombre} (${a.malla.vigenteAlAnalizar ? 'vigente' : 'no vigente'} al analizar)`
  : 'versión no registrada';
const unir = (items: AsignaturaMalla[]) => [...new Set(items.map(describir))].join(' · ') || 'versión no registrada';

/** Presenta la instantánea recibida sin consultar Catálogo; también se imprime en el PDF. No muestra identificadores internos. */
export function MallasAnalisisResumen({ mallas }: { mallas?: MallasGuardadas | null }) {
  if (!mallas) return <p className="malla-resumen">Malla: versión no registrada.</p>;
  return <p className="malla-resumen" aria-label="Mallas guardadas del análisis">
    Malla de origen: {unir(mallas.origenes ?? [])}. Malla receptora: {unir(mallas.receptoras ?? [])}.
  </p>;
}
