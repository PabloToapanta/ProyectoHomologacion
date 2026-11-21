import {useEffect,useState} from 'react';
import {FormDialog} from '../../shared/ui/FormDialog';
import {AnalisisInternoPanel} from './AnalisisInternoPanel';
import {InformePanel} from './InformePanel';
import type {DocenteAsignado} from './DocentesAsignados';
import '../catalogo/catalogo.css';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Solicitud={id:string;numeroQuipux:string;identificacionEstudiante:string;nombreEstudiante:string;tipoCambio:string;institucionOrigen:string|null;carreraOrigenExterna:string|null;estado:string;fechaRegistro:string};
type Asignatura={id:string;nombreAsignatura:string;creditos:number;horas:number;nota:number;estado:string};
type Detalle=Solicitud&{asignaturasAprobadas:Asignatura[];docentesAsignados:DocenteAsignado[];periodosCursados?:number|null;cambiosPreviosCarrera?:number|null};
/** Propuesta HU-30: HU-18 y HU-20 fijan el contrato definitivo del resultado. */
export type ResultadoAnalisis={porcentajeAcademico:number|null;porcentajeHoras:number|null;porcentajeCreditos:number|null;sugerencia:string;justificacion:string;evidencias:string[]};
export type Analisis={estado:string;mensaje:string;resultado:ResultadoAnalisis|null};
type SolicitudAsignada={solicitud:Detalle;analisis:Analisis};
const mensaje=(e:unknown)=>e instanceof Error?e.message:'No se pudo completar la operación.';
const porcentaje=(v:number|null)=>v===null?'Sin dato':`${v.toLocaleString('es-EC',{maximumFractionDigits:2})} %`;
/** Solo «Completo» con resultado presenta una sugerencia; cualquier otro caso se muestra como no concluido. */
export function AnalisisSolicitud({analisis}:{analisis:Analisis}) {
 const {estado,mensaje:texto,resultado}=analisis;
 if(estado==='Completo'&&resultado) return <section aria-labelledby="analisis-titulo">
  <h3 id="analisis-titulo">Resultado del análisis automatizado</h3>
  <p>Insumo para su revisión académica: no es la decisión final de homologación.</p>
  <dl><dt>Académico combinado</dt><dd>{porcentaje(resultado.porcentajeAcademico)}</dd><dt>Horas</dt><dd>{porcentaje(resultado.porcentajeHoras)}</dd><dt>Créditos</dt><dd>{porcentaje(resultado.porcentajeCreditos)}</dd>
   <dt>Sugerencia</dt><dd>{resultado.sugerencia}</dd><dt>Justificación</dt><dd>{resultado.justificacion}</dd></dl>
  <h4>Evidencia disponible</h4>{resultado.evidencias.length?<ul>{resultado.evidencias.map(e=><li key={e}>{e}</li>)}</ul>:<p>Sin evidencia registrada.</p>}
 </section>;
 const titulo=estado==='Pendiente'?'Análisis pendiente':estado==='Fallido'?'El análisis falló':'Información incompleta';
 return <section aria-labelledby="analisis-titulo" role="status"><h3 id="analisis-titulo">{titulo}</h3><p>{texto||'No hay un resultado de análisis disponible para esta solicitud.'}</p></section>;
}
/** HU-30 consulta solicitudes; HU-18 (interno) y HU-19 (externo) analizan parejas sin aprobar ni remitir (HU-31). */
export function SolicitudesDocentePage({request}:{request:Request}) {
 const [items,setItems]=useState<Solicitud[]>([]),[cargando,setCargando]=useState(true),[errorCarga,setErrorCarga]=useState(''),[intento,setIntento]=useState(0);
 const [detalle,setDetalle]=useState<SolicitudAsignada|null>(null),[error,setError]=useState(''),[ocupado,setOcupado]=useState(false);
 async function leer<T>(path:string,init?:RequestInit):Promise<T> {
  const response=await request(path,init);
  if(!response.ok){let text='No se pudo completar la operación.';try{const problem=await response.json();text=problem.detail??problem.title??text;}catch{/* respuesta sin cuerpo */}throw new Error(text);}
  return response.json() as Promise<T>;
 }
 useEffect(()=>{
  const controller=new AbortController();setCargando(true);setErrorCarga('');
  leer<Solicitud[]>('/api/solicitudes/asignadas',{signal:controller.signal}).then(s=>{if(!controller.signal.aborted)setItems(s);})
   .catch(e=>{if(!controller.signal.aborted)setErrorCarga(mensaje(e));}).finally(()=>{if(!controller.signal.aborted)setCargando(false);});
  return ()=>controller.abort();
 // leer usa exclusivamente el request estable del proveedor.
 },[request,intento]);
 async function ver(id:string) {
  if(ocupado)return;setOcupado(true);setError('');setDetalle(null);
  try{setDetalle(await leer<SolicitudAsignada>(`/api/solicitudes/asignadas/${id}`));}catch(e){setError(mensaje(e));}finally{setOcupado(false);}
 }
 const s=detalle?.solicitud;
 return <section className="catalogo-section" aria-labelledby="asignadas-title">
  <p className="eyebrow">DOCENCIA / SOLICITUDES ASIGNADAS</p><h1 id="asignadas-title">Mis solicitudes asignadas</h1>
  <p>Consulta las solicitudes en las que figura entre los dos docentes asignados y el estado de su análisis automatizado.</p>
  {error&&<p role="alert">{error}</p>}
  {cargando?<p>Cargando solicitudes asignadas…</p>:errorCarga?<div role="alert"><p>{errorCarga}</p><button className="app-button app-button--secondary" onClick={()=>setIntento(i=>i+1)}>Reintentar</button></div>:
   items.length===0?<p>No tiene solicitudes asignadas.</p>:<div className="solicitudes-table"><table><caption>Solicitudes asignadas</caption><thead><tr><th>Trámite Quipux</th><th>Estudiante</th><th>Tipo</th><th>Estado</th><th>Consulta</th></tr></thead>
   <tbody>{items.map(x=><tr key={x.id}><td>{x.numeroQuipux}</td><td>{x.nombreEstudiante}</td><td>{x.tipoCambio}</td><td>{x.estado}</td><td><button className="app-button app-button--secondary" disabled={ocupado} onClick={()=>ver(x.id)}>Ver {x.numeroQuipux}</button></td></tr>)}</tbody></table></div>}
  {detalle&&s&&<FormDialog title={`Solicitud ${s.numeroQuipux}`} titleId="detalle-asignada" busy={ocupado} onDismiss={()=>setDetalle(null)}>
   <dl><dt>Estudiante</dt><dd>{s.nombreEstudiante} · {s.identificacionEstudiante}</dd><dt>Tipo de cambio</dt><dd>{s.tipoCambio}{s.tipoCambio==='Externo'&&` · ${s.institucionOrigen} · ${s.carreraOrigenExterna}`}</dd><dt>Estado</dt><dd>{s.estado}</dd><dt>Fecha de registro</dt><dd>{new Date(s.fechaRegistro).toLocaleString('es-EC')}</dd>
    {s.tipoCambio==='Externo'&&<><dt>Períodos cursados</dt><dd>{s.periodosCursados??'Sin dato'}</dd><dt>Cambios previos de carrera</dt><dd>{s.cambiosPreviosCarrera??'Sin dato'}</dd></>}</dl>
   <h3>Docentes asignados</h3><ul aria-label="Docentes asignados">{s.docentesAsignados.map(d=><li key={d.id}>{d.nombreUsuario}</li>)}</ul>
   <h3>Asignaturas aprobadas</h3>{s.asignaturasAprobadas.length?<ul>{s.asignaturasAprobadas.map(a=><li key={a.id}>{a.nombreAsignatura} · {a.creditos} créditos · {a.horas} horas · nota {a.nota} ({a.estado})</li>)}</ul>:<p>Sin asignaturas aprobadas registradas.</p>}
   {s.tipoCambio==='Interno'||s.tipoCambio==='Externo'?<AnalisisInternoPanel key={s.id} tipo={s.tipoCambio} request={request} solicitudId={s.id} aprobadas={s.asignaturasAprobadas} inicial={detalle.analisis}/>:<AnalisisSolicitud analisis={detalle.analisis}/>}
   {(s.tipoCambio==='Interno'||s.tipoCambio==='Externo')&&<InformePanel key={`informe-${s.id}`} request={request} solicitudId={s.id} aprobadas={s.asignaturasAprobadas}/>}
   <button className="app-button app-button--secondary" onClick={()=>setDetalle(null)}>Cerrar detalle</button>
  </FormDialog>}
 </section>;
}
