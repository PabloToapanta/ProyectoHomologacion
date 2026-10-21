import { useEffect, useState } from 'react';
import { carrerasApi } from './carreras-api';
import { asignaturasApi } from './asignaturas-api';
import type { CatalogoRequest } from './carreras-api';
import type { OpcionAsignatura } from './comparacion-api';
export function SelectorParAsignaturas({ request, origen, receptora, seleccionar, disabled = false }: {
  request: CatalogoRequest; origen: string; receptora: string; seleccionar: (lado: 'o' | 'r', id: string) => void; disabled?: boolean;
}) {
  const [opciones, setOpciones] = useState<OpcionAsignatura[]>([]), [error, setError] = useState('');
  const [cargando, setCargando] = useState(true), [intento, setIntento] = useState(0);
  useEffect(() => {
    const controller = new AbortController(); setCargando(true); setError('');
    async function cargar() {
      const carreras = await carrerasApi(request).listar(controller.signal);
      const grupos = await Promise.all(carreras.map(async c => (await asignaturasApi(request).listar(c.id, controller.signal)).map(a => ({ ...a, carrera: c.nombre }))));
      if (!controller.signal.aborted) setOpciones(grupos.flat());
    }
    cargar().catch(e => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'No se pudo cargar el catálogo.'); })
      .finally(() => { if (!controller.signal.aborted) setCargando(false); });
    return () => controller.abort();
  }, [request, intento]);
  return <>
    {cargando && <p role="status">Cargando catálogo…</p>}
    {error && <p role="alert">{error} <button type="button" onClick={() => setIntento(i => i + 1)}>Reintentar catálogo</button></p>}
    {!cargando && !error && !opciones.length && <p>No hay asignaturas registradas. Agrégalas en el catálogo.</p>}
    <div className="catalogo-fields selector-par">{(['o', 'r'] as const).map(lado => <label key={lado} className="auth-label">Asignatura {lado === 'o' ? 'origen' : 'receptora'}<select aria-label={`Asignatura ${lado === 'o' ? 'origen' : 'receptora'}`} className="auth-input" disabled={disabled || cargando || !!error} value={lado === 'o' ? origen : receptora} onChange={e => seleccionar(lado, e.target.value)}>
      <option value="">Selecciona una asignatura</option>{[...new Set(opciones.map(a => a.carrera))].map(c => <optgroup label={c} key={c}>{opciones.filter(a => a.carrera === c).map(a => <option key={a.id} value={a.id}>{a.codigo} · {a.nombre} — {c}</option>)}</optgroup>)}
    </select></label>)}</div>
  </>;
}
