import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { evaluar } from './api';
import type { Request } from './api';
import { SelectorParAsignaturas } from '../catalogo/SelectorParAsignaturas';
import { elementos, jsonCatalogo, rutaPerfil, rutaRevision } from '../catalogo/comparacion-api';
import type { DetalleAsignatura, PerfilComparacion, Revision, Vinculo } from '../catalogo/comparacion-api';
import './equivalencia.css';

type Resultado = { estado: string; compatibilidadAcademicaPct: number | null; compatibilidadHorariaPct: number | null; compatibilidadCreditosPct: number | null; umbralPct: number; justificacion: string; informacionFaltante: string[]; contenidosSinCobertura: string[]; resultadosAprendizajeSinCobertura: string[] };
export function EquivalenciaForm({ request }: { request: Request }) {
  const [par, setPar] = useState({ o: '', r: '' }), [resultado, setResultado] = useState<Resultado | null>(null);
  const [error, setError] = useState(''), [aviso, setAviso] = useState(''), [ocupado, setOcupado] = useState(false);
  const [programas, setProgramas] = useState<DetalleAsignatura[]>([]), [sintetico, setSintetico] = useState(false);
  const pendiente = useRef<AbortController | null>(null);
  useEffect(() => () => pendiente.current?.abort(), []);
  function seleccionar(lado: 'o' | 'r', id: string) {
    pendiente.current?.abort(); setOcupado(false); setPar(p => ({ ...p, [lado]: id })); setResultado(null); setProgramas([]); setAviso(''); setError(''); setSintetico(false);
  }
  async function comparar(e: FormEvent) {
    e.preventDefault(); if (ocupado || !par.o || !par.r) return;
    setResultado(null); setError(''); setAviso('');
    if (par.o === par.r) { setError('Selecciona dos asignaturas distintas.'); return; }
    const c = new AbortController(); pendiente.current = c; setOcupado(true);
    try {
      const [o, r, po, pr] = await Promise.all([
        jsonCatalogo<DetalleAsignatura>(request, `/api/asignaturas/${encodeURIComponent(par.o)}`, { signal: c.signal }),
        jsonCatalogo<DetalleAsignatura>(request, `/api/asignaturas/${encodeURIComponent(par.r)}`, { signal: c.signal }),
        jsonCatalogo<PerfilComparacion | null>(request, rutaPerfil(par.o), { signal: c.signal }),
        jsonCatalogo<PerfilComparacion | null>(request, rutaPerfil(par.r), { signal: c.signal }),
      ]);
      const revision = await jsonCatalogo<Revision>(request, rutaRevision(par.o, par.r), { signal: c.signal });
      if (c.signal.aborted) return;
      setProgramas([o, r]); setSintetico(!!(po?.esSintetico || pr?.esSintetico));
      const vigente = revision.registrada && revision.vigente;
      if (!vigente) setAviso('Revisa las correspondencias de este par en el catálogo: faltan o los programas cambiaron.');
      const datos = (a: DetalleAsignatura, p: PerfilComparacion | null) => ({ id: a.id, planAnalitico: p?.planAnalitico ?? null, horas: a.horas, creditos: a.creditos,
        unidadHoras: p?.unidadHoras ?? null, sistemaCreditos: p?.sistemaCreditos ?? null,
        contenidos: a.unidades.length ? elementos(a, 'contenidos').map(e => e.id) : null,
        resultadosAprendizaje: a.unidades.length ? elementos(a, 'resultados').map(e => e.id) : null });
      const vinculos = (items: Vinculo[]) => items.map(v => ({ asignaturaOrigen: o.id, elementoOrigen: v.elementoOrigen, elementoReceptor: v.elementoReceptor, evidencia: { ubicacionOrigen: v.ubicacionOrigen, ubicacionReceptora: v.ubicacionReceptora } }));
      // Escenario de compatibilidad entre programas con origen aprobado; no acredita a ningún estudiante.
      const res = await evaluar<Resultado>(request, 'evaluar', { origenes: [{ ...datos(o, po), aprobada: true }], receptora: datos(r, pr),
        correspondenciasContenidos: vigente ? vinculos(revision.contenidos) : null,
        correspondenciasResultadosAprendizaje: vigente ? vinculos(revision.resultados) : null }, c.signal);
      if (!c.signal.aborted) setResultado(res);
    } catch (e) { if (!c.signal.aborted) setError(e instanceof Error ? e.message : 'No se pudo comparar.'); }
    finally { if (!c.signal.aborted) setOcupado(false); }
  }
  const texto = (tipo: 'contenidos' | 'resultados', id: string) => elementos(programas[1] ?? null, tipo).find(e => e.id === id)?.texto ?? id;
  return <section className="catalogo-card equivalencia-simple"><h2>Comparar asignaturas</h2>
    <p>Selecciona dos asignaturas para consultar su compatibilidad académica, horaria y de créditos.</p>
    <form onSubmit={comparar}><SelectorParAsignaturas request={request} origen={par.o} receptora={par.r} seleccionar={seleccionar} disabled={ocupado} />
      <button className="app-button app-button--primary" disabled={ocupado || !par.o || !par.r}>{ocupado ? 'Comparando…' : 'Comparar'}</button>
    </form>
    <p className="section-description">Compara programas bajo el supuesto de una asignatura de origen aprobada. La aprobación, antigüedad y requisitos del estudiante se validan por separado.</p>
    {error && <p role="alert" className="auth-error">{error}</p>}
    {aviso && <p role="note">{aviso}</p>}
    {sintetico && <p role="note">Demostración con datos sintéticos. No corresponde a programas ni equivalencias oficiales.</p>}
    {resultado && <div role="status"><h3>{({ Homologable: 'Se sugiere homologable', NoHomologable: 'No alcanza la equivalencia requerida', InformacionIncompleta: 'Información incompleta' })[resultado.estado] ?? resultado.estado}</h3>
      <p>Umbral: {resultado.umbralPct} % por criterio.</p>
      <table className="equivalencia-tabla"><caption>Compatibilidad de las asignaturas</caption><thead><tr><th>Criterio</th><th>Compatibilidad</th><th>%</th></tr></thead><tbody>
        {([['Académica combinada', resultado.compatibilidadAcademicaPct], ['Carga horaria', resultado.compatibilidadHorariaPct], ['Créditos', resultado.compatibilidadCreditosPct]] as const).map(([label, value]) => <tr key={label}><th scope="row">{label}</th><td>{value !== null ? <progress aria-label={label} max={100} value={value} /> : 'Sin determinar'}</td><td>{value !== null ? `${Number(value.toFixed(2))} %` : '—'}</td></tr>)}
      </tbody></table>
      <p>{resultado.justificacion}</p>
      <details><summary>Ver fundamento y datos utilizados</summary>
        {programas.map(a => <p key={a.id}>{a.nombre}: {a.horas} horas · {a.creditos} créditos.</p>)}
        <ul>{resultado.informacionFaltante.map(m => <li key={m}>{m}</li>)}</ul>
        <h4>Contenidos sin cobertura acreditada</h4><ul>{resultado.contenidosSinCobertura.map(id => <li key={id}>{texto('contenidos', id)}</li>)}</ul>
        <h4>Resultados sin cobertura acreditada</h4><ul>{resultado.resultadosAprendizajeSinCobertura.map(id => <li key={id}>{texto('resultados', id)}</li>)}</ul>
      </details>
    </div>}
  </section>;
}
