import {fechaHora} from '../../shared/fecha';
import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { ConfiguracionApiError, configuracionApi } from './configuracion-api';
import type { ConfiguracionRequest, EntradaHistorial, ParametrosConfiguracion } from './configuracion-api';
import { base as rutaNormativas, leer, normativaDelRespaldo, respaldoDe } from '../normativas/normativas-api';
import type { Normativa } from '../normativas/normativas-api';

type Campo = Exclude<keyof ParametrosConfiguracion, 'versionId'>;
const campos: { key: Campo; label: string; max: number }[] = [
  { key: 'umbralPct', label: 'Umbral de compatibilidad (%)', max: 100 },
  { key: 'antiguedadMaximaAnios', label: 'Antigüedad máxima (años)', max: 50 },
  { key: 'periodosMinimosInterno', label: 'Períodos mínimos internos', max: 50 },
  { key: 'asignaturasHomologablesMinimasInterno', label: 'Asignaturas homologables mínimas internas', max: 50 },
  { key: 'periodosMinimosExterno', label: 'Períodos mínimos externos', max: 50 },
  { key: 'asignaturasHomologablesMinimasExterno', label: 'Asignaturas homologables mínimas externas', max: 50 }
];
const errorTexto = (error: unknown) => error instanceof Error ? error.message : 'No se pudo completar la operación.';
const resumen = (p: ParametrosConfiguracion) =>
  `${p.umbralPct}% · ${p.antiguedadMaximaAnios} años · interno ${p.periodosMinimosInterno}/${p.asignaturasHomologablesMinimasInterno} · externo ${p.periodosMinimosExterno}/${p.asignaturasHomologablesMinimasExterno}`;

export function ConfiguracionPage({ request }: { request: ConfiguracionRequest }) {
  const api = useMemo(() => configuracionApi(request), [request]);
  const [vigente, setVigente] = useState<ParametrosConfiguracion | null>(null);
  const [etag, setEtag] = useState('');
  const [valores, setValores] = useState<ParametrosConfiguracion | null>(null);
  const [motivo, setMotivo] = useState('');
  const [normativaId, setNormativaId] = useState('');
  const [detalleRespaldo, setDetalleRespaldo] = useState('');
  const [normativas, setNormativas] = useState<Normativa[]>([]);
  const [errorNormativas, setErrorNormativas] = useState('');
  const [historial, setHistorial] = useState<EntradaHistorial[]>([]);
  const [cursorSiguiente, setCursorSiguiente] = useState<string | null>(null);
  const [ocupado, setOcupado] = useState(false);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');
  const [aviso, setAviso] = useState('');

  useEffect(() => {
    const controller = new AbortController();
    Promise.all([api.obtener(controller.signal), api.historial(null, controller.signal)])
      .then(([actual, pagina]) => {
        if (controller.signal.aborted) return;
        setVigente(actual.parametros); setValores(actual.parametros); setEtag(actual.etag);
        setHistorial(pagina.entradas); setCursorSiguiente(pagina.cursorSiguiente);
      })
      .catch(e => { if (!controller.signal.aborted) setError(errorTexto(e)); })
      .finally(() => { if (!controller.signal.aborted) setCargando(false); });
    // HU-38: el respaldo se elige del catálogo de normativas; si no responde, la página sigue disponible para consulta.
    request(rutaNormativas, { signal: controller.signal })
      .then(r => leer<Normativa[]>(r, 'No se pudo consultar el catálogo de normativas.'))
      .then(lista => { if (!controller.signal.aborted) setNormativas(lista); })
      .catch(e => { if (!controller.signal.aborted) setErrorNormativas(errorTexto(e)); });
    return () => controller.abort();
  }, [api, request]);

  const vigentes = normativas.filter(n => n.estado === 'Vigente');
  // Aviso determinista: la configuración vigente cita una normativa que ya no rige.
  const respaldoVigente = vigente && historial[0]?.versionId === vigente.versionId ? historial[0].respaldo : undefined;
  const citada = normativaDelRespaldo(respaldoVigente, normativas);

  async function recargar() {
    const [actual, pagina] = await Promise.all([api.obtener(), api.historial()]);
    setVigente(actual.parametros); setValores(actual.parametros); setEtag(actual.etag);
    setHistorial(pagina.entradas); setCursorSiguiente(pagina.cursorSiguiente);
  }

  async function guardar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!valores || !etag || ocupado) return;
    const invalidos = campos.some(c => !Number.isInteger(valores[c.key]) || valores[c.key] < 1 || valores[c.key] > c.max);
    const normativa = vigentes.find(n => n.id === normativaId);
    const respaldo = normativa ? [respaldoDe(normativa), detalleRespaldo.trim()].filter(Boolean).join(' · ') : '';
    if (invalidos || !motivo.trim() || motivo.trim().length > 500 || !normativa || respaldo.length > 2000) {
      setError('Revisa los rangos enteros, completa el motivo (hasta 500 caracteres) y elige la normativa vigente que respalda el cambio.'); return;
    }
    setOcupado(true); setError(''); setAviso('');
    try {
      const { versionId: _versionId, ...parametros } = valores;
      await api.actualizar({ ...parametros, motivo: motivo.trim(), respaldo }, etag);
      await recargar();
      setMotivo(''); setNormativaId(''); setDetalleRespaldo(''); setAviso('Configuración guardada. Los análisis anteriores conservan sus resultados.');
    } catch (e) {
      if (e instanceof ConfiguracionApiError && e.status === 409) {
        try { await recargar(); } catch (recarga) { setError(errorTexto(recarga)); return; }
      }
      setError(errorTexto(e));
    } finally { setOcupado(false); }
  }

  async function masHistorial() {
    if (!cursorSiguiente || ocupado) return;
    setOcupado(true); setError('');
    try {
      const pagina = await api.historial(cursorSiguiente);
      setHistorial(actual => [...actual, ...pagina.entradas]);
      setCursorSiguiente(pagina.cursorSiguiente);
    } catch (e) { setError(errorTexto(e)); }
    finally { setOcupado(false); }
  }

  return <section className="configuracion-page" aria-labelledby="configuracion-title">
    <p className="eyebrow">CONFIGURACIÓN</p>
    <h1 id="configuracion-title">Parámetros de homologación</h1>
    <p>Los valores nuevos se aplican a los análisis que comiencen después de guardar. Cada cambio cita la normativa vigente que lo respalda.</p>
    {citada && citada.estado !== 'Vigente' && <p role="alert" className="configuracion-alerta">
      La configuración vigente se apoya en {citada.codigo}, que está «{citada.estado}»{citada.reformadaPor ? ` por ${citada.reformadaPor.codigo}` : ''}.
      Revisa los parámetros con la normativa vigente y guarda el cambio citándola.</p>}
    {vigente && historial[0]?.versionId === vigente.versionId && normativas.length > 0 && !citada && <p className="configuracion-nota">
      La configuración vigente no cita una normativa del catálogo (respaldo en texto libre anterior a HU-38).</p>}
    {cargando && <p>Cargando configuración…</p>}
    {error && <p role="alert" className="configuracion-error">{error}</p>}
    {aviso && <p role="status" className="configuracion-aviso">{aviso}</p>}
    {!cargando && !vigente && <button type="button" className="app-button" onClick={() => void recargar().catch(e => setError(errorTexto(e)))}>Reintentar</button>}
    {vigente && valores && <>
      <p className="configuracion-version">Versión vigente: <code>{vigente.versionId}</code></p>
      <form onSubmit={guardar} className="configuracion-form">
        <fieldset disabled={ocupado}>
          <legend>Valores académicos</legend>
          <div className="configuracion-campos">{campos.map(c => <label key={c.key}>{c.label}
            <input type="number" min="1" max={c.max} step="1" required value={valores[c.key]}
              onChange={e => setValores(actual => actual && ({ ...actual, [c.key]: e.target.value === '' ? 0 : Number(e.target.value) }))} />
          </label>)}</div>
          <p>El cambio externo siempre requiere cero cambios previos de carrera.</p>
          <label>Motivo del cambio<textarea required maxLength={500} value={motivo} onChange={e => setMotivo(e.target.value)} /></label>
          <label>Normativa que respalda el cambio<select required value={normativaId} onChange={e => setNormativaId(e.target.value)}>
            <option value="">Selecciona una normativa vigente</option>
            {vigentes.map(n => <option key={n.id} value={n.id}>{respaldoDe(n)}</option>)}
          </select></label>
          {errorNormativas && <p role="alert" className="configuracion-error">{errorNormativas}</p>}
          {!errorNormativas && vigentes.length === 0 && <p className="configuracion-nota">No hay normativas vigentes. Regístrala primero en <a href="/normativas">Normativas</a>.</p>}
          <label>Detalle del respaldo (opcional: artículo, sección)<textarea maxLength={500} value={detalleRespaldo} onChange={e => setDetalleRespaldo(e.target.value)} /></label>
          <button type="submit" className="app-button app-button--primary">Guardar configuración</button>
        </fieldset>
      </form>
      <section aria-labelledby="historial-title" className="configuracion-historial">
        <h2 id="historial-title">Historial de cambios</h2>
        <div className="configuracion-tabla"><table><thead><tr><th>Fecha y hora</th><th>Actor</th><th>Valores anteriores</th><th>Valores nuevos</th><th>Motivo</th><th>Respaldo</th></tr></thead>
          <tbody>{historial.map(entrada => <tr key={entrada.versionId}>
            <td>{fechaHora(entrada.fechaHoraUtc)}</td><td><code>{entrada.actorId === '00000000-0000-0000-0000-000000000000' ? 'Sistema' : entrada.actorId}</code></td>
            <td>{entrada.anterior ? resumen(entrada.anterior) : 'Configuración inicial'}</td><td>{resumen(entrada.nueva)}</td>
            <td>{entrada.motivo}</td><td>{entrada.respaldo}</td></tr>)}</tbody></table></div>
        {cursorSiguiente && <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={() => void masHistorial()}>Ver más</button>}
      </section>
    </>}
  </section>;
}
