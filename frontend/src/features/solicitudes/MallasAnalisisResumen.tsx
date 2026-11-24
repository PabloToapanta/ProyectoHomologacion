export type MallaGuardada = { id: string; carreraId: string; nombre: string; vigenteAlAnalizar: boolean };
type AsignaturaMalla = { asignaturaAprobadaId: string | null; asignaturaCatalogoId: string | null; malla: MallaGuardada | null };
export type MallasGuardadas = { origenes: AsignaturaMalla[]; receptoras: AsignaturaMalla[] };

/** Presenta la instantánea recibida sin consultar Catálogo; también se imprime en el PDF. */
export function MallasAnalisisResumen({ mallas }: { mallas?: MallasGuardadas | null }) {
  if (!mallas) return <p>Versión no registrada.</p>;
  const lista = (items: AsignaturaMalla[], etiqueta: string) => items.map((a, i) => <li key={`${etiqueta}/${a.asignaturaAprobadaId ?? a.asignaturaCatalogoId ?? i}`}>
    {etiqueta} {a.asignaturaAprobadaId ?? a.asignaturaCatalogoId ?? i + 1}: {a.malla
      ? `${a.malla.nombre} · ${a.malla.id} · ${a.malla.vigenteAlAnalizar ? 'Vigente al analizar' : 'No vigente al analizar'}`
      : 'versión no registrada'}
  </li>);
  return <section aria-label="Mallas guardadas del análisis"><h5>Versiones de malla al analizar</h5>
    <ul>{lista(mallas.origenes ?? [], 'Origen')}{lista(mallas.receptoras ?? [], 'Receptora')}</ul>
  </section>;
}
