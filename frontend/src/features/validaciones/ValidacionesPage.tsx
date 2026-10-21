import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router';
import { Brand } from '../../shared/ui/Brand';
import { useAuth } from '../auth';
import { evaluar } from './api';
import { EquivalenciaForm } from './EquivalenciaForm';
import '../catalogo/catalogo.css';

type Resultado = { estado?: string; motivo?: string; cumple?: boolean; incumplimientos?: string[] };
export function ValidacionesPage() {
  const { request, logout, session } = useAuth();
  const [seccion, setSeccion] = useState('antiguedad');
  return <div className="workspace"><header className="workspace-header"><Brand /><button className="app-button app-button--secondary" onClick={logout}>Cerrar sesión</button></header>
    <main className="workspace-main catalogo"><p className="eyebrow">HOMOLOGACIÓN / VALIDACIONES</p><h1>Validaciones académicas</h1>
      <p>Estos resultados son sugerencias. La decisión oficial corresponde al Consejo Directivo.</p>
      {session?.role === 'Administrador' && <p><Link to="/panel">Volver al panel</Link></p>}
      <nav className="catalogo-nav" aria-label="Tipo de validación">{[['antiguedad', 'Aprobación y antigüedad'], ['requisitos', 'Requisitos de cambio'], ['equivalencia', 'Equivalencia de asignaturas']].map(([id, label]) => <button key={id} className="app-button app-button--secondary" aria-pressed={seccion === id} onClick={() => setSeccion(id!)}>{label}</button>)}</nav>
      {seccion === 'equivalencia' ? <EquivalenciaForm request={request} /> : <ReglasForm key={seccion} tipo={seccion} request={request} />}
    </main></div>;
}

function ReglasForm({ tipo, request }: { tipo: string; request: ReturnType<typeof useAuth>['request'] }) {
  const antiguedad = tipo === 'antiguedad';
  const [resultado, setResultado] = useState<Resultado | null>(null);
  const [error, setError] = useState(''); const [ocupado, setOcupado] = useState(false);
  const pendiente = useRef<AbortController | null>(null);
  useEffect(() => () => pendiente.current?.abort(), []);
  function limpiar() { pendiente.current?.abort(); setOcupado(false); setResultado(null); setError(''); }
  async function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); if (ocupado) return;
    const datos = new FormData(e.currentTarget);
    const input = antiguedad ? { estadoAsignatura: datos.get('estado'), fechaAprobacion: datos.get('aprobacion') || null, fechaEvaluacion: datos.get('evaluacion') }
      : { tipoCambio: datos.get('tipo'), periodosCursados: Number(datos.get('periodos')), asignaturasHomologables: Number(datos.get('asignaturas')), cambiosPrevios: Number(datos.get('cambios')) };
    const controller = new AbortController(); pendiente.current = controller; setOcupado(true); setResultado(null); setError('');
    try { const r = await evaluar<Resultado>(request, antiguedad ? 'validar-antiguedad' : 'validar-requisitos', input, controller.signal); if (!controller.signal.aborted) setResultado(r); }
    catch (e) { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'No hay conexión con el servicio.'); }
    finally { if (!controller.signal.aborted) setOcupado(false); }
  }
  return <section className="catalogo-card"><h2>{antiguedad ? 'Aprobación y antigüedad' : 'Requisitos de cambio de carrera'}</h2>
    {!antiguedad && <p>Indica la cantidad de asignaturas homologables de evaluaciones completas verificadas. No se calcula automáticamente aquí. No verifica apostillas ni gratuidad.</p>}
    <form onSubmit={enviar} onChange={limpiar}><fieldset className="catalogo-fields" disabled={ocupado}>{antiguedad ? <>
      <label className="auth-label">Estado de asignatura<select className="auth-input" name="estado" required><option>Aprobada</option><option>Reprobada</option><option>NoCursada</option></select></label>
      <label className="auth-label">Fecha de aprobación<input className="auth-input" type="date" name="aprobacion" /></label>
      <label className="auth-label">Fecha de evaluación<input className="auth-input" type="date" name="evaluacion" required /></label>
    </> : <>
      <label className="auth-label">Tipo de cambio<select className="auth-input" name="tipo" required><option>Interno</option><option>Externo</option></select></label>
      {[["periodos", "Períodos cursados"], ["asignaturas", "Asignaturas homologables"], ["cambios", "Cambios previos"]].map(([name, label]) => <label key={name} className="auth-label">{label}<input className="auth-input" name={name} type="number" min="0" max="2147483647" step="1" required /></label>)}
    </>}</fieldset><button className="app-button app-button--primary" disabled={ocupado}>{ocupado ? 'Evaluando…' : 'Validar'}</button> <button type="reset" className="app-button app-button--secondary" disabled={ocupado} onClick={limpiar}>Limpiar</button></form>
    {error && <p role="alert" className="auth-error">{error}</p>}{resultado && <div role="status"><h3>{antiguedad ? ({ Elegible: 'Elegible por aprobación y antigüedad', NoElegible: 'No elegible', RequiereValidacionConocimientos: 'Requiere validación de conocimientos' }[resultado.estado!] || resultado.estado) : resultado.cumple ? 'Cumple los requisitos evaluados' : 'No cumple los requisitos evaluados'}</h3><p>{resultado.motivo}</p><ul>{resultado.incumplimientos?.map(m => <li key={m}>{m}</li>)}</ul></div>}
  </section>;
}
