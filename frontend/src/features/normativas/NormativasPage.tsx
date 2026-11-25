import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { fechaHora } from '../../shared/fecha';
import { FormDialog } from '../../shared/ui/FormDialog';
import { base, leer } from './normativas-api';
import type { EstadoNormativa, EventoNormativa, Normativa, Request } from './normativas-api';

const estados: EstadoNormativa[] = ['Vigente', 'Reformada', 'Derogada'];
const fecha = (iso: string) => { const [a, m, d] = iso.split('-'); return `${d}/${m}/${a}`; };
const tamano = (bytes: number) => bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KiB` : `${(bytes / 1024 / 1024).toFixed(1)} MiB`;
const mensaje = (e: unknown) => e instanceof Error ? e.message : 'No se pudo completar la operación.';
const formularioVacio = { codigo: '', titulo: '', emisor: '', fechaEmision: '', vigenteDesde: '', reformaAId: '' };

type Accion = { tipo: 'derogar' | 'eliminar'; normativa: Normativa };

/**
 * HU-38: catálogo de normativas. Consultan Administrador, Coordinador y Docente; registran y derogan
 * Administrador y Coordinador; solo el Administrador elimina un registro equivocado.
 */
export function NormativasPage({ request, rol }: { request: Request; rol: string }) {
  const gestiona = rol === 'Administrador' || rol === 'Coordinador';
  const elimina = rol === 'Administrador';
  const [normativas, setNormativas] = useState<Normativa[]>([]);
  const [filtro, setFiltro] = useState('');
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');
  const [aviso, setAviso] = useState('');
  const [registrando, setRegistrando] = useState(false);
  const [form, setForm] = useState(formularioVacio);
  const [archivo, setArchivo] = useState<File | null>(null);
  const [accion, setAccion] = useState<Accion | null>(null);
  const [motivo, setMotivo] = useState('');
  const [historial, setHistorial] = useState<{ normativa: Normativa; eventos: EventoNormativa[] } | null>(null);
  const [ocupado, setOcupado] = useState(false);
  const [errorDialogo, setErrorDialogo] = useState('');

  const cargar = useCallback(async (signal?: AbortSignal) => {
    const lista = await leer<Normativa[]>(await request(base, { signal }), 'No se pudo consultar el catálogo de normativas.');
    setNormativas(lista);
  }, [request]);

  useEffect(() => {
    const controller = new AbortController();
    cargar(controller.signal).catch(e => { if (!controller.signal.aborted) setError(mensaje(e)); })
      .finally(() => { if (!controller.signal.aborted) setCargando(false); });
    return () => controller.abort();
  }, [cargar]);

  const visibles = filtro ? normativas.filter(n => n.estado === filtro) : normativas;
  const vigentes = normativas.filter(n => n.estado === 'Vigente');

  async function registrar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!archivo) { setErrorDialogo('Selecciona el PDF oficial de la normativa.'); return; }
    setOcupado(true); setErrorDialogo('');
    try {
      const datos = new FormData();
      for (const [clave, valor] of Object.entries(form)) if (valor.trim()) datos.append(clave, valor.trim());
      datos.append('archivo', archivo, archivo.name);
      const nueva = await leer<Normativa>(await request(base, { method: 'POST', body: datos }), 'No se pudo registrar la normativa.');
      await cargar();
      setRegistrando(false); setForm(formularioVacio); setArchivo(null);
      setAviso(nueva.reformaA ? `Normativa ${nueva.codigo} registrada. ${nueva.reformaA.codigo} pasa a «Reformada».` : `Normativa ${nueva.codigo} registrada como vigente.`);
    } catch (e) { setErrorDialogo(mensaje(e)); }
    finally { setOcupado(false); }
  }

  async function confirmarAccion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!accion) return;
    if (!motivo.trim()) { setErrorDialogo('Indica el motivo.'); return; }
    setOcupado(true); setErrorDialogo('');
    try {
      const { tipo, normativa } = accion;
      const init: RequestInit = { method: tipo === 'derogar' ? 'POST' : 'DELETE', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ motivo: motivo.trim() }) };
      await leer(await request(tipo === 'derogar' ? `${base}/${normativa.id}/derogacion` : `${base}/${normativa.id}`, init),
        tipo === 'derogar' ? 'No se pudo derogar la normativa.' : 'No se pudo eliminar la normativa.');
      await cargar();
      setAviso(tipo === 'derogar' ? `Normativa ${normativa.codigo} derogada.` : normativa.reformaA
        ? `Normativa ${normativa.codigo} eliminada. ${normativa.reformaA.codigo} vuelve a estar vigente.` : `Normativa ${normativa.codigo} eliminada.`);
      setAccion(null); setMotivo('');
    } catch (e) { setErrorDialogo(mensaje(e)); }
    finally { setOcupado(false); }
  }

  async function verHistorial(normativa: Normativa) {
    setError('');
    try {
      const eventos = await leer<EventoNormativa[]>(await request(`${base}/${normativa.id}/historial`), 'No se pudo consultar el historial.');
      setHistorial({ normativa, eventos });
    } catch (e) { setError(mensaje(e)); }
  }

  async function descargar(normativa: Normativa) {
    setError('');
    try {
      const r = await request(`${base}/${normativa.id}/pdf`);
      if (!r.ok) await leer(r, 'No se pudo descargar el PDF.');
      const url = URL.createObjectURL(await r.blob());
      const enlace = document.createElement('a');
      enlace.href = url; enlace.download = normativa.nombreArchivo; enlace.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (e) { setError(mensaje(e)); }
  }

  return <section className="normativas-page" aria-labelledby="normativas-title">
    <p className="eyebrow">CATÁLOGO / NORMATIVAS</p>
    <h1 id="normativas-title">Normativas institucionales</h1>
    <p>Rige siempre la normativa aprobada más reciente: al registrar una reforma, la anterior pasa a «Reformada». Los parámetros de homologación se actualizan en Configuración citando la normativa vigente.</p>
    {error && <p role="alert" className="normativas-error">{error}</p>}
    {aviso && <p role="status" className="normativas-aviso">{aviso}</p>}
    <div className="normativas-barra">
      <label>Filtrar por estado<select value={filtro} onChange={e => setFiltro(e.target.value)}>
        <option value="">Todas</option>{estados.map(e => <option key={e} value={e}>{e}</option>)}
      </select></label>
      {gestiona && <button type="button" className="app-button app-button--primary" onClick={() => { setRegistrando(true); setErrorDialogo(''); setAviso(''); }}>Registrar normativa</button>}
    </div>
    {cargando ? <p role="status">Cargando normativas…</p> : visibles.length === 0
      ? <p>{normativas.length === 0 ? 'No hay normativas registradas.' : 'No hay normativas con ese estado.'}</p>
      : <div className="normativas-tabla"><table aria-label="Normativas registradas"><thead><tr>
        <th>Código</th><th>Título</th><th>Emisor</th><th>Vigente desde</th><th>Estado</th><th>Acciones</th></tr></thead>
        <tbody>{visibles.map(n => <tr key={n.id}>
          <td><strong>{n.codigo}</strong>{n.reformaA && <><br /><small>Reforma a {n.reformaA.codigo}</small></>}</td>
          <td>{n.titulo}</td><td>{n.emisor}</td><td>{fecha(n.vigenteDesde)}</td>
          <td><span className={`normativa-estado normativa-estado--${n.estado.toLowerCase()}`}>{n.estado}</span>
            {n.reformadaPor && <><br /><small>por {n.reformadaPor.codigo}</small></>}
            {n.motivoDerogacion && <><br /><small>{n.motivoDerogacion}</small></>}</td>
          <td className="normativas-acciones">
            <button type="button" className="app-button app-button--secondary" onClick={() => void descargar(n)} aria-label={`Descargar PDF de ${n.codigo}`}>PDF ({tamano(n.tamano)})</button>
            <button type="button" className="app-button app-button--secondary" onClick={() => void verHistorial(n)} aria-label={`Historial de ${n.codigo}`}>Historial</button>
            {gestiona && n.estado === 'Vigente' && <button type="button" className="app-button app-button--secondary" aria-label={`Derogar ${n.codigo}`}
              onClick={() => { setAccion({ tipo: 'derogar', normativa: n }); setMotivo(''); setErrorDialogo(''); setAviso(''); }}>Derogar</button>}
            {elimina && n.estado !== 'Reformada' && <button type="button" className="app-button app-button--danger" aria-label={`Eliminar ${n.codigo}`}
              onClick={() => { setAccion({ tipo: 'eliminar', normativa: n }); setMotivo(''); setErrorDialogo(''); setAviso(''); }}>Eliminar</button>}
          </td></tr>)}</tbody></table></div>}

    {registrando && <FormDialog title="Registrar normativa" titleId="registrar-normativa" busy={ocupado} onDismiss={() => setRegistrando(false)}>
      <form className="normativas-form" onSubmit={registrar} aria-label="Registrar normativa">
        <label>Código de resolución<input required maxLength={50} value={form.codigo} onChange={e => setForm({ ...form, codigo: e.target.value })} /></label>
        <label>Título<input required maxLength={300} value={form.titulo} onChange={e => setForm({ ...form, titulo: e.target.value })} /></label>
        <label>Órgano emisor<input required maxLength={200} value={form.emisor} onChange={e => setForm({ ...form, emisor: e.target.value })} /></label>
        <div className="normativas-fechas">
          <label>Fecha de emisión<input type="date" required value={form.fechaEmision} onChange={e => setForm({ ...form, fechaEmision: e.target.value })} /></label>
          <label>Vigente desde<input type="date" required value={form.vigenteDesde} onChange={e => setForm({ ...form, vigenteDesde: e.target.value })} /></label>
        </div>
        <label>¿Reforma a otra normativa?<select value={form.reformaAId} onChange={e => setForm({ ...form, reformaAId: e.target.value })}>
          <option value="">No, es una normativa nueva</option>
          {vigentes.map(n => <option key={n.id} value={n.id}>{n.codigo} — {n.titulo}</option>)}
        </select></label>
        <label>PDF oficial (hasta 10 MB)<input type="file" accept="application/pdf,.pdf" onChange={e => setArchivo(e.target.files?.[0] ?? null)} /></label>
        {errorDialogo && <p role="alert" className="normativas-error">{errorDialogo}</p>}
        <div className="normativas-botones">
          <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={() => setRegistrando(false)}>Cancelar</button>
          <button type="submit" className="app-button app-button--primary" disabled={ocupado}>{ocupado ? 'Guardando…' : 'Registrar'}</button>
        </div>
      </form>
    </FormDialog>}

    {accion && <FormDialog title={accion.tipo === 'derogar' ? `Derogar ${accion.normativa.codigo}` : `Eliminar ${accion.normativa.codigo}`}
      titleId="accion-normativa" busy={ocupado} onDismiss={() => setAccion(null)}>
      <form className="normativas-form" onSubmit={confirmarAccion}>
        <p>{accion.tipo === 'derogar'
          ? 'La normativa queda como «Derogada» y deja de poder citarse como respaldo. No se elimina.'
          : `Úsalo solo si se registró por error. El PDF y los datos se eliminan; el historial conserva quién la eliminó y por qué.${accion.normativa.reformaA ? ` ${accion.normativa.reformaA.codigo} volverá a estar vigente.` : ''}`}</p>
        <label>Motivo<textarea required maxLength={500} value={motivo} onChange={e => setMotivo(e.target.value)} /></label>
        {errorDialogo && <p role="alert" className="normativas-error">{errorDialogo}</p>}
        <div className="normativas-botones">
          <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={() => setAccion(null)}>Cancelar</button>
          <button type="submit" className={`app-button ${accion.tipo === 'eliminar' ? 'app-button--danger' : 'app-button--primary'}`} disabled={ocupado}>
            {accion.tipo === 'derogar' ? 'Derogar' : 'Eliminar definitivamente'}</button>
        </div>
      </form>
    </FormDialog>}

    {historial && <FormDialog title={`Historial de ${historial.normativa.codigo}`} titleId="historial-normativa" onDismiss={() => setHistorial(null)}>
      {historial.eventos.length === 0 ? <p>Sin eventos registrados.</p> : <ol className="normativas-historial">
        {historial.eventos.map(e => <li key={e.id}><strong>{e.tipo}</strong> · {fechaHora(e.fecha)} · {e.usuario}{e.detalle && <> — {e.detalle}</>}</li>)}
      </ol>}
    </FormDialog>}
  </section>;
}
