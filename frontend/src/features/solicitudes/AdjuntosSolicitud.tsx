import {useEffect,useState} from 'react';
import type {FormEvent} from 'react';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Adjunto={id:string;tipo:string;nombreArchivo:string;tamanoBytes:number;huellaSha256:string;subidoPor:string;fechaSubidaUtc:string};
const limite=20*1024*1024;
const mensaje=(e:unknown)=>e instanceof Error?e.message:'No se pudo completar la operación.';
async function leer<T>(response:Response):Promise<T>{
 if(!response.ok){let text='No se pudo completar la operación.';try{const problem=await response.json();text=problem.detail??problem.title??text;}catch{/* Respuesta sin cuerpo. */}throw new Error(text);}
 return response.json() as Promise<T>;
}
export function AdjuntosSolicitud({solicitudId,request,puedeSubir,finalizada=false}:{solicitudId:string;request:Request;puedeSubir:boolean;finalizada?:boolean}){
 const [items,setItems]=useState<Adjunto[]>([]),[tipo,setTipo]=useState('Historial académico'),[archivo,setArchivo]=useState<File|null>(null);
 const [cargando,setCargando]=useState(true),[ocupado,setOcupado]=useState(false),[error,setError]=useState(''),[aviso,setAviso]=useState('');
 async function actualizar(signal?:AbortSignal){setCargando(true);try{setItems(await leer<Adjunto[]>(await request(`/api/solicitudes/${solicitudId}/adjuntos`,{signal})));}catch(e){if(!signal?.aborted)setError(mensaje(e));}finally{if(!signal?.aborted)setCargando(false);}}
 useEffect(()=>{const controller=new AbortController();void actualizar(controller.signal);return()=>controller.abort();},[solicitudId,request]);
 async function subir(e:FormEvent){e.preventDefault();if(!archivo||ocupado)return;setError('');setAviso('');if(archivo.size>limite){setError('El archivo PDF no puede superar 20 MiB.');return;}setOcupado(true);
  try{const form=new FormData();form.append('tipo',tipo);form.append('archivo',archivo,archivo.name);await leer(await request(`/api/solicitudes/${solicitudId}/adjuntos`,{method:'POST',body:form}));setArchivo(null);setAviso('Adjunto guardado en el expediente.');await actualizar();}
  catch(e){setError(mensaje(e));}finally{setOcupado(false);}}
 async function descargar(adjunto:Adjunto){setError('');setOcupado(true);try{const response=await request(`/api/solicitudes/${solicitudId}/adjuntos/${adjunto.id}`);if(!response.ok)throw new Error((await leer<{title:string}>(response)).title);const url=URL.createObjectURL(await response.blob());const link=document.createElement('a');link.href=url;link.download=adjunto.nombreArchivo;link.click();URL.revokeObjectURL(url);}catch(e){setError(mensaje(e));}finally{setOcupado(false);}}
 return <section aria-labelledby={`adjuntos-${solicitudId}`}><h3 id={`adjuntos-${solicitudId}`}>Historial académico y sílabos</h3>
  <p>PDF de hasta 20 MiB. Se conservan en el expediente de la solicitud.</p>{error&&<p role="alert">{error}</p>}{aviso&&<p role="status">{aviso}</p>}
  {puedeSubir&&!finalizada&&<form onSubmit={subir}><fieldset className="solicitud-fields" disabled={ocupado}><legend>Adjuntar documento</legend>
   <label>Tipo de documento<select value={tipo} onChange={e=>setTipo(e.target.value)}><option>Historial académico</option><option>Sílabo</option></select></label>
   <label>Archivo PDF<input type="file" accept="application/pdf,.pdf" onChange={e=>setArchivo(e.target.files?.[0]??null)} required/></label>
  </fieldset><button className="app-button app-button--primary" disabled={ocupado||!archivo}>{ocupado?'Guardando…':'Adjuntar PDF'}</button></form>}
  {cargando?<p>Cargando adjuntos…</p>:items.length===0?<p>No hay documentos adjuntos.</p>:<ul>{items.map(a=><li key={a.id}>{a.tipo}: {a.nombreArchivo} · {(a.tamanoBytes/1024/1024).toFixed(2)} MiB · SHA-256 {a.huellaSha256} · subido {new Date(a.fechaSubidaUtc).toLocaleString('es-EC')} <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={()=>descargar(a)}>Descargar</button></li>)}</ul>}
 </section>;
}
