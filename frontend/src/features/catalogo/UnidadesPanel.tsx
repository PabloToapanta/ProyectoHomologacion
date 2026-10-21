import { useEffect, useState } from 'react';

import type { FormEvent } from 'react';

import type { CatalogoRequest } from './carreras-api';

interface Unidad { id: string; numero: number; nombre: string; horas: number; resultadoAprendizaje: string; contenidos: { id: string; orden: number; tema: string }[] }

const inicial = { numero: '', nombre: '', horas: '', resultadoAprendizaje: '', contenidos: '' };

const mensaje = (e: unknown) => e instanceof Error ? e.message : 'No se pudo completar la operación.';

export function UnidadesPanel({ asignaturaId, nombre, request, cerrar }: { asignaturaId: string; nombre: string; request: CatalogoRequest; cerrar: () => void }) {

  const [unidades, setUnidades] = useState<Unidad[]>([]);

  const [form, setForm] = useState(inicial);

  const [registrando, setRegistrando] = useState(false);
  const [editando, setEditando] = useState<string | null>(null);

  const [error, setError] = useState('');

  const [aviso, setAviso] = useState('');

  const [cargando, setCargando] = useState(true);

  const [guardando, setGuardando] = useState(false);

  const [intento, setIntento] = useState(0);

  useEffect(() => {

    const controller = new AbortController();

    setCargando(true); setError('');

    request(`/api/asignaturas/${encodeURIComponent(asignaturaId)}`, { signal: controller.signal }).then(async response => {

      if (!response.ok) throw new Error('No se pudo consultar la asignatura. Actualiza o vuelve a iniciar sesión.');

      const detalle = await response.json() as { unidades: Unidad[] };

      if (!controller.signal.aborted) setUnidades(detalle.unidades);

    }).catch(e => { if (!controller.signal.aborted) setError(mensaje(e)); })

      .finally(() => { if (!controller.signal.aborted) setCargando(false); });

    return () => controller.abort();

  }, [asignaturaId, request, intento]);

  async function guardar(e: FormEvent) {

    e.preventDefault(); if (guardando) return;

    const numero = Number(form.numero), horas = Number(form.horas);

    const contenidos = form.contenidos.split('\n').map(t => t.trim()).filter(Boolean);

    if (!Number.isInteger(numero) || numero <= 0 || numero > 2147483647 || !Number.isInteger(horas) || horas <= 0 || horas > 2147483647 || !form.nombre.trim() || !form.resultadoAprendizaje.trim() || contenidos.length === 0 || contenidos.length > 200 || contenidos.some(c => c.length > 4000)) {

      setError('Completa nombre, resultado, número y horas positivos, y entre 1 y 200 contenidos (máximo 4000 caracteres por contenido).'); return;

    }

    setGuardando(true); setError(''); setAviso('');

    try {

      const response = await request(`/api/asignaturas/${encodeURIComponent(asignaturaId)}/unidades${editando ? `/${encodeURIComponent(editando)}` : ''}`, { method: editando ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...form, numero, horas, contenidos }) });

      if (!response.ok) {

        const problem = await response.json().catch(() => null) as { detail?: string; errors?: Record<string, string[]> } | null;

        throw new Error(problem?.detail || (problem?.errors && Object.values(problem.errors).flat().join(' ')) || 'No se pudo registrar la unidad. Comprueba tus permisos y conexión.');

      }

      const unidad = await response.json() as Unidad;

      setUnidades(items => [...items.filter(u => u.id !== unidad.id), unidad].sort((a, b) => a.numero - b.numero));

      setForm(inicial); setEditando(null); setRegistrando(false); setAviso(editando ? 'Unidad actualizada correctamente.' : 'Unidad registrada correctamente.');

    } catch (e) { setError(mensaje(e)); } finally { setGuardando(false); }

  }

  const formulario = <>
      <h3>{editando ? `Editar unidad ${unidades.find(u => u.id === editando)?.numero ?? ''}` : 'Registrar unidad curricular'}</h3><form onSubmit={guardar} aria-label={editando ? 'Editar unidad' : 'Registrar unidad'}>{editando && error && <p role="alert" className="auth-error">{error}</p>}<fieldset className="catalogo-fields" disabled={guardando}>

        <label className="auth-label">Número de unidad<input className="auth-input" type="number" min="1" max="2147483647" required value={form.numero} onChange={e => setForm({ ...form, numero: e.target.value })} /></label>

        <label className="auth-label">Nombre de unidad<input className="auth-input" maxLength={200} required value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} /></label>

        <label className="auth-label">Horas de unidad<input className="auth-input" type="number" min="1" max="2147483647" required value={form.horas} onChange={e => setForm({ ...form, horas: e.target.value })} /></label>

        <label className="auth-label">Resultado de aprendizaje<textarea className="auth-input" rows={4} maxLength={4000} required value={form.resultadoAprendizaje} onChange={e => setForm({ ...form, resultadoAprendizaje: e.target.value })} /></label>

        <label className="auth-label">Contenidos (uno por línea)<textarea className="auth-input" required rows={8} value={form.contenidos} onChange={e => setForm({ ...form, contenidos: e.target.value })} /></label>

      </fieldset><button className="app-button app-button--primary" disabled={guardando}>{guardando ? 'Guardando…' : editando ? 'Guardar cambios' : 'Guardar unidad'}</button><button type="button" className="app-button app-button--secondary" disabled={guardando} onClick={() => { setEditando(null); setRegistrando(false); setForm(inicial); setError(''); }}>{editando ? 'Cancelar edición' : 'Cancelar registro'}</button><p>Cada línea se guarda como un contenido. Une los saltos de línea que provengan del PDF antes de guardar.</p></form>
  </>;
  return <section className="catalogo-card" aria-label={`Unidades de ${nombre}`}>

    <div className="catalogo-list-heading"><h2>Unidades y contenidos · {nombre}</h2><button className="app-button app-button--secondary" disabled={guardando} onClick={cerrar}>Cerrar unidades</button></div>

    {!editando && error && <p role="alert" className="auth-error">{error}</p>}{aviso && <p role="status">{aviso}</p>}

    {cargando ? <p role="status">Cargando unidades…</p> : <>

      <button className="app-button app-button--primary" disabled={guardando || !!editando || registrando} aria-expanded={registrando} onClick={() => { setRegistrando(true); setForm(inicial); setError(''); setAviso(''); }}>Nueva unidad</button>
      <button className="app-button app-button--secondary" disabled={guardando || !!editando || registrando} onClick={() => setIntento(i => i + 1)}>Actualizar unidades</button>

      {registrando && !editando && <section className="catalogo-card" aria-label="Nueva unidad">{formulario}</section>}
      {unidades.length === 0 ? <p>No hay unidades registradas.</p> : unidades.map(u => <article key={u.id} className="catalogo-card" aria-label={`Unidad ${u.numero}`}>{editando === u.id ? formulario : <><h3>U.{u.numero} · {u.nombre} ({u.horas} horas)</h3><p><strong>Resultado de aprendizaje:</strong> {u.resultadoAprendizaje}</p><ol>{u.contenidos.map(c => <li key={c.id}>{c.tema}</li>)}</ol><button className="app-button app-button--secondary" disabled={guardando || !!editando || registrando} onClick={() => { setEditando(u.id); setError(''); setAviso(''); setForm({ numero: String(u.numero), nombre: u.nombre, horas: String(u.horas), resultadoAprendizaje: u.resultadoAprendizaje, contenidos: u.contenidos.map(c => c.tema).join('\n') }); }}>Editar unidad {u.numero}</button></>}</article>)}




    </>}

  </section>;

}

