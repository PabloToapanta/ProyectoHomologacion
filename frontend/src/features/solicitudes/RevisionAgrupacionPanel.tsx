import {useEffect,useState} from 'react';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Revision={docenteId:string;docente:string;postura:string;decision:string;observacion:string|null};
export function RevisionAgrupacionPanel({request,ruta,sugerencia}:{request:Request;ruta:string;sugerencia:string}){
 const [revisiones,setRevisiones]=useState<Revision[]>([]),[postura,setPostura]=useState('Ajuste'),[decision,setDecision]=useState(''),[observacion,setObservacion]=useState(''),[error,setError]=useState(''),[aviso,setAviso]=useState(''),[busy,setBusy]=useState(false);
 useEffect(()=>{const c=new AbortController();request(ruta,{signal:c.signal}).then(async r=>{if(!r.ok)throw new Error('No se pudieron consultar las revisiones grupales.');const d=await r.json() as Revision[];if(!c.signal.aborted)setRevisiones(d);}).catch(e=>{if(!c.signal.aborted)setError(String(e));});return()=>c.abort();},[request,ruta]);
 async function guardar(e:React.FormEvent){e.preventDefault();if(busy)return;setBusy(true);setError('');setAviso('');try{
  const r=await request(ruta,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify({postura,decision:postura==='Ajuste'?decision:null,observacion:observacion.trim()||null})});
  if(!r.ok){const p=await r.json();throw new Error(p.detail??p.title??'No se guardó la revisión.');}const d=await r.json() as Revision;setRevisiones(prev=>[...prev.filter(x=>x.docenteId!==d.docenteId),d]);setAviso('Revisión del grupo guardada.');
 }catch(e){setError(e instanceof Error?e.message:'No se guardó la revisión.');}finally{setBusy(false);}}
 const completa=sugerencia==='Homologable'||sugerencia==='NoHomologable';
 return <section aria-label="Revisión docente del grupo"><h4>Mi revisión de toda la agrupación</h4><p>Una decisión por docente y grupo. El informe generado impide correcciones posteriores.</p>
  {error&&<p role="alert">{error}</p>}{aviso&&<p role="status">{aviso}</p>}<form onSubmit={guardar}><fieldset disabled={busy}>
  <label>Postura del grupo<select value={postura} onChange={e=>setPostura(e.target.value)}><option value="Ajuste">Ajuste</option><option value="DeAcuerdo" disabled={!completa}>De acuerdo con la sugerencia</option></select></label>
  {postura==='Ajuste'&&<label>Decisión del grupo<select value={decision} required onChange={e=>setDecision(e.target.value)}><option value="">Seleccione</option><option value="Homologable">Homologable</option><option value="NoHomologable">No homologable</option></select></label>}
  <label>Observación del grupo<textarea maxLength={2000} required={postura==='Ajuste'} value={observacion} onChange={e=>setObservacion(e.target.value)}/></label>
  <button className="app-button" disabled={busy}>Guardar mi revisión del grupo</button></fieldset></form>
  <ul aria-label="Revisiones del grupo">{revisiones.map(r=><li key={r.docenteId}>{r.docente} · {r.postura} · {r.decision} · {r.observacion}</li>)}</ul>
 </section>;
}
