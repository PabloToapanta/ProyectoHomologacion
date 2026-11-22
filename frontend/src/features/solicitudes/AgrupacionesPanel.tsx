import {useState} from 'react';
import {ResultadoAgrupacion,estadoGrupo,type EvaluacionGrupo} from './ResultadoAgrupacion';
import {RevisionAgrupacionPanel} from './RevisionAgrupacionPanel';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Aporte={asignaturaAprobadaId:string;asignaturaReceptoraId:string;horasAportadas:number|null;creditosAportados:number|null};
type Grupo={grupoId:string;version:string;congelado:boolean;origenes:string[];receptoras:string[];aportes:Aporte[];ultimoAnalisis:{evaluacion:EvaluacionGrupo}|null};
type Opciones={origenes:{id:string;horas:number;creditos:number;estado:string;elegible:boolean|null;motivos:string[]}[];receptoras:{id:string;codigo:string;nombre:string;nivel:number;horas:number;creditos:number}[];vinculos:{asignaturaAprobadaId:string;asignaturaReceptoraId:string;habilitado:boolean;motivo:string}[]};
const leyenda='Homologación por agrupación — regla de análisis pendiente de validación institucional';
export function AgrupacionesPanel({request,solicitudId,aprobadas}:{request:Request;solicitudId:string;aprobadas:{id:string;nombreAsignatura:string}[]}){
 const ruta=`/api/homologaciones/solicitudes/${solicitudId}/agrupaciones`;
 const [opciones,setOpciones]=useState<Opciones|null>(null),[lista,setLista]=useState<Grupo[]>([]),[grupo,setGrupo]=useState<Grupo|null>(null),[aportes,setAportes]=useState<Aporte[]>([]);
 const [sucio,setSucio]=useState(false);
 const [error,setError]=useState(''),[aviso,setAviso]=useState(''),[busy,setBusy]=useState(false),[evaluacion,setEvaluacion]=useState<EvaluacionGrupo|null>(null),[advertencias,setAdvertencias]=useState<string[]>([]);
 const nombre=(id:string)=>aprobadas.find(a=>a.id===id)?.nombreAsignatura??opciones?.receptoras.find(r=>r.id===id)?.nombre??id;
 async function leer<T>(path:string,init?:RequestInit):Promise<T>{const r=await request(path,init);if(!r.ok){let texto='No se completó la operación.';try{const p=await r.json();texto=p.detail??p.title??texto;}catch{/* sin cuerpo */}throw Object.assign(new Error(texto),{status:r.status});}return r.status===204?undefined as T:r.json() as Promise<T>;}
 function cargar(g:Grupo|null){setSucio(false);setGrupo(g);setAportes(g?.aportes??[]);setEvaluacion(g?.ultimoAnalisis?.evaluacion??null);setAdvertencias([]);setAviso('');}
 async function accion(fn:()=>Promise<void>){if(busy)return;setBusy(true);setError('');setAviso('');try{await fn();}catch(e){let mensaje=e instanceof Error?e.message:'Operación fallida.';
  if((e as {status?:number}).status===409&&grupo){try{const g=await leer<Grupo>(`${ruta}/${grupo.grupoId}`);cargar(g);mensaje+=' Se recargó la composición vigente; revise sus datos antes de reintentar.';}catch{mensaje+=' No fue posible recargar; vuelva a abrir el grupo.';cargar(null);}}setError(mensaje);
 }finally{setBusy(false);}}
 const actualizarLista=(g:Grupo)=>setLista(prev=>[...prev.filter(x=>x.grupoId!==g.grupoId),g]);
 const abrir=()=>accion(async()=>{const [o,gs]=await Promise.all([leer<Opciones>(ruta+'/opciones'),leer<Grupo[]>(ruta)]);setOpciones(o);setLista(gs);cargar(null);});
 const ver=(id:string)=>accion(async()=>cargar(await leer<Grupo>(`${ruta}/${id}`)));
 const headers=()=>({'Content-Type':'application/json',...(grupo?{'If-Match':grupo.version}:{})});
 const guardar=()=>accion(async()=>{const input={origenes:[...new Set(aportes.map(a=>a.asignaturaAprobadaId))],receptoras:[...new Set(aportes.map(a=>a.asignaturaReceptoraId))],aportes};
  const g=await leer<Grupo>(grupo?`${ruta}/${grupo.grupoId}`:ruta,{method:grupo?'PUT':'POST',headers:headers(),body:JSON.stringify(input)});cargar(g);actualizarLista(g);setAviso('Borrador guardado.');});
 const analizar=()=>accion(async()=>{if(!grupo)return;const r=await leer<{guardado:boolean;evaluacion:EvaluacionGrupo;detalle:Grupo;advertencias:string[]}>(`${ruta}/${grupo.grupoId}/analisis`,{method:'POST',headers:headers()});cargar(r.detalle);actualizarLista(r.detalle);setEvaluacion(r.evaluacion);setAdvertencias(r.advertencias);setAviso(r.guardado?'Análisis del grupo guardado.':'Información incompleta: complete el reparto; no se guardó ni congeló el borrador.');});
 const eliminar=()=>accion(async()=>{if(!grupo)return;await leer(`${ruta}/${grupo.grupoId}`,{method:'DELETE',headers:headers()});setLista(prev=>prev.filter(x=>x.grupoId!==grupo.grupoId));cargar(null);setAviso('Borrador eliminado; reservas liberadas.');});
 function activar(o:string,r:string){setSucio(true);setAportes(prev=>prev.some(a=>a.asignaturaAprobadaId===o&&a.asignaturaReceptoraId===r)?prev.filter(a=>a.asignaturaAprobadaId!==o||a.asignaturaReceptoraId!==r):[...prev,{asignaturaAprobadaId:o,asignaturaReceptoraId:r,horasAportadas:null,creditosAportados:null}]);setEvaluacion(null);}
 function cantidad(o:string,r:string,campo:'horasAportadas'|'creditosAportados',valor:string){setSucio(true);setAportes(prev=>prev.map(a=>a.asignaturaAprobadaId===o&&a.asignaturaReceptoraId===r?{...a,[campo]:valor===''?null:Number(valor)}:a));setEvaluacion(null);}
 const sum=(os:Aporte[],key:'horasAportadas'|'creditosAportados')=>Math.round(os.reduce((s,a)=>s+(a[key]??0),0)*100)/100;
 const desbordado=opciones?.origenes.some(o=>{const as=aportes.filter(a=>a.asignaturaAprobadaId===o.id);return sum(as,'horasAportadas')>o.horas||sum(as,'creditosAportados')>o.creditos;});
 const precision=aportes.some(a=>[a.horasAportadas,a.creditosAportados].some(n=>n!==null&&(!Number.isFinite(n)||n<0||Math.abs(n*100-Math.round(n*100))>1e-8)));
 return <section aria-label="Agrupaciones N:M"><h3>Agrupación N:M</h3><p>{leyenda}. No es una decisión académica oficial.</p>
  <button className="app-button app-button--secondary" disabled={busy} onClick={abrir}>Abrir agrupaciones</button>
  {error&&<p role="alert">{error}</p>}{aviso&&<p role="status">{aviso}</p>}{busy&&<p role="status">Procesando agrupación…</p>}
  {opciones&&<><ul aria-label="Grupos de la solicitud">{lista.map(g=><li key={g.grupoId}><button disabled={busy} onClick={()=>ver(g.grupoId)}>Abrir grupo {g.grupoId}</button> · {g.congelado?'Composición congelada':'Borrador'}</li>)}</ul>
   <button disabled={busy} onClick={()=>cargar(null)}>Nuevo grupo</button><p>{grupo?`Grupo ${grupo.grupoId} · ${grupo.congelado?'Composición congelada':'Borrador editable'}`:'Seleccione las celdas del conjunto. Para una sola pareja use el análisis 1:1.'}</p>
   <div className="solicitudes-table"><table><caption>Orígenes × receptoras y aportes explícitos</caption><thead><tr><th>Origen aprobado</th>{opciones.receptoras.map(r=><th key={r.id}>{r.codigo} · {r.nombre} · Nivel {r.nivel}<br/>{r.horas} horas · {r.creditos} créditos<br/>Recibe {sum(aportes.filter(a=>a.asignaturaReceptoraId===r.id),'horasAportadas')} h / {sum(aportes.filter(a=>a.asignaturaReceptoraId===r.id),'creditosAportados')} cr</th>)}<th>Asignado / saldo</th></tr></thead>
   <tbody>{opciones.origenes.map(o=>{const usados=aportes.filter(a=>a.asignaturaAprobadaId===o.id);return <tr key={o.id}><th>{nombre(o.id)}<br/>{o.horas} horas · {o.creditos} créditos<br/>{o.motivos.join(' ')}</th>{opciones.receptoras.map(r=>{const a=aportes.find(a=>a.asignaturaAprobadaId===o.id&&a.asignaturaReceptoraId===r.id);const v=opciones.vinculos.find(v=>v.asignaturaAprobadaId===o.id&&v.asignaturaReceptoraId===r.id);return <td key={r.id}>
    <label><input type="checkbox" aria-label={`Vínculo ${nombre(o.id)} a ${r.codigo}`} checked={!!a} disabled={busy||grupo?.congelado||(!a&&!v?.habilitado)} onChange={()=>activar(o.id,r.id)}/>Vincular</label>{!v?.habilitado&&<p>{v?.motivo??'No se pudo verificar la revisión.'}</p>}
    {a&&<><label>Horas<input aria-label={`Horas ${o.id} a ${r.codigo}`} type="number" step="0.01" min="0" value={a.horasAportadas??''} disabled={busy||grupo?.congelado} onChange={e=>cantidad(o.id,r.id,'horasAportadas',e.target.value)}/></label>
     <label>Créditos<input aria-label={`Créditos ${o.id} a ${r.codigo}`} type="number" step="0.01" min="0" value={a.creditosAportados??''} disabled={busy||grupo?.congelado} onChange={e=>cantidad(o.id,r.id,'creditosAportados',e.target.value)}/></label></>}
   </td>;})}<td>Asignado: {sum(usados,'horasAportadas')} horas · {sum(usados,'creditosAportados')} créditos<p>Saldo: {Math.round((o.horas-sum(usados,'horasAportadas'))*100)/100} horas · {Math.round((o.creditos-sum(usados,'creditosAportados'))*100)/100} créditos</p></td></tr>;})}</tbody></table></div>
   <p>{sucio&&grupo?'Guarde las correcciones antes de analizar. ':''}Vacío significa pendiente; cero significa que no aporta. No se distribuyen cantidades automáticamente.</p>{desbordado&&<p role="alert">El aporte de un origen supera sus horas o créditos disponibles.</p>}{precision&&<p role="alert">Use cero o cantidades positivas con hasta dos decimales.</p>}
   {!grupo?.congelado&&<button className="app-button" disabled={busy||!!desbordado||precision||aportes.length<2} onClick={guardar}>Guardar borrador</button>}
   {grupo&&<><button className="app-button" disabled={busy||sucio} onClick={analizar}>Analizar grupo guardado</button>{!grupo.congelado&&<button disabled={busy} onClick={eliminar}>Eliminar borrador</button>}</>}
   {evaluacion&&<ResultadoAgrupacion evaluacion={evaluacion} nombre={nombre}/>} {advertencias.map(a=><p key={a}>{a}</p>)}
   {grupo?.ultimoAnalisis&&<RevisionAgrupacionPanel key={`${grupo.grupoId}/${grupo.version}`} request={request} ruta={`${ruta}/${grupo.grupoId}/revisiones`} sugerencia={estadoGrupo(grupo.ultimoAnalisis.evaluacion.estado)}/>}
  </>}
 </section>;
}
