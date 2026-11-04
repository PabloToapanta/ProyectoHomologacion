import {useEffect,useState} from 'react';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Aprobada={id:string;nombreAsignatura:string};
type DocenteEnInforme={docenteId:string;docente:string;postura:'DeAcuerdo'|'Ajuste';decision:'Homologable'|'NoHomologable';observacion:string|null;fecha:string};
type Pareja={asignaturaAprobadaId:string;asignaturaReceptoraId:string;receptoraCodigo:string|null;receptoraNombre:string|null;
 agente:{estado:string;sugerencia:string|null;porcentajeAcademico:number|null;porcentajeHoras:number|null;porcentajeCreditos:number|null};
 docentes:DocenteEnInforme[];sugerenciaFinal:'Convalidar'|'NoConvalidar'|'SinAcuerdo'};
type Informe={solicitudId:string;tipoCambio:string;fecha:string;generadoPor:string;parejas:Pareja[];aprobadasSinAnalisis:string[];
 convalidar:number;noConvalidar:number;sinAcuerdo:number;advertencias:string[]};
const posturas={DeAcuerdo:'De acuerdo',Ajuste:'Ajuste'} as const;
const decisiones={Homologable:'Homologable',NoHomologable:'No homologable'} as const;
const finales={Convalidar:'Convalidar',NoConvalidar:'No convalidar',SinAcuerdo:'Sin acuerdo'} as const;
const pct=(v:number|null)=>v===null?'—':`${v.toLocaleString('es-EC',{maximumFractionDigits:2})} %`;

/** HU-23: informe fijo de la solicitud; cualquiera de los dos docentes asignados lo genera. No remite al coordinador (HU-32). */
export function InformePanel({request,solicitudId,aprobadas}:{request:Request;solicitudId:string;aprobadas:Aprobada[]}) {
 const ruta=`/api/homologaciones/solicitudes/${solicitudId}/informe`;
 const [informe,setInforme]=useState<Informe|null>(null),[pendiente,setPendiente]=useState(false),[cargando,setCargando]=useState(true);
 const [ocupado,setOcupado]=useState(false),[error,setError]=useState('');
 async function motivo(r:Response,defecto:string){try{const p=await r.json();return p.detail??p.title??defecto;}catch{return defecto;}}
 useEffect(()=>{
  const controller=new AbortController();setCargando(true);setError('');setInforme(null);setPendiente(false);
  request(ruta,{signal:controller.signal}).then(async r=>{
   if(controller.signal.aborted)return;
   if(r.status===404){setPendiente(true);return;}
   if(!r.ok)throw new Error(await motivo(r,'No se pudo consultar el informe.'));
   setInforme(await r.json() as Informe);
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
   setInforme(await r.json() as Informe);setPendiente(false);
  }catch(e){setError(e instanceof Error?e.message:'No se pudo generar el informe.');}finally{setOcupado(false);}
 }
 const nombre=(id:string)=>aprobadas.find(a=>a.id===id)?.nombreAsignatura??'Asignatura sin nombre registrado';
 return <section aria-labelledby="informe-titulo">
  <h3 id="informe-titulo">Informe de homologación</h3>
  {cargando&&<p>Consultando informe…</p>}{error&&<p role="alert">{error}</p>}
  {pendiente&&!informe&&<>
   <p>El informe aún no se ha generado. Puede generarlo cualquiera de los dos docentes cuando cada pareja analizada tenga el análisis de ambos. Una vez generado es fijo y los análisis docentes quedan cerrados.</p>
   <button className="app-button" disabled={ocupado} onClick={generar}>{ocupado?'Generando…':'Generar informe'}</button>
  </>}
  {informe&&<>
   <p>Generado por {informe.generadoPor} el {new Date(informe.fecha).toLocaleString('es-EC')}. Convalidar: {informe.convalidar} · No convalidar: {informe.noConvalidar} · Sin acuerdo: {informe.sinAcuerdo}.</p>
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
   {informe.aprobadasSinAnalisis.length>0&&<p>Sin análisis docente: {informe.aprobadasSinAnalisis.map(nombre).join(', ')}.</p>}
   {informe.advertencias.map(a=><p key={a}>{a}</p>)}
  </>}
 </section>;
}
