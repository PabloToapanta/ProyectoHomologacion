import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import type { CatalogoRequest } from './carreras-api';
import { guardarJson, jsonCatalogo, rutaPerfil } from './comparacion-api';
import type { PerfilComparacion } from './comparacion-api';
import { CorrespondenciasPanel } from './CorrespondenciasPanel';
const vacio: PerfilComparacion = { planAnalitico: '', unidadHoras: '', sistemaCreditos: '', esSintetico: false };
export function PerfilComparacionPanel({ id, nombre, request, cerrar }: { id: string; nombre: string; request: CatalogoRequest; cerrar: () => void }) {
  const [perfil, setPerfil] = useState(vacio), [cargando, setCargando] = useState(true), [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState(''), [aviso, setAviso] = useState(''), [revision, setRevision] = useState(false), [intento, setIntento] = useState(0);
  useEffect(() => {
    const c = new AbortController(); setCargando(true); setError('');
    jsonCatalogo<PerfilComparacion | null>(request, rutaPerfil(id), { signal: c.signal }).then(p => { if (!c.signal.aborted) setPerfil(p ?? vacio); })
      .catch(e => { if (!c.signal.aborted) setError(e.message); }).finally(() => { if (!c.signal.aborted) setCargando(false); });
    return () => c.abort();
  }, [id, request, intento]);
  async function guardar(e: FormEvent) {
    e.preventDefault(); if (ocupado) return; setOcupado(true); setError(''); setAviso('');
    try { await jsonCatalogo(request, rutaPerfil(id), guardarJson(perfil)); setAviso('Perfil guardado.'); setRevision(false); }
    catch (e) { setError(e instanceof Error ? e.message : 'No se pudo guardar.'); } finally { setOcupado(false); }
  }
  return <section className="catalogo-card" aria-label="Perfil de comparación">
    <div className="catalogo-list-heading"><h2>Perfil de comparación · {nombre}</h2><button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={cerrar}>Cerrar perfil</button></div>
    <p>Estos datos se guardan una sola vez y se reutilizan al comparar. Una modificación exige revisar las correspondencias existentes.</p>
    {error && <p role="alert">{error} <button type="button" onClick={() => setIntento(i => i + 1)}>Recargar perfil</button></p>}
    {aviso && <p role="status">{aviso}</p>}
    {cargando ? <p role="status">Cargando perfil…</p> : <form onSubmit={guardar} onChange={() => setAviso('')}><fieldset className="catalogo-fields" disabled={ocupado}>
      {([['planAnalitico', 'Referencia al plan analítico', 500], ['unidadHoras', 'Unidad de horas', 100], ['sistemaCreditos', 'Sistema de créditos', 100]] as const).map(([key, label, max]) => <label key={key} className="auth-label">{label}<input className="auth-input" required maxLength={max} value={perfil[key]} onChange={e => setPerfil(p => ({ ...p, [key]: e.target.value }))} /></label>)}
      <label><input type="checkbox" checked={perfil.esSintetico} onChange={e => setPerfil(p => ({ ...p, esSintetico: e.target.checked }))} /> Datos sintéticos de demostración</label>
    </fieldset><button className="app-button app-button--primary" disabled={ocupado}>{ocupado ? 'Guardando…' : 'Guardar perfil'}</button></form>}
    <p>Los contenidos y resultados se mantienen en «Ver unidades». Aquí se guarda la evidencia de correspondencia entre programas; no la aprobación de estudiantes.</p>
    <button type="button" className="app-button app-button--secondary" disabled={ocupado || cargando || !!error} onClick={() => setRevision(v => !v)}>Revisar correspondencias</button>
    {revision && <CorrespondenciasPanel request={request} origenInicial={id} />}
  </section>;
}
