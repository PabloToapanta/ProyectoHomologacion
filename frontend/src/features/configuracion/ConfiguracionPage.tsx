import {fechaHora} from '../../shared/fecha';
import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { ConfiguracionApiError, configuracionApi } from './configuracion-api';
import type { ConfiguracionRequest, EntradaHistorial, ParametrosConfiguracion } from './configuracion-api';

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
  const [respaldo, setRespaldo] = useState('');
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
    return () => controller.abort();
  }, [api]);

  async function recargar() {
    const [actual, pagina] = await Promise.all([api.obtener(), api.historial()]);
    setVigente(actual.parametros); setValores(actual.parametros); setEtag(actual.etag);
    setHistorial(pagina.entradas); setCursorSiguiente(pagina.cursorSiguiente);
  }

  async function guardar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!valores || !etag || ocupado) return;
    const invalidos = campos.some(c => !Number.isInteger(valores[c.key]) || valores[c.key] < 1 || valores[c.key] > c.max);
    if (invalidos || !motivo.trim() || motivo.trim().length > 500 || !respaldo.trim() || respaldo.trim().length > 2000) {
      setError('Revisa los rangos enteros y completa motivo (hasta 500) y respaldo (hasta 2000 caracteres).'); return;
    }
    setOcupado(true); setError(''); setAviso('');
    try {
      const { versionId: _versionId, ...parametros } = valores;
      await api.actualizar({ ...parametros, motivo: motivo.trim(), respaldo: respaldo.trim() }, etag);
      await recargar();
      setMotivo(''); setRespaldo(''); setAviso('Configuración guardada. Los análisis anteriores conservan sus resultados.');
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
    <p className="eyebrow">COORDINACIÓN / CONFIGURACIÓN</p>
    <h1 id="configuracion-title">Parámetros de homologación</h1>
    <p>Los valores nuevos se aplican a los análisis que comiencen después de guardar.</p>
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
          <label>Respaldo<textarea required maxLength={2000} value={respaldo} onChange={e => setRespaldo(e.target.value)} /></label>
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
