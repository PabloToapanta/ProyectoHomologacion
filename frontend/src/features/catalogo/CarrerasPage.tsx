import { useEffect, useMemo, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { carrerasApi } from './carreras-api';
import type { Carrera, CarreraInput, CatalogoRequest } from './carreras-api';
import './catalogo.css';

const FACULTAD_RECEPTORA = 'FISEI';
const vacia: CarreraInput = { nombre: '', facultad: FACULTAD_RECEPTORA, vigencia: true, modalidad: null };
const mensaje = (error: unknown) => error instanceof Error ? error.message : 'No se pudo completar la operación.';

export function CarrerasPage({ request }: { request: CatalogoRequest }) {
  const api = useMemo(() => carrerasApi(request), [request]);
  const [carreras, setCarreras] = useState<Carrera[]>([]);
  const [cargando, setCargando] = useState(true);
  const [errorCarga, setErrorCarga] = useState('');
  const [error, setError] = useState('');
  const [aviso, setAviso] = useState('');
  const [intento, setIntento] = useState(0);
  const [form, setForm] = useState<CarreraInput | null>(null);
  const [editando, setEditando] = useState<string | null>(null);
  const [eliminando, setEliminando] = useState<Carrera | null>(null);
  const [ocupado, setOcupado] = useState(false);
  const dialog = useRef<HTMLDialogElement>(null);
  const nombre = useRef<HTMLInputElement>(null);
  const requiereRevision = !!form && form.facultad.trim().toUpperCase() !== FACULTAD_RECEPTORA;

  useEffect(() => {
    const controller = new AbortController();
    setCargando(true); setErrorCarga('');
    api.listar(controller.signal).then(items => {
      if (!controller.signal.aborted) setCarreras(items);
    }).catch(e => { if (!controller.signal.aborted) setErrorCarga(mensaje(e)); })
      .finally(() => { if (!controller.signal.aborted) setCargando(false); });
    return () => controller.abort();
  }, [api, intento]);

  useEffect(() => { if (form) nombre.current?.focus(); }, [editando, form !== null]);
  useEffect(() => {
    if (eliminando && dialog.current) {
      if (dialog.current.showModal) dialog.current.showModal();
      else dialog.current.setAttribute('open', '');
    }
    else dialog.current?.close?.();
  }, [eliminando]);

  function abrir(carrera?: Carrera) {
    setEditando(carrera?.id ?? null);
    setForm(carrera ? { nombre: carrera.nombre, facultad: carrera.facultad.trim().toUpperCase() === FACULTAD_RECEPTORA ? FACULTAD_RECEPTORA : carrera.facultad, vigencia: carrera.vigencia, modalidad: carrera.modalidad } : { ...vacia });
    setError(''); setAviso('');
  }

  async function guardar(event: FormEvent) {
    event.preventDefault();
    if (!form || ocupado || requiereRevision) return;
    if (!form.nombre.trim()) { setError('Nombre es obligatorio.'); return; }
    setOcupado(true); setError(''); setAviso('');
    try {
      const input = { ...form, nombre: form.nombre.trim(), facultad: FACULTAD_RECEPTORA, modalidad: form.modalidad?.trim() || null };
      const guardada = editando ? await api.actualizar(editando, input) : await api.crear(input);
      setCarreras(items => editando ? items.map(c => c.id === editando ? guardada : c) : [...items, guardada]);
      setForm(null); setEditando(null); setAviso('Carrera guardada correctamente.');
    } catch (e) { setError(mensaje(e)); }
    finally { setOcupado(false); }
  }

  async function eliminar() {
    if (!eliminando || ocupado) return;
    setOcupado(true); setError(''); setAviso('');
    try {
      await api.eliminar(eliminando.id);
      setCarreras(items => items.filter(c => c.id !== eliminando.id));
      setEliminando(null); setAviso('Carrera eliminada correctamente.');
    } catch (e) { setEliminando(null); setError(mensaje(e)); }
    finally { setOcupado(false); }
  }

  return <div className="catalogo">
    <p className="eyebrow">ADMINISTRACIÓN / CATÁLOGO ACADÉMICO</p>
    <div className="workspace-heading"><div><h1>Gestión de carreras</h1><p>Registra y actualiza las carreras de FISEI.</p></div>
      <button className="app-button app-button--primary" disabled={ocupado || cargando || !!errorCarga || !!form} onClick={() => abrir()}>Nueva carrera</button></div>
    {aviso && <p role="status" className="catalogo-aviso">{aviso}</p>}
    {error && <p role="alert" className="auth-error">{error}</p>}
    {form && <section className="catalogo-card" aria-labelledby="form-title">
      <h2 id="form-title">{editando ? 'Editar carrera' : 'Nueva carrera'}</h2>
      {requiereRevision && <p role="alert" className="auth-error">Este registro anterior requiere revisión antes de editar: su facultad no es FISEI. Se conservan sus datos y asignaturas; coordina su corrección con el equipo.</p>}
      <form onSubmit={guardar}>
        <fieldset disabled={ocupado} className="catalogo-fields">
          <label className="auth-label">Nombre<input ref={nombre} className="auth-input" required maxLength={200} value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} /></label>
          <label className="auth-label">Facultad<input className="auth-input" readOnly value={form.facultad} /></label>
          <label className="auth-label">Modalidad (opcional)<input className="auth-input" maxLength={100} value={form.modalidad ?? ''} onChange={e => setForm({ ...form, modalidad: e.target.value })} /></label>
          <label className="catalogo-check"><input type="checkbox" checked={form.vigencia} onChange={e => setForm({ ...form, vigencia: e.target.checked })} />Vigente</label>
        </fieldset>
        <div className="catalogo-actions"><button className="app-button app-button--primary" disabled={ocupado || requiereRevision}>{ocupado ? 'Guardando…' : 'Guardar carrera'}</button>
          <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={() => { setForm(null); setError(''); }}>Cancelar</button></div>
      </form>
    </section>}
    <section className="catalogo-card" aria-label="Listado de carreras" aria-busy={cargando}>
      <div className="catalogo-list-heading"><h2>Carreras registradas <span>({carreras.length})</span></h2>
        <button className="app-button app-button--secondary" disabled={cargando || ocupado || !!form} onClick={() => setIntento(i => i + 1)}>Actualizar listado</button></div>
      {cargando ? <p role="status">Cargando carreras…</p> : errorCarga ? <div><p role="alert" className="auth-error">{errorCarga}</p><button className="app-button app-button--secondary" onClick={() => setIntento(i => i + 1)}>Reintentar</button></div>
        : carreras.length === 0 ? <p className="catalogo-empty">No hay carreras registradas.</p>
        : <div className="catalogo-table-scroll"><table><caption className="sr-only">Carreras FISEI y registros anteriores pendientes de revisión</caption><thead><tr><th scope="col">Carrera</th><th scope="col">Facultad</th><th scope="col">Modalidad</th><th scope="col">Vigencia</th><th scope="col">Acciones</th></tr></thead>
          <tbody>{[...carreras].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es') || a.facultad.localeCompare(b.facultad, 'es')).map(c => <tr key={c.id}>
            <td>{c.nombre}</td><td>{c.facultad}{c.facultad.trim().toUpperCase() !== FACULTAD_RECEPTORA && <small> · Pendiente de revisión</small>}</td><td>{c.modalidad || 'Sin especificar'}</td><td><span className={`catalogo-badge ${c.vigencia ? '' : 'catalogo-badge--inactive'}`}>{c.vigencia ? 'Vigente' : 'No vigente'}</span></td>
            <td><div className="catalogo-actions"><button className="app-button app-button--secondary" aria-label={`Editar ${c.nombre}`} disabled={ocupado || !!form} onClick={() => abrir(c)}>Editar</button>
              <button className="app-button catalogo-danger" aria-label={`Eliminar ${c.nombre}`} disabled={ocupado || !!form} onClick={() => { setEliminando(c); setError(''); }}>Eliminar</button></div></td>
          </tr>)}</tbody></table></div>}
    </section>
    {eliminando && <dialog ref={dialog} aria-labelledby="delete-title" onCancel={e => { if (ocupado) e.preventDefault(); else setEliminando(null); }}>
      <h2 id="delete-title">Eliminar carrera</h2><p>¿Deseas eliminar «{eliminando.nombre}»?</p><p>Solo se permite eliminar carreras sin asignaturas relacionadas.</p>
      <div className="catalogo-actions"><button className="app-button catalogo-danger" disabled={ocupado} onClick={eliminar}>{ocupado ? 'Eliminando…' : 'Confirmar eliminación'}</button>
        <button className="app-button app-button--secondary" disabled={ocupado} onClick={() => setEliminando(null)}>Cancelar</button></div>
    </dialog>}
  </div>;
}
