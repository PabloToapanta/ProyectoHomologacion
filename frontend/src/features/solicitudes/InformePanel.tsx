import {useEffect,useState} from 'react';
import {GruposInforme,type GrupoInforme} from './GruposInforme';
import './InformePanel.css';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Aprobada={id:string;nombreAsignatura:string};
type DocenteEnInforme={docenteId:string;docente:string;postura:'DeAcuerdo'|'Ajuste';decision:'Homologable'|'NoHomologable';observacion:string|null;fecha:string};
type Pareja={asignaturaAprobadaId:string;asignaturaReceptoraId:string;receptoraCodigo:string|null;receptoraNombre:string|null;
 agente:{estado:string;sugerencia:string|null;porcentajeAcademico:number|null;porcentajeHoras:number|null;porcentajeCreditos:number|null};
 docentes:DocenteEnInforme[];sugerenciaFinal:'Convalidar'|'NoConvalidar'|'SinAcuerdo'};
type Informe={solicitudId:string;tipoCambio:string;fecha:string;generadoPor:string;parejas:Pareja[];aprobadasSinAnalisis:string[];
 convalidar:number;noConvalidar:number;sinAcuerdo:number;advertencias:string[];grupos?:GrupoInforme[]};
function esInforme(value:unknown):value is Informe {
 if(typeof value!=='object'||value===null)return false;
 const x=value as Partial<Informe>;
 return typeof x.solicitudId==='string'&&typeof x.tipoCambio==='string'&&typeof x.fecha==='string'&&typeof x.generadoPor==='string'&&
  Array.isArray(x.parejas)&&Array.isArray(x.aprobadasSinAnalisis)&&Array.isArray(x.advertencias)&&
  Number.isFinite(x.convalidar)&&Number.isFinite(x.noConvalidar)&&Number.isFinite(x.sinAcuerdo)&&
  (x.grupos===undefined||Array.isArray(x.grupos));
}
const posturas={DeAcuerdo:'De acuerdo',Ajuste:'Ajuste'} as const;
const decisiones={Homologable:'Homologable',NoHomologable:'No homologable'} as const;
const finales={Convalidar:'Convalidar',NoConvalidar:'No convalidar',SinAcuerdo:'Sin acuerdo'} as const;
const pct=(v:number|null)=>v===null?'—':`${v.toLocaleString('es-EC',{maximumFractionDigits:2})} %`;

type Remision={solicitudId:string;fecha:string;remitidoPorId:string;remitidoPor:string};

/** HU-23: informe fijo; HU-32 añade la remisión del docente asignado; puede deshabilitarse para la vista del coordinador. */
export function InformePanel({request,solicitudId,aprobadas,estadoSolicitud,onRemitida,puedeGenerar=true}:{request:Request;solicitudId:string;aprobadas:Aprobada[];
 estadoSolicitud?:string;onRemitida?:(estado:'Remitida')=>void;puedeGenerar?:boolean}) {
 const ruta=`/api/homologaciones/solicitudes/${solicitudId}/informe`;
 const [informe,setInforme]=useState<Informe|null>(null),[pendiente,setPendiente]=useState(false),[cargando,setCargando]=useState(true);
 const [ocupado,setOcupado]=useState(false),[error,setError]=useState(''),[remision,setRemision]=useState<Remision|null>(null);
 async function motivo(r:Response,defecto:string){try{const p=await r.json();return p.detail??p.title??defecto;}catch{return defecto;}}
 useEffect(()=>{
  const controller=new AbortController();setCargando(true);setError('');setInforme(null);setPendiente(false);
  request(ruta,{signal:controller.signal}).then(async r=>{
   if(controller.signal.aborted)return;
   if(r.status===404){setPendiente(true);return;}
   if(!r.ok)throw new Error(await motivo(r,'No se pudo consultar el informe.'));
   const body:unknown=await r.json();if(!esInforme(body))throw new Error('El servicio devolvió un informe con formato no válido.');
   setInforme(body);
  }).catch(e=>{if(!controller.signal.aborted)setError(e instanceof Error?e.message:'No se pudo consultar el informe.');})
   .finally(()=>{if(!controller.signal.aborted)setCargando(false);});
  return ()=>controller.abort();
 // request es estable; la ruta identifica la solicitud.
 },[request,ruta]);
 async function generar(){
  if(ocupado)return;setOcupado(true);setError('');
  try{
   const r=await request(ruta,{method:'POST'});
   if(!r.ok)throw new Error(await motivo(r,'No se pudo generar el informe.'));
   const body:unknown=await r.json();if(!esInforme(body))throw new Error('El servicio devolvió un informe con formato no válido.');
   setInforme(body);setPendiente(false);
  }catch(e){setError(e instanceof Error?e.message:'No se pudo generar el informe.');}finally{setOcupado(false);}
 }
 async function remitir(){
  if(ocupado)return;setOcupado(true);setError('');
  try{
   const r=await request(`${ruta}/remision`,{method:'POST'});
   if(!r.ok)throw new Error(await motivo(r,'No se pudo remitir el informe.'));
   setRemision(await r.json() as Remision);onRemitida?.('Remitida');
  }catch(e){setError(e instanceof Error?e.message:'No se pudo remitir el informe.');}finally{setOcupado(false);}
 }
 const remitida=remision!==null||estadoSolicitud==='Remitida';
 const nombre=(id:string)=>aprobadas.find(a=>a.id===id)?.nombreAsignatura??'Asignatura sin nombre registrado';
 return <section className="informe-pdf" aria-labelledby="informe-titulo">
  <h3 id="informe-titulo">Informe de homologación</h3>
  {cargando&&<p>Consultando informe…</p>}{error&&<p role="alert">{error}</p>}
  {pendiente&&!informe&&<>
   <p>El informe aún no se ha generado. {puedeGenerar?'Puede generarlo cualquiera de los dos docentes cuando cada pareja analizada tenga el análisis de ambos. Una vez generado es fijo y los análisis docentes quedan cerrados.':'Los docentes asignados deben generarlo antes de que Coordinación pueda exportarlo.'}</p>
   {puedeGenerar&&<button className="app-button" disabled={ocupado} onClick={generar}>{ocupado?'Generando…':'Generar informe'}</button>}
  </>}
  {informe&&<>
   <button type="button" className="app-button informe-pdf__exportar" onClick={()=>window.print()}>Exportar informe a PDF</button>
   <div className="informe-pdf__contenido">
   <header className="informe-pdf__cabecera"><p className="informe-pdf__rotulo">Sistema de Homologación FISEI-UTA</p><h2>Informe técnico de homologación</h2>
    <p>Solicitud: {informe.solicitudId} · Tipo de cambio: {informe.tipoCambio}</p>
    <p>Generado por {informe.generadoPor} el {new Date(informe.fecha).toLocaleString('es-EC')}.</p>
    <p className="informe-pdf__aviso">Este documento es un informe técnico y no constituye una resolución oficial.</p>
   </header>
   <p>Parejas analizadas: {informe.parejas.length} · Convalidar: {informe.convalidar} · No convalidar: {informe.noConvalidar} · Sin acuerdo: {informe.sinAcuerdo}.</p>
   <div className="solicitudes-table"><table><caption>Informe de homologación por pareja</caption>
    <thead><tr><th>Asignatura aprobada</th><th>Receptora</th><th>Agente</th><th>Porcentajes (académico · horas · créditos)</th><th>Docentes</th><th>Sugerencia final</th></tr></thead>
    <tbody>{informe.parejas.map(p=><tr key={`${p.asignaturaAprobadaId}/${p.asignaturaReceptoraId}`}>
     <td>{nombre(p.asignaturaAprobadaId)}</td>
     <td>{p.receptoraCodigo??'—'} · {p.receptoraNombre??'—'}</td>
     <td>{p.agente.estado}{p.agente.sugerencia?` · ${p.agente.sugerencia}`:''}</td>
     <td>{pct(p.agente.porcentajeAcademico)} · {pct(p.agente.porcentajeHoras)} · {pct(p.agente.porcentajeCreditos)}</td>
     <td><ul>{p.docentes.map(d=><li key={d.docenteId}>{`${d.docente}: ${posturas[d.postura]} · ${decisiones[d.decision]}${d.observacion?` · ${d.observacion}`:''}`}</li>)}</ul></td>
     <td><strong>{finales[p.sugerenciaFinal]}</strong></td>
    </tr>)}</tbody></table></div>
   {!!informe.grupos?.length&&<GruposInforme grupos={informe.grupos} nombre={nombre}/>}
   {informe.aprobadasSinAnalisis.length>0&&<p>Sin análisis docente: {informe.aprobadasSinAnalisis.map(nombre).join(', ')}.</p>}
   {informe.advertencias.map(a=><p key={a}>{a}</p>)}
   {remision?<p role="status">{`Informe remitido al coordinador de carrera por ${remision.remitidoPor} el ${new Date(remision.fecha).toLocaleString('es-EC')}.`}</p>
    :estadoSolicitud==='Finalizada'?<p role="status">Solicitud finalizada por el coordinador de carrera.</p>
    :remitida?<p role="status">Informe remitido al coordinador de carrera.</p>
    :estadoSolicitud!==undefined&&<>
     <p>Remita el informe al coordinador de carrera cuando esté listo. Se remite una sola vez; las parejas «Sin acuerdo» las decide el coordinador.</p>
     <button className="app-button" disabled={ocupado} onClick={remitir}>{ocupado?'Remitiendo…':'Remitir al coordinador'}</button>
    </>}
   </div>
  </>}
 </section>;
}
