import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { evaluar } from './api';
import type { Request } from './api';

const lineas = (s: string) => s.split('\n').map(v => v.trim()).filter(Boolean);
type Resultado = { estado: string; homologable: boolean; compatibilidadAcademicaPct: number | null; compatibilidadHorariaPct: number | null; compatibilidadCreditosPct: number | null; umbralPct: number; justificacion: string; informacionFaltante: string[]; contenidosSinCobertura: string[]; resultadosAprendizajeSinCobertura: string[] };
export function EquivalenciaForm({ request }: { request: Request }) {
  const [inventarios, setInventarios] = useState({ oc: '', or: '', rc: '', rr: '' });
  const [resultado, setResultado] = useState<Resultado | null>(null);
  const [error, setError] = useState(''); const [ocupado, setOcupado] = useState(false);
  const pendiente = useRef<AbortController | null>(null);
  useEffect(() => () => pendiente.current?.abort(), []);
  function limpiarResultado() { setResultado(null); setError(''); }
  async function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); if (ocupado) return;
    const f = new FormData(e.currentTarget);
    const datos = (prefix: string) => ({ id: String(f.get(`${prefix}id`)).trim(), planAnalitico: String(f.get(`${prefix}plan`)).trim(), horas: Number(f.get(`${prefix}horas`)), unidadHoras: String(f.get(`${prefix}unidad`)).trim(), creditos: Number(f.get(`${prefix}creditos`)), sistemaCreditos: String(f.get(`${prefix}sistema`)).trim(), contenidos: lineas(inventarios[prefix === 'o' ? 'oc' : 'rc']), resultadosAprendizaje: lineas(inventarios[prefix === 'o' ? 'or' : 'rr']) });
    const origen = datos('o'), receptora = datos('r');
    if (Object.values(inventarios).some(v => new Set(lineas(v)).size !== lineas(v).length)) { setError('Cada inventario debe tener elementos distintos.'); return; }
    const correspondencias = (tipo: 'c' | 'r') => lineas(inventarios[tipo === 'c' ? 'rc' : 'rr']).flatMap((receptor, i) => {
      const origenElemento = f.get(`${tipo}${i}origen`);
      return origenElemento ? [{ asignaturaOrigen: origen.id, elementoOrigen: origenElemento, elementoReceptor: receptor, evidencia: { ubicacionOrigen: f.get(`${tipo}${i}eo`), ubicacionReceptora: f.get(`${tipo}${i}er`) } }] : [];
    });
    const controller = new AbortController(); pendiente.current = controller; setOcupado(true); limpiarResultado();
    try { const r = await evaluar<Resultado>(request, 'evaluar', { origenes: [{ ...origen, aprobada: f.get('aprobada') === 'si' }], receptora, correspondenciasContenidos: correspondencias('c'), correspondenciasResultadosAprendizaje: correspondencias('r') }, controller.signal); if (!controller.signal.aborted) setResultado(r); }
    catch (e) { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'No hay conexión con el servicio.'); }
    finally { if (!controller.signal.aborted) setOcupado(false); }
  }
  return <section className="catalogo-card"><h2>Equivalencia de asignaturas</h2><p>Compara un origen con una receptora. Registra elementos de ambos planes y selecciona las correspondencias con evidencia. No se generan coincidencias automáticamente.</p>
    <form onSubmit={enviar} onChange={limpiarResultado}><fieldset disabled={ocupado}>
      {(['o', 'r'] as const).map(prefix => <section className="catalogo-card" key={prefix}><h3>{prefix === 'o' ? 'Asignatura de origen' : 'Asignatura receptora'}</h3><div className="catalogo-fields">
        {[["id", "Identificador"], ["plan", "Referencia al plan analítico"], ["unidad", "Unidad de horas"], ["sistema", "Sistema de créditos"]].map(([key, label]) => <label className="auth-label" key={key}>{label} ({prefix === 'o' ? 'origen' : 'receptora'})<input className="auth-input" name={`${prefix}${key}`} required maxLength={500} /></label>)}
        {['horas', 'creditos'].map(key => <label className="auth-label" key={key}>{key === 'horas' ? 'Horas' : 'Créditos'} ({prefix === 'o' ? 'origen' : 'receptora'})<input className="auth-input" name={`${prefix}${key}`} type="number" min="0.0001" step="any" required /></label>)}
        {prefix === 'o' && <label className="auth-label">Consta aprobada<select className="auth-input" name="aprobada" required><option value="">Selecciona</option><option value="si">Sí</option><option value="no">No</option></select></label>}
        {(['c', 'r'] as const).map(tipo => { const key = `${prefix}${tipo}` as keyof typeof inventarios; return <label className="auth-label" key={key}>{tipo === 'c' ? 'Contenidos' : 'Resultados de aprendizaje'} ({prefix === 'o' ? 'origen' : 'receptora'}, uno por línea)<textarea className="auth-input" rows={4} maxLength={20000} value={inventarios[key]} onChange={e => setInventarios({ ...inventarios, [key]: e.target.value })} /></label>; })}
      </div></section>)}
      <h3>Correspondencias y evidencia</h3><p>Deja «Sin correspondencia acreditada» cuando no haya evidencia. Las ubicaciones deben indicar página, unidad o sección en ambos planes.</p>
      {(['c', 'r'] as const).map(tipo => <section key={tipo}><h4>{tipo === 'c' ? 'Contenidos receptores' : 'Resultados receptores'}</h4>{lineas(inventarios[tipo === 'c' ? 'rc' : 'rr']).map((elemento, i) => <div className="catalogo-card" key={`${tipo}-${elemento}`}><p>{elemento}</p><label className="auth-label">Elemento de origen para {elemento}<select className="auth-input" name={`${tipo}${i}origen`}><option value="">Sin correspondencia acreditada</option>{lineas(inventarios[tipo === 'c' ? 'oc' : 'or']).map(o => <option key={o}>{o}</option>)}</select></label><div className="catalogo-fields">{['eo', 'er'].map(extremo => <label key={extremo} className="auth-label">Ubicación de evidencia {extremo === 'eo' ? 'origen' : 'receptora'} para {elemento}<input className="auth-input" name={`${tipo}${i}${extremo}`} maxLength={500} /></label>)}</div></div>)}</section>)}
    </fieldset><button className="app-button app-button--primary" disabled={ocupado}>{ocupado ? 'Evaluando…' : 'Evaluar equivalencia'}</button></form>
    {error && <p role="alert" className="auth-error">{error}</p>}{resultado && <div role="status"><h3>{({ Homologable: 'Se sugiere homologable', NoHomologable: 'No alcanza la equivalencia requerida', InformacionIncompleta: 'Información incompleta' })[resultado.estado] || resultado.estado}</h3><p>{resultado.justificacion}</p><dl>{[['Académica', resultado.compatibilidadAcademicaPct], ['Horaria', resultado.compatibilidadHorariaPct], ['Créditos', resultado.compatibilidadCreditosPct]].map(([label, value]) => <div key={String(label)}><dt>{label}</dt><dd>{value === null ? 'Sin determinar' : `${value} %`}</dd></div>)}</dl><p>Umbral: {resultado.umbralPct} % por criterio.</p><ul>{resultado.informacionFaltante.map(m => <li key={m}>{m}</li>)}</ul><h4>Contenidos sin cobertura acreditada</h4><ul>{resultado.contenidosSinCobertura.map(m => <li key={m}>{m}</li>)}</ul><h4>Resultados sin cobertura acreditada</h4><ul>{resultado.resultadosAprendizajeSinCobertura.map(m => <li key={m}>{m}</li>)}</ul></div>}
  </section>;
}
