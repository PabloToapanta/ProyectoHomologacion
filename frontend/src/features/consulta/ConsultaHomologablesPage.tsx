import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';

type Request = (path: string, init?: RequestInit) => Promise<Response>;
type Carrera = { id: string; nombre: string; facultad: string; vigencia: boolean };
type Asignatura = { id: string; codigo: string; nombre: string; nivel: number; malla?: { id: string; nombre: string; vigente: boolean } | null };
type Par = {
  origen: Asignatura; receptora: Asignatura; estado: 'Homologable' | 'NoHomologable' | 'InformacionIncompleta';
  porcentajeAcademico: number | null; porcentajeHoras: number | null; porcentajeCreditos: number | null; umbralPct: number;
  justificacion: string; informacionFaltante: string[];
};
type Resultado = { origen: Carrera; receptora: Carrera; pares: Par[]; advertencias: string[] };

const base = '/api/homologaciones/consulta-homologables';
const estados = { Homologable: 'Homologable', NoHomologable: 'No homologable', InformacionIncompleta: 'Información incompleta' } as const;
const pct = (v: number | null) => v === null ? '—' : `${v.toLocaleString('es-EC', { maximumFractionDigits: 2 })} %`;
const malla = (a: Asignatura) => a.malla ? ` · ${a.malla.nombre}${a.malla.vigente ? '' : ' (no vigente)'}` : '';
async function leer<T>(r: Response, defecto: string): Promise<T> {
  if (!r.ok) {
    let texto = defecto;
    try { const p = await r.json(); texto = p.detail ?? p.title ?? texto; } catch { /* sin cuerpo */ }
    throw new Error(texto);
  }
  return r.json() as Promise<T>;
}

/** HU-09: consulta informativa del motor determinista entre dos carreras FISEI. No crea correspondencias ni llama al agente. */
export function ConsultaHomologablesPage({ request }: { request: Request }) {
  const [carreras, setCarreras] = useState<Carrera[]>([]);
  const [origen, setOrigen] = useState('');
  const [receptora, setReceptora] = useState('');
  const [resultado, setResultado] = useState<Resultado | null>(null);
  const [cargando, setCargando] = useState(true);
  const [consultando, setConsultando] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const controller = new AbortController();
    request(`${base}/carreras`, { signal: controller.signal })
      .then(r => leer<Carrera[]>(r, 'No se pudieron consultar las carreras.'))
      .then(c => { if (!controller.signal.aborted) setCarreras(c); })
      .catch(e => { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'No se pudieron consultar las carreras.'); })
      .finally(() => { if (!controller.signal.aborted) setCargando(false); });
    return () => controller.abort();
  }, [request]);

  async function consultar(event: FormEvent) {
    event.preventDefault();
    if (consultando) return;
    if (!origen || !receptora) { setError('Seleccione la carrera de origen y la receptora.'); return; }
    if (origen === receptora) { setError('Seleccione dos carreras distintas.'); return; }
    setConsultando(true); setError(''); setResultado(null);
    try {
      const q = new URLSearchParams({ origen, receptora });
      setResultado(await leer<Resultado>(await request(`${base}?${q}`), 'No se pudo realizar la consulta.'));
    } catch (e) { setError(e instanceof Error ? e.message : 'No se pudo realizar la consulta.'); }
    finally { setConsultando(false); }
  }

  const opciones = carreras.map(c => <option key={c.id} value={c.id}>{c.nombre}{c.vigencia ? '' : ' (no vigente)'}</option>);
  return <section aria-labelledby="consulta-titulo">
    <p className="eyebrow">CONSULTA ACADÉMICA</p>
    <h1 id="consulta-titulo">Materias homologables entre carreras FISEI</h1>
    <p>El motor compara los pares con correspondencias revisadas en el catálogo, con horas y créditos del catálogo. Es una consulta informativa: no es una homologación ni una aprobación oficial, y no usa el agente de IA.</p>
    {cargando && <p role="status">Cargando carreras…</p>}
    {error && <p role="alert">{error}</p>}
    {!cargando && <form onSubmit={consultar} noValidate>
      <label className="auth-label">Carrera de origen<select className="auth-input" value={origen} disabled={consultando}
        onChange={e => { setOrigen(e.target.value); setResultado(null); }}><option value="">Seleccione</option>{opciones}</select></label>
      <label className="auth-label">Carrera receptora<select className="auth-input" value={receptora} disabled={consultando}
        onChange={e => { setReceptora(e.target.value); setResultado(null); }}><option value="">Seleccione</option>{opciones}</select></label>
      <button className="app-button app-button--primary" disabled={consultando}>{consultando ? 'Consultando…' : 'Consultar'}</button>
    </form>}
    {resultado && <>
      <h2>{resultado.origen.nombre} → {resultado.receptora.nombre}</h2>
      {resultado.pares.length === 0
        ? <p>No hay pares con correspondencias revisadas entre estas carreras. No se infieren coincidencias sin revisión.</p>
        : <div className="solicitudes-table"><table><caption>Pares con correspondencias revisadas</caption>
          <thead><tr><th>Asignatura de origen</th><th>Asignatura receptora</th><th>Académico</th><th>Horas</th><th>Créditos</th><th>Resultado del motor</th></tr></thead>
          <tbody>{resultado.pares.map(p => <tr key={`${p.origen.id}/${p.receptora.id}`}>
            <td>{p.origen.codigo} · {p.origen.nombre}{malla(p.origen)}</td>
            <td>{p.receptora.codigo} · {p.receptora.nombre}</td>
            <td>{pct(p.porcentajeAcademico)}</td><td>{pct(p.porcentajeHoras)}</td><td>{pct(p.porcentajeCreditos)}</td>
            <td><strong>{estados[p.estado]}</strong> (umbral {p.umbralPct} %)
              {p.informacionFaltante.length > 0 && <ul>{p.informacionFaltante.map(f => <li key={f}>{f}</li>)}</ul>}</td>
          </tr>)}</tbody></table></div>}
      {resultado.advertencias.map(a => <p key={a}>{a}</p>)}
    </>}
  </section>;
}
