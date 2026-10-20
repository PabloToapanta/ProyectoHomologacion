import { useEffect, useMemo, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { carrerasApi } from './carreras-api';
import type { Carrera, CatalogoRequest } from './carreras-api';
import { asignaturasApi } from './asignaturas-api';
import type { Asignatura } from './asignaturas-api';
import './catalogo.css';

const vacio = { codigo: '', nombre: '', nivel: '', creditos: '', horas: '' };
const mensaje = (e: unknown) => e instanceof Error ? e.message : 'No se pudo completar la operación.';

export function AsignaturasPage({ request }: { request: CatalogoRequest }) {
  const apiCarreras = useMemo(() => carrerasApi(request), [request]);
  const api = useMemo(() => asignaturasApi(request), [request]);
  const [carreras, setCarreras] = useState<Carrera[]>([]);
  const [carreraId, setCarreraId] = useState('');
  const [asignaturas, setAsignaturas] = useState<Asignatura[]>([]);
  const [cargandoCarreras, setCargandoCarreras] = useState(true);
  const [cargando, setCargando] = useState(false);
  const [errorCarreras, setErrorCarreras] = useState('');
  const [errorListado, setErrorListado] = useState('');
  const [error, setError] = useState('');
  const [aviso, setAviso] = useState('');
  const [intentoCarreras, setIntentoCarreras] = useState(0);
  const [intento, setIntento] = useState(0);
  const [form, setForm] = useState<typeof vacio | null>(null);
  const [guardando, setGuardando] = useState(false);
  const codigoRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const controller = new AbortController();
    setCargandoCarreras(true); setErrorCarreras('');
    apiCarreras.listar(controller.signal).then(items => {
      if (!controller.signal.aborted) setCarreras(items);
    }).catch(e => { if (!controller.signal.aborted) setErrorCarreras(mensaje(e)); })
      .finally(() => { if (!controller.signal.aborted) setCargandoCarreras(false); });
    return () => controller.abort();
  }, [apiCarreras, intentoCarreras]);

  useEffect(() => {
    const controller = new AbortController();
    setAsignaturas([]); setErrorListado(''); setError(''); setAviso(''); setForm(null);
    if (!carreraId) { setCargando(false); return () => controller.abort(); }
    setCargando(true);
    api.listar(carreraId, controller.signal).then(items => {
      if (!controller.signal.aborted) setAsignaturas(items);
    }).catch(e => { if (!controller.signal.aborted) setErrorListado(mensaje(e)); })
      .finally(() => { if (!controller.signal.aborted) setCargando(false); });
    return () => controller.abort();
  }, [api, carreraId, intento]);

  useEffect(() => { if (form) codigoRef.current?.focus(); }, [form !== null]);

  async function guardar(event: FormEvent) {
    event.preventDefault();
    if (!form || guardando || !carreraId) return;
    const nivel = Number(form.nivel), creditos = Number(form.creditos), horas = Number(form.horas);
    if (!form.codigo.trim() || !form.nombre.trim()) { setError('Código y nombre son obligatorios.'); return; }
    if (!Number.isSafeInteger(nivel) || !Number.isSafeInteger(horas) || nivel <= 0 || horas <= 0 || !Number.isFinite(creditos) || creditos <= 0) {
      setError('Nivel, créditos y horas deben ser positivos; nivel y horas deben ser enteros.'); return;
    }
    if (nivel > 2147483647 || horas > 2147483647 || creditos > 9999999.99 || !/^\d+(\.\d{1,2})?$/.test(form.creditos)) {
      setError('Revisa los límites de nivel y horas. Créditos admite hasta dos decimales.'); return;
    }
    setGuardando(true); setError(''); setAviso('');
    try {
      const creada = await api.crear({ carreraId, codigo: form.codigo.trim(), nombre: form.nombre.trim(), nivel, creditos, horas });
      setAsignaturas(items => [...items, creada]);
      setForm(null); setAviso('Asignatura registrada correctamente.');
    } catch (e) { setError(mensaje(e)); }
    finally { setGuardando(false); }
  }

  return <main className="workspace-main catalogo">
    <p className="eyebrow">ADMINISTRACIÓN / CATÁLOGO ACADÉMICO</p>
    <div className="workspace-heading"><div><h1>Gestión de asignaturas</h1><p>Consulta las asignaturas por carrera y registra sus datos académicos.</p></div>
      <button className="app-button app-button--primary" disabled={!carreraId || cargando || !!errorListado || guardando || !!form} onClick={() => { setForm({ ...vacio }); setError(''); setAviso(''); }}>Nueva asignatura</button></div>
    {cargandoCarreras ? <p role="status">Cargando carreras…</p> : errorCarreras ? <div><p role="alert" className="auth-error">{errorCarreras}</p><button className="app-button app-button--secondary" onClick={() => setIntentoCarreras(i => i + 1)}>Reintentar carreras</button></div>
      : carreras.length === 0 ? <p className="catalogo-empty">Registra una carrera antes de agregar asignaturas.</p>
      : <label className="auth-label catalogo-select-label">Carrera<select className="auth-input" disabled={guardando} value={carreraId} onChange={e => setCarreraId(e.target.value)}>
        <option value="">Selecciona una carrera</option>{carreras.map(c => <option key={c.id} value={c.id}>{c.nombre} · {c.facultad}{c.vigencia ? '' : ' (no vigente)'}</option>)}
      </select></label>}
    {aviso && <p role="status" className="catalogo-aviso">{aviso}</p>}
    {error && <p role="alert" className="auth-error">{error}</p>}
    {form && <section className="catalogo-card" aria-labelledby="asignatura-form-title"><h2 id="asignatura-form-title">Nueva asignatura</h2>
      <form onSubmit={guardar} noValidate><fieldset className="catalogo-fields" disabled={guardando}>
        <label className="auth-label">Código<input ref={codigoRef} className="auth-input" required maxLength={50} value={form.codigo} onChange={e => setForm({ ...form, codigo: e.target.value })} /></label>
        <label className="auth-label">Nombre<input className="auth-input" required maxLength={200} value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} /></label>
        <label className="auth-label">Nivel<input className="auth-input" type="number" required min="1" max="2147483647" step="1" value={form.nivel} onChange={e => setForm({ ...form, nivel: e.target.value })} /></label>
        <label className="auth-label">Créditos<input className="auth-input" type="number" required min="0.01" max="9999999.99" step="0.01" value={form.creditos} onChange={e => setForm({ ...form, creditos: e.target.value })} /></label>
        <label className="auth-label">Horas<input className="auth-input" type="number" required min="1" max="2147483647" step="1" value={form.horas} onChange={e => setForm({ ...form, horas: e.target.value })} /></label>
      </fieldset><div className="catalogo-actions"><button className="app-button app-button--primary" disabled={guardando}>{guardando ? 'Guardando…' : 'Guardar asignatura'}</button>
        <button type="button" className="app-button app-button--secondary" disabled={guardando} onClick={() => { setForm(null); setError(''); }}>Cancelar</button></div></form>
    </section>}
    {carreraId && <section className="catalogo-card" aria-label="Asignaturas de la carrera" aria-busy={cargando}>
      <div className="catalogo-list-heading"><h2>Asignaturas por nivel <span>({asignaturas.length})</span></h2>
        <button className="app-button app-button--secondary" disabled={cargando || guardando || !!form} onClick={() => setIntento(i => i + 1)}>Actualizar asignaturas</button></div>
      {cargando ? <p role="status">Cargando asignaturas…</p> : errorListado ? <div><p role="alert" className="auth-error">{errorListado}</p><button className="app-button app-button--secondary" onClick={() => setIntento(i => i + 1)}>Reintentar asignaturas</button></div>
        : asignaturas.length === 0 ? <p className="catalogo-empty">No hay asignaturas registradas en esta carrera.</p>
        : <><p className="catalogo-scroll-hint">Desliza la tabla horizontalmente para ver todos los datos.</p><div className="catalogo-table-scroll" tabIndex={0} role="region" aria-label="Tabla de asignaturas con desplazamiento horizontal"><table><caption className="sr-only">Asignaturas ordenadas por nivel y código</caption><thead><tr><th scope="col">Nivel</th><th scope="col">Código</th><th scope="col">Asignatura</th><th scope="col">Créditos</th><th scope="col">Horas</th></tr></thead>
          <tbody>{[...asignaturas].sort((a, b) => a.nivel - b.nivel || a.codigo.localeCompare(b.codigo, 'es')).map(a => <tr key={a.id}><td>{a.nivel}</td><td>{a.codigo}</td><td>{a.nombre}</td><td>{a.creditos}</td><td>{a.horas}</td></tr>)}</tbody></table></div></>}
    </section>}
  </main>;
}
