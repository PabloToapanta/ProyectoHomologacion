import {useEffect,useState} from 'react';
import type {FormEvent} from 'react';
import {fechaHora} from '../../shared/fecha';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Aprobada={id:string;nombreAsignatura:string};
type DocenteEnInforme={docenteId:string;docente:string;postura:'DeAcuerdo'|'Ajuste';decision:'Homologable'|'NoHomologable';observacion:string|null;fecha:string};
type Pareja={asignaturaAprobadaId:string;asignaturaReceptoraId:string;receptoraCodigo:string|null;receptoraNombre:string|null;
 agente:{estado:string;sugerencia:string|null;porcentajeAcademico:number|null;porcentajeHoras:number|null;porcentajeCreditos:number|null};
 docentes:DocenteEnInforme[];sugerenciaFinal:'Convalidar'|'NoConvalidar'|'SinAcuerdo'};
type Informe={solicitudId:string;fecha:string;generadoPor:string;parejas:Pareja[];aprobadasSinAnalisis:string[];advertencias:string[]};
type Decision={asignaturaAprobadaId:string;asignaturaReceptoraId:string;resultado:'Convalidar'|'NoConvalidar';justificacion:string;coordinador:string;fecha:string};
type Finalizacion={solicitudId:string;fecha:string;finalizadoPorId:string;finalizadoPor:string};
type Revision={informe:Informe;remitidoEl:string;remitidoPor:string;decisiones:Decision[];decisionesPendientes:number;finalizacion:Finalizacion|null};
const posturas={DeAcuerdo:'De acuerdo',Ajuste:'Ajuste'} as const;
const decisiones={Homologable:'Homologable',NoHomologable:'No homologable'} as const;
const resultados={Convalidar:'Convalidar',NoConvalidar:'No convalidar',SinAcuerdo:'Sin acuerdo'} as const;
const pct=(v:number|null)=>v===null?'—':`${v.toLocaleString('es-EC',{maximumFractionDigits:2})} %`;
const fecha=(v:string)=>fechaHora(v);
const clave=(aprobada:string,receptora:string)=>`${aprobada}/${receptora}`;
async function motivo(r:Response,defecto:string){try{const p=await r.json();return p.detail??p.title??defecto;}catch{return defecto;}}

/** Formulario de la decisión propia y motivada del coordinador en una pareja «Sin acuerdo». */
function FormDecision({pareja,actual,ocupado,guardar}:{pareja:Pareja;actual?:Decision;ocupado:boolean;guardar:(resultado:string,justificacion:string)=>Promise<void>}){
 const [resultado,setResultado]=useState<string>(actual?.resultado??'Convalidar'),[justificacion,setJustificacion]=useState(actual?.justificacion??'');
 const etiqueta=`Decisión del coordinador · ${pareja.receptoraCodigo??pareja.asignaturaReceptoraId}`;
 async function enviar(e:FormEvent){e.preventDefault();await guardar(resultado,justificacion);}
 return <form noValidate onSubmit={enviar}><fieldset className="solicitud-fields" disabled={ocupado} aria-label={etiqueta}><legend>{etiqueta}</legend>
  <label>Decisión<select value={resultado} onChange={e=>setResultado(e.target.value)}><option value="Convalidar">Convalidar</option><option value="NoConvalidar">No convalidar</option></select></label>
  <label>Justificación<textarea value={justificacion} maxLength={2000} rows={3} onChange={e=>setJustificacion(e.target.value)}/></label>
  <button type="submit" className="app-button app-button--secondary">Guardar decisión</button>
 </fieldset></form>;
}

/** HU-33: el coordinador revisa el informe remitido, decide las parejas «Sin acuerdo» y finaliza la solicitud una sola vez. */
export function RevisionInformeCoordinador({request,solicitudId,aprobadas,onFinalizada}:{request:Request;solicitudId:string;aprobadas:Aprobada[];onFinalizada?:(estado:'Finalizada')=>void}){
 const ruta=`/api/homologaciones/solicitudes/${solicitudId}/informe`;
 const [revision,setRevision]=useState<Revision|null>(null),[pendiente,setPendiente]=useState(''),[cargando,setCargando]=useState(true);
 const [ocupado,setOcupado]=useState(false),[error,setError]=useState('');
 useEffect(()=>{
  const controller=new AbortController();setCargando(true);setError('');setRevision(null);setPendiente('');
  request(`${ruta}/revision`,{signal:controller.signal}).then(async r=>{
   if(controller.signal.aborted)return;
   if(r.status===404){setPendiente(await motivo(r,'El informe de esta solicitud aún no ha sido remitido al coordinador de carrera.'));return;}
   if(!r.ok)throw new Error(await motivo(r,'No se pudo consultar el informe remitido.'));
   setRevision(await r.json() as Revision);
  }).catch(e=>{if(!controller.signal.aborted)setError(e instanceof Error?e.message:'No se pudo consultar el informe remitido.');})
   .finally(()=>{if(!controller.signal.aborted)setCargando(false);});
  return ()=>controller.abort();
 // request es estable; la ruta identifica la solicitud.
 },[request,ruta]);
 async function decidir(p:Pareja,resultado:string,justificacion:string){
  if(ocupado||!revision)return;setOcupado(true);setError('');
  try{
   const r=await request(`${ruta}/decisiones`,{method:'PUT',headers:{'Content-Type':'application/json'},
    body:JSON.stringify({asignaturaAprobadaId:p.asignaturaAprobadaId,asignaturaReceptoraId:p.asignaturaReceptoraId,resultado,justificacion})});
   if(!r.ok)throw new Error(await motivo(r,'No se pudo registrar la decisión.'));
   const guardada=await r.json() as Decision;
   setRevision(actual=>{
    if(!actual)return actual;
    const otras=actual.decisiones.filter(d=>clave(d.asignaturaAprobadaId,d.asignaturaReceptoraId)!==clave(guardada.asignaturaAprobadaId,guardada.asignaturaReceptoraId));
    const lista=[...otras,guardada];
    const faltan=actual.informe.parejas.filter(x=>x.sugerenciaFinal==='SinAcuerdo'&&!lista.some(d=>clave(d.asignaturaAprobadaId,d.asignaturaReceptoraId)===clave(x.asignaturaAprobadaId,x.asignaturaReceptoraId))).length;
    return {...actual,decisiones:lista,decisionesPendientes:faltan};
   });
  }catch(e){setError(e instanceof Error?e.message:'No se pudo registrar la decisión.');}finally{setOcupado(false);}
 }
 async function finalizar(){
  if(ocupado||!revision)return;setOcupado(true);setError('');
  try{
   const r=await request(`${ruta}/finalizacion`,{method:'POST'});
   if(!r.ok)throw new Error(await motivo(r,'No se pudo finalizar la solicitud.'));
   const fin=await r.json() as Finalizacion;
   setRevision(actual=>actual&&{...actual,finalizacion:fin});onFinalizada?.('Finalizada');
  }catch(e){setError(e instanceof Error?e.message:'No se pudo finalizar la solicitud.');}finally{setOcupado(false);}
 }
 const nombre=(id:string)=>aprobadas.find(a=>a.id===id)?.nombreAsignatura??'Asignatura sin nombre registrado';
 const finalizada=revision?.finalizacion??null;
 return <section aria-labelledby="revision-informe-titulo">
  <h3 id="revision-informe-titulo">Revisión del informe y finalización</h3>
  {cargando&&<p>Consultando informe remitido…</p>}{error&&<p role="alert">{error}</p>}
  {pendiente&&<p>{pendiente}</p>}
  {revision&&<>
   <p>{`Remitido por ${revision.remitidoPor} el ${fecha(revision.remitidoEl)}. Generado por ${revision.informe.generadoPor} el ${fecha(revision.informe.fecha)}.`}</p>
   <div className="solicitudes-table"><table><caption>Revisión del informe por pareja</caption>
    <thead><tr><th>Asignatura aprobada</th><th>Receptora</th><th>Agente</th><th>Porcentajes (académico · horas · créditos)</th><th>Docentes</th><th>Sugerencia final</th><th>Decisión del coordinador</th></tr></thead>
    <tbody>{revision.informe.parejas.map(p=>{
     const d=revision.decisiones.find(x=>clave(x.asignaturaAprobadaId,x.asignaturaReceptoraId)===clave(p.asignaturaAprobadaId,p.asignaturaReceptoraId));
     return <tr key={clave(p.asignaturaAprobadaId,p.asignaturaReceptoraId)}>
      <td>{nombre(p.asignaturaAprobadaId)}</td>
      <td>{p.receptoraCodigo??'—'} · {p.receptoraNombre??'—'}</td>
      <td>{p.agente.estado}{p.agente.sugerencia?` · ${p.agente.sugerencia}`:''}</td>
      <td>{pct(p.agente.porcentajeAcademico)} · {pct(p.agente.porcentajeHoras)} · {pct(p.agente.porcentajeCreditos)}</td>
      <td><ul>{p.docentes.map(x=><li key={x.docenteId}>{`${x.docente}: ${posturas[x.postura]} · ${decisiones[x.decision]}${x.observacion?` · ${x.observacion}`:''}`}</li>)}</ul></td>
      <td><strong>{resultados[p.sugerenciaFinal]}</strong></td>
      <td>{p.sugerenciaFinal!=='SinAcuerdo'?'No requiere':d?`${resultados[d.resultado]} · ${d.justificacion} · Decidido por ${d.coordinador} el ${fecha(d.fecha)}`:'Pendiente'}</td>
     </tr>;})}</tbody></table></div>
   {revision.informe.aprobadasSinAnalisis.length>0&&<p>Sin análisis docente: {revision.informe.aprobadasSinAnalisis.map(nombre).join(', ')}.</p>}
   {revision.informe.advertencias.map(a=><p key={a}>{a}</p>)}
   {finalizada?<p role="status">{`Solicitud finalizada por ${finalizada.finalizadoPor} el ${fecha(finalizada.fecha)}. Ya no admite cambios.`}</p>:<>
    {revision.informe.parejas.filter(p=>p.sugerenciaFinal==='SinAcuerdo').map(p=><FormDecision key={clave(p.asignaturaAprobadaId,p.asignaturaReceptoraId)} pareja={p} ocupado={ocupado}
     actual={revision.decisiones.find(x=>clave(x.asignaturaAprobadaId,x.asignaturaReceptoraId)===clave(p.asignaturaAprobadaId,p.asignaturaReceptoraId))}
     guardar={(resultado,justificacion)=>decidir(p,resultado,justificacion)}/>)}
    <p>{revision.decisionesPendientes>0
     ?`Falta decidir ${revision.decisionesPendientes} pareja${revision.decisionesPendientes===1?'':'s'} «Sin acuerdo» antes de finalizar.`
     :'Al finalizar, la solicitud queda «Finalizada» con la fecha y su usuario, y ya no admite cambios.'}</p>
    <button className="app-button" disabled={ocupado||revision.decisionesPendientes>0} onClick={finalizar}>Finalizar solicitud</button>
   </>}
  </>}
 </section>;
}
