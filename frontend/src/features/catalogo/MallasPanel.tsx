import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import type { CatalogoRequest } from './carreras-api';
import { mallasApi } from './mallas-api';
import type { Malla } from './mallas-api';

export function MallasPanel({ carreraId, request, onSeleccion, onCambio }: {
  carreraId: string; request: CatalogoRequest; onSeleccion: (malla: Malla | null) => void; onCambio: () => void;
}) {
  const api = useMemo(() => mallasApi(request), [request]);
  const [mallas, setMallas] = useState<Malla[]>([]);
  const [seleccion, setSeleccion] = useState('');
  const [nombre, setNombre] = useState('');
  const [vigente, setVigente] = useState(false);
  const [cargando, setCargando] = useState(true);
  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState('');
  const [aviso, setAviso] = useState('');
  const [intento, setIntento] = useState(0);
  const mensaje = (e: unknown) => e instanceof Error ? e.message : 'No se pudo conectar con las mallas.';

  useEffect(() => {
    const controller = new AbortController();
    setCargando(true);
    api.listar(carreraId, controller.signal).then(items => {
      if (!controller.signal.aborted) setMallas(items);
    }).catch(e => { if (!controller.signal.aborted) setError(mensaje(e)); })
      .finally(() => { if (!controller.signal.aborted) setCargando(false); });
    return () => controller.abort();
  }, [api, carreraId, intento]);

  async function guardar(event: FormEvent) {
    event.preventDefault();
    if (guardando) return;
    if (!nombre.trim()) { setError('El nombre de la versión es obligatorio.'); return; }
    setGuardando(true); setError(''); setAviso('');
    try {
      await api.crear(carreraId, nombre.trim(), vigente);
      setNombre(''); setVigente(false); setAviso('Versión creada.'); onCambio();
    } catch (e) { setError(mensaje(e)); }
    finally { setGuardando(false); setIntento(i => i + 1); }
  }

  async function activar(malla: Malla) {
    if (guardando) return;
    setGuardando(true); setError(''); setAviso('');
    try {
      await api.activar(malla.id);
      setAviso('Versión activada.'); onCambio();
    } catch (e) { setError(mensaje(e)); }
    finally { setGuardando(false); setIntento(i => i + 1); }
  }

  return <section className="catalogo-card" aria-label="Versiones de malla" aria-busy={cargando || guardando}>
    <h2>Versiones de malla</h2>
    <p>Activar una versión conserva las asignaturas de las anteriores. No se copian ni trasladan automáticamente.</p>
    {error && <p role="alert" className="auth-error">{error}</p>}
    {aviso && <p role="status">{aviso}</p>}
    {cargando ? <p role="status">Cargando versiones…</p> : <>
      <label className="auth-label">Versión de malla<select className="auth-input" value={seleccion} disabled={guardando} onChange={e => {
        setSeleccion(e.target.value); onSeleccion(mallas.find(m => m.id === e.target.value) ?? null);
      }}><option value="">Todas las versiones</option>{mallas.map(m => <option key={m.id} value={m.id}>{m.nombre} · {m.vigente ? 'Vigente' : 'No vigente'}</option>)}</select></label>
      {mallas.length === 0 && <p>No hay versiones registradas.</p>}
      <ul>{mallas.map(m => <li key={m.id}>{m.nombre} — {m.vigente ? 'Vigente' : 'No vigente'}{' '}
        {!m.vigente && <button className="app-button app-button--secondary" disabled={guardando} onClick={() => void activar(m)}>Activar {m.nombre}</button>}</li>)}</ul>
    </>}
    <form onSubmit={guardar} noValidate><fieldset className="catalogo-fields" disabled={guardando || cargando}>
      <label className="auth-label">Nombre de la versión<input className="auth-input" required maxLength={100} value={nombre} onChange={e => setNombre(e.target.value)} /></label>
      <label><input type="checkbox" checked={vigente} onChange={e => setVigente(e.target.checked)} /> Activar al crear</label>
      <button className="app-button app-button--primary" disabled={guardando || cargando}>{guardando ? 'Guardando…' : 'Crear versión'}</button>
    </fieldset></form>
    <button className="app-button app-button--secondary" disabled={cargando || guardando} onClick={() => { setError(''); setIntento(i => i + 1); }}>Actualizar versiones</button>
  </section>;
}
