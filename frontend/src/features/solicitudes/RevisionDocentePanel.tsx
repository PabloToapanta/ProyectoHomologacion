import {useEffect,useState} from 'react';
import type {FormEvent} from 'react';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Postura='DeAcuerdo'|'Ajuste';
type Decision='Homologable'|'NoHomologable';
export type RevisionDocente={solicitudId:string;asignaturaAprobadaId:string;asignaturaReceptoraId:string;docenteId:string;docente:string;postura:Postura;decision:Decision;
 observacion:string|null;estadoAgente:string;sugerenciaAgente:string|null;fecha:string};
const posturas={DeAcuerdo:'De acuerdo',Ajuste:'Ajuste'} as const;
const decisiones={Homologable:'Homologable',NoHomologable:'No homologable'} as const;

/** HU-31: análisis propio del docente sobre la pareja seleccionada, junto al del agente y al del otro docente. No es una resolución oficial. */
export function RevisionDocentePanel({request,solicitudId,aprobadaId,receptoraId,sugerenciaAgente}:{request:Request;solicitudId:string;aprobadaId:string;receptoraId:string;sugerenciaAgente:string|null}) {
 const ruta=`/api/homologaciones/solicitudes/${solicitudId}/revisiones-docentes`;
 const [revisiones,setRevisiones]=useState<RevisionDocente[]>([]),[postura,setPostura]=useState<Postura|''>(''),[decision,setDecision]=useState<Decision|''>('');
 const [observacion,setObservacion]=useState(''),[ocupado,setOcupado]=useState(false),[error,setError]=useState(''),[aviso,setAviso]=useState('');
 async function leer<T>(init?:RequestInit):Promise<T> {
  const response=await request(ruta,init);
  if(!response.ok){let texto='No fue posible consultar o guardar el análisis docente.';try{const p=await response.json();texto=p.detail??p.title??texto;}catch{/* sin cuerpo */}throw new Error(texto);}
  return response.json() as Promise<T>;
 }
 useEffect(()=>{
  const controller=new AbortController();setPostura('');setDecision('');setObservacion('');setAviso('');setError('');
  leer<RevisionDocente[]>({signal:controller.signal}).then(r=>{if(!controller.signal.aborted)setRevisiones(r);})
   .catch(e=>{if(!controller.signal.aborted)setError(e instanceof Error?e.message:'No se pudieron consultar los análisis docentes.');});
  return ()=>controller.abort();
 // request es estable; la pareja define qué se muestra.
 },[request,ruta,aprobadaId,receptoraId]);
 async function guardar(event:FormEvent) {
  event.preventDefault();if(ocupado||!postura)return;setOcupado(true);setError('');setAviso('');
  try{
   const guardada=await leer<RevisionDocente>({method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify({asignaturaAprobadaId:aprobadaId,asignaturaReceptoraId:receptoraId,
    postura,decision:postura==='Ajuste'?decision||null:null,observacion:observacion.trim()||null})});
   setRevisiones(prev=>[...prev.filter(r=>r.docenteId!==guardada.docenteId||r.asignaturaAprobadaId!==aprobadaId||r.asignaturaReceptoraId!==receptoraId),guardada]);
   setAviso('Su análisis quedó registrado.');
  }catch(e){setError(e instanceof Error?e.message:'No se registró el análisis.');}finally{setOcupado(false);}
 }
 const dePareja=revisiones.filter(r=>r.asignaturaAprobadaId===aprobadaId&&r.asignaturaReceptoraId===receptoraId);
 const conSugerencia=sugerenciaAgente==='Homologable'||sugerenciaAgente==='No homologable';
 return <section aria-labelledby="revision-docente-titulo">
  <h4 id="revision-docente-titulo">Mi análisis</h4>
  <p>{conSugerencia?`Sugerencia del agente: ${sugerenciaAgente}.`:'El agente no dio una sugerencia para esta pareja: registre su decisión con una observación.'} Cada docente asignado registra el suyo; puede corregirlo mientras no se genere el informe.</p>
  {error&&<p role="alert">{error}</p>}{aviso&&<p role="status">{aviso}</p>}
  <form onSubmit={guardar}><fieldset disabled={ocupado} className="solicitud-fields"><legend>Postura</legend>
   <label><input type="radio" name="postura" value="DeAcuerdo" disabled={!conSugerencia} checked={postura==='DeAcuerdo'} onChange={()=>setPostura('DeAcuerdo')}/>De acuerdo con el agente</label>
   <label><input type="radio" name="postura" value="Ajuste" checked={postura==='Ajuste'} onChange={()=>setPostura('Ajuste')}/>Ajuste con mi decisión</label>
   {postura==='Ajuste'&&<label>Mi decisión<select required value={decision} onChange={e=>setDecision(e.target.value as Decision)}><option value="">Seleccione</option><option value="Homologable">Homologable</option><option value="NoHomologable">No homologable</option></select></label>}
   {postura&&<label>Observación<textarea required={postura==='Ajuste'} maxLength={2000} value={observacion} onChange={e=>setObservacion(e.target.value)}/></label>}
  </fieldset><button className="app-button" disabled={ocupado||!postura}>{ocupado?'Guardando…':'Guardar mi análisis'}</button></form>
  {dePareja.length>0?<ul aria-label="Análisis de los docentes">{dePareja.map(r=><li key={r.docenteId}>
   {r.docente} · {posturas[r.postura]} · {decisiones[r.decision]}{r.observacion?` · ${r.observacion}`:''} · {new Date(r.fecha).toLocaleString('es-EC')}
  </li>)}</ul>:<p>Ningún docente ha registrado su análisis para esta pareja.</p>}
 </section>;
}
