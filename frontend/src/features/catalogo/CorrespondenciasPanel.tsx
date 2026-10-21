import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import type { CatalogoRequest } from './carreras-api';
import { elementos, guardarJson, jsonCatalogo, rutaRevision } from './comparacion-api';
import type { DetalleAsignatura, Revision, Vinculo } from './comparacion-api';
import { SelectorParAsignaturas } from './SelectorParAsignaturas';
export function CorrespondenciasPanel({ request, origenInicial }: { request: CatalogoRequest; origenInicial: string }) {
  const [par, setPar] = useState({ o: origenInicial, r: '' }), [datos, setDatos] = useState<DetalleAsignatura[]>([]);
  const [vinculos, setVinculos] = useState<{ contenidos: Vinculo[]; resultados: Vinculo[] }>({ contenidos: [], resultados: [] });
  const [error, setError] = useState(''), [aviso, setAviso] = useState(''), [ocupado, setOcupado] = useState(false), [intento, setIntento] = useState(0);
  const generacion = useRef(0);
  useEffect(() => {
    if (!par.o || !par.r) return;
    const c = new AbortController(); setOcupado(true); setError('');
    Promise.all([jsonCatalogo<DetalleAsignatura>(request, `/api/asignaturas/${par.o}`, { signal: c.signal }),
      jsonCatalogo<DetalleAsignatura>(request, `/api/asignaturas/${par.r}`, { signal: c.signal }),
      jsonCatalogo<Revision>(request, rutaRevision(par.o, par.r), { signal: c.signal })]).then(([o, r, revision]) => {
        if (c.signal.aborted) return;
        setDatos([o, r]); setVinculos(revision.vigente ? { contenidos: revision.contenidos, resultados: revision.resultados } : { contenidos: [], resultados: [] });
        if (revision.registrada && !revision.vigente) setAviso('Los programas cambiaron. Revisa las correspondencias de nuevo.');
      }).catch(e => { if (!c.signal.aborted) setError(e.message); }).finally(() => { if (!c.signal.aborted) setOcupado(false); });
    return () => c.abort();
  }, [par, request, intento]);
  function seleccionar(lado: 'o' | 'r', id: string) { generacion.current++; setPar(p => ({ ...p, [lado]: id })); setDatos([]); setError(''); setAviso(''); }
  function editar(tipo: 'contenidos' | 'resultados', receptor: string, campo: keyof Vinculo, valor: string) {
    setAviso(''); setVinculos(actual => {
      const previo = actual[tipo].find(v => v.elementoReceptor === receptor) ?? { elementoOrigen: '', elementoReceptor: receptor, ubicacionOrigen: '', ubicacionReceptora: '' };
      return { ...actual, [tipo]: [...actual[tipo].filter(v => v.elementoReceptor !== receptor), { ...previo, [campo]: valor }] };
    });
  }
  async function guardar(e: FormEvent) {
    e.preventDefault(); if (ocupado) return; setOcupado(true); setError(''); setAviso(''); const intentoActual = generacion.current;
    try {
      await jsonCatalogo(request, rutaRevision(par.o, par.r), guardarJson({ contenidos: vinculos.contenidos.filter(v => v.elementoOrigen), resultados: vinculos.resultados.filter(v => v.elementoOrigen) }));
      if (intentoActual === generacion.current) setAviso('Correspondencias revisadas y guardadas.');
    } catch (e) { if (intentoActual === generacion.current) setError(e instanceof Error ? e.message : 'No se pudo guardar.'); }
    finally { if (intentoActual === generacion.current) setOcupado(false); }
  }
  return <section className="catalogo-card"><h3>Correspondencias académicas del catálogo</h3>
    <p>Revisa ambos planes y registra evidencia. Guardar con todas las filas «Sin correspondencia» registra una revisión explícita con cobertura cero. No se generan coincidencias por nombres.</p>
    <SelectorParAsignaturas request={request} origen={par.o} receptora={par.r} seleccionar={seleccionar} disabled={ocupado} />
    {error && <p role="alert">{error} <button type="button" onClick={() => setIntento(i => i + 1)}>Reintentar revisión</button></p>}{aviso && <p role="status">{aviso}</p>}
    {ocupado && <p role="status">Procesando revisión…</p>}
    {datos.length === 2 && <form onSubmit={guardar}><fieldset disabled={ocupado}>
      {(['contenidos', 'resultados'] as const).map(tipo => <section key={tipo}><h4>{tipo === 'contenidos' ? 'Contenidos receptores' : 'Resultados receptores'}</h4>
        {elementos(datos[1]!, tipo).map(elemento => {
          const v = vinculos[tipo].find(v => v.elementoReceptor === elemento.id);
          return <div className="catalogo-card" key={elemento.id}><p>{elemento.texto}</p><label className="auth-label">Origen para {elemento.texto}<select className="auth-input" value={v?.elementoOrigen ?? ''} onChange={e => editar(tipo, elemento.id, 'elementoOrigen', e.target.value)}><option value="">Sin correspondencia acreditada</option>{elementos(datos[0]!, tipo).map(o => <option key={o.id} value={o.id}>{o.texto}</option>)}</select></label>
            <div className="catalogo-fields">{(['ubicacionOrigen', 'ubicacionReceptora'] as const).map(campo => <label className="auth-label" key={campo}>Evidencia {campo === 'ubicacionOrigen' ? 'origen' : 'receptora'} para {elemento.texto}<input className="auth-input" required={!!v?.elementoOrigen} maxLength={500} value={v?.[campo] ?? ''} onChange={e => editar(tipo, elemento.id, campo, e.target.value)} /></label>)}</div></div>;
        })}</section>)}
    </fieldset><button className="app-button app-button--primary" disabled={ocupado || par.o === par.r}>Guardar revisión de correspondencias</button></form>}
  </section>;
}
