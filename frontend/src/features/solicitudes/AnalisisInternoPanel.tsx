import {useEffect,useState} from 'react';
import {AnalisisSolicitud,type Analisis} from './SolicitudesDocentePage';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Aprobada={id:string;nombreAsignatura:string};
type Receptora={id:string;codigo:string;nombre:string;nivel:number};
type Resultado=Analisis&{asignaturaAprobadaId:string;asignaturaReceptoraId:string;advertencias:string[]};
type Guardado={fecha:string;analisis:Resultado};
const pendiente:Analisis={estado:'Pendiente',mensaje:'Seleccione una pareja y ejecute su análisis.',resultado:null};
export function AnalisisInternoPanel({request,solicitudId,aprobadas,inicial}:{request:Request;solicitudId:string;aprobadas:Aprobada[];inicial:Analisis}) {
 const ruta=`/api/homologaciones/solicitudes/${solicitudId}/analisis-interno`;
 const [abierto,setAbierto]=useState(false),[ocupado,setOcupado]=useState(false),[error,setError]=useState('');
 const [receptoras,setReceptoras]=useState<Receptora[]>([]),[guardados,setGuardados]=useState<Guardado[]>([]);
 const [aprobada,setAprobada]=useState(''),[receptora,setReceptora]=useState(''),[resultado,setResultado]=useState<Resultado|null>(null),[confirmado,setConfirmado]=useState(false);
 async function leer<T>(path:string,init?:RequestInit):Promise<T> {
  const response=await request(path,init);
  if(!response.ok){let texto='No fue posible consultar o guardar el análisis.';try{const p=await response.json();texto=p.detail??p.title??texto;}catch{/* sin cuerpo */}throw new Error(texto);}
  return response.json() as Promise<T>;
 }
 useEffect(()=>{
  const controller=new AbortController();
  leer<Guardado[]>(ruta,{signal:controller.signal}).then(datos=>{
   if(!controller.signal.aborted){setGuardados(datos);setResultado(datos[0]?.analisis??null);}
  }).catch(e=>{if(!controller.signal.aborted)setError(e instanceof Error?e.message:'No se pudo recuperar el análisis guardado.');});
  return ()=>controller.abort();
 // La identidad de request procede del proveedor estable; ruta identifica la solicitud.
 },[request,ruta]);
 async function consultar(){
  if(ocupado)return;setOcupado(true);setError('');setConfirmado(false);
  try{const [opciones,datos]=await Promise.all([leer<Receptora[]>(ruta+'/opciones'),leer<Guardado[]>(ruta)]);
   setReceptoras(opciones);setGuardados(datos);setResultado(datos[0]?.analisis??null);setAbierto(true);
  }catch(e){setError(e instanceof Error?e.message:'No se pudo consultar el análisis.');}finally{setOcupado(false);}
 }
 async function ejecutar(){
  if(ocupado||!aprobada||!receptora)return;setOcupado(true);setError('');setConfirmado(false);setResultado(null);
  try{const r=await leer<Resultado>(ruta,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({asignaturaAprobadaId:aprobada,asignaturaReceptoraId:receptora})});
   setResultado(r);setConfirmado(true);
   setGuardados(prev=>[{fecha:new Date().toISOString(),analisis:r},...prev.filter(x=>x.analisis.asignaturaAprobadaId!==r.asignaturaAprobadaId||x.analisis.asignaturaReceptoraId!==r.asignaturaReceptoraId)]);
  }catch(e){setError(e instanceof Error?e.message:'No se confirma ningún guardado.');}finally{setOcupado(false);}
 }
 function cambiar(setter:(v:string)=>void,value:string){setter(value);setResultado(null);setConfirmado(false);}
 return <section aria-label="Análisis interno por asignatura">
  <button className="app-button app-button--secondary" disabled={ocupado} onClick={consultar}>Consultar análisis interno</button>
  {ocupado&&<p role="status">Procesando análisis…</p>}{error&&<p role="alert">{error}</p>}
  {abierto&&<>
   <p>El motor calcula la equivalencia y qwen3:4b explica las evidencias. Cada pareja conserva su último intento. No es una aprobación oficial.</p>
   <label>Asignatura aprobada<select value={aprobada} disabled={ocupado} onChange={e=>cambiar(setAprobada,e.target.value)}><option value="">Seleccione</option>{aprobadas.map(a=><option key={a.id} value={a.id}>{a.nombreAsignatura}</option>)}</select></label>
   <label>Asignatura receptora<select value={receptora} disabled={ocupado} onChange={e=>cambiar(setReceptora,e.target.value)}><option value="">Seleccione</option>{receptoras.map(a=><option key={a.id} value={a.id}>Nivel {a.nivel} · {a.codigo} · {a.nombre}</option>)}</select></label>
   <button className="app-button" disabled={ocupado||!aprobada||!receptora} onClick={ejecutar}>Ejecutar análisis y guardar</button>
   {guardados.length>0&&<ul aria-label="Análisis guardados por pareja">{guardados.map(x=><li key={`${x.analisis.asignaturaAprobadaId}/${x.analisis.asignaturaReceptoraId}`}>
    <button className="app-button app-button--secondary" disabled={ocupado} onClick={()=>{setResultado(x.analisis);setConfirmado(false);}}>Ver {aprobadas.find(a=>a.id===x.analisis.asignaturaAprobadaId)?.nombreAsignatura??'Asignatura histórica'} → {receptoras.find(a=>a.id===x.analisis.asignaturaReceptoraId)?.nombre??'Receptora histórica'} ({x.analisis.estado})</button>
   </li>)}</ul>}
  </>}
  {confirmado&&<p role="status">Análisis guardado.</p>}
  <AnalisisSolicitud analisis={resultado??(abierto?pendiente:inicial)}/>
  {resultado?.advertencias?.map(a=><p key={a}>{a}</p>)}
 </section>;
}
