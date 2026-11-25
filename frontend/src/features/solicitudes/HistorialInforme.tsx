import {fechaHora} from '../../shared/fecha';
import {useEffect,useState} from 'react';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Evento={id:string;tipo:'Generado'|'Remitido'|'Finalizado';fecha:string;usuarioId:string;usuario:string;rol:string};
const tipos={Generado:'Informe generado',Remitido:'Informe remitido al coordinador',Finalizado:'Solicitud finalizada'} as const;
const esEvento=(e:unknown):e is Evento=>typeof e==='object'&&e!==null&&typeof (e as Evento).id==='string'&&(e as Evento).tipo in tipos;

/** HU-25: historial del informe para el coordinador, en el orden cronológico que devuelve el servidor. Solo lectura. */
export function HistorialInforme({request,solicitudId}:{request:Request;solicitudId:string}) {
 const ruta=`/api/homologaciones/solicitudes/${solicitudId}/informe/historial`;
 const [eventos,setEventos]=useState<Evento[]|null>(null),[error,setError]=useState('');
 useEffect(()=>{
  const controller=new AbortController();setEventos(null);setError('');
  request(ruta,{signal:controller.signal}).then(async r=>{
   if(controller.signal.aborted)return;
   if(!r.ok){let titulo='No se pudo consultar el historial del informe.';try{const p=await r.json();titulo=p.detail??p.title??titulo;}catch{/* sin detalle */}throw new Error(titulo);}
   const datos:unknown=await r.json();
   if(!controller.signal.aborted)setEventos(Array.isArray(datos)?datos.filter(esEvento):[]);
  }).catch(e=>{if(!controller.signal.aborted)setError(e instanceof Error?e.message:'No se pudo consultar el historial del informe.');});
  return ()=>controller.abort();
 // request es estable; la ruta identifica la solicitud.
 },[request,ruta]);
 return <section aria-labelledby="historial-informe-titulo">
  <h3 id="historial-informe-titulo">Historial del informe</h3>
  {error&&<p role="alert">{error}</p>}
  {!error&&eventos===null&&<p>Consultando historial…</p>}
  {eventos?.length===0&&<p>Sin eventos registrados: el informe aún no se ha generado.</p>}
  {eventos&&eventos.length>0&&<ol aria-label="Historial del informe">{eventos.map(e=><li key={e.id}>
   {`${tipos[e.tipo]} · ${fechaHora(e.fecha)} · ${e.usuario} (${e.rol})`}
  </li>)}</ol>}
 </section>;
}
