import {useEffect,useState} from 'react';
import type {FormEvent} from 'react';
import {FormDialog} from '../../shared/ui/FormDialog';
import {AsignaturasAprobadas} from './AsignaturasAprobadas';
import type {AsignaturaAprobada,AsignaturaAprobadaInput,AsignaturaCatalogo} from './AsignaturasAprobadas';
import {DocentesAsignados} from './DocentesAsignados';
import type {DocenteAsignado,DocenteDisponible} from './DocentesAsignados';
import {DatosCambioExterno} from './DatosCambioExterno';
import type {DatosExternos} from './DatosCambioExterno';
import '../catalogo/catalogo.css';
type Request=(path:string,init?:RequestInit)=>Promise<Response>;
type Carrera={id:string;nombre:string};
type Input={numeroQuipux:string;identificacionEstudiante:string;nombreEstudiante:string;tipoCambio:'Interno'|'Externo';carreraOrigenId:string|null;institucionOrigen:string|null;carreraOrigenExterna:string|null;carreraReceptoraId:string};
type Solicitud=Input&{id:string;estado:string;fechaRegistro:string;asignaturasAprobadas?:AsignaturaAprobada[];docentesAsignados?:DocenteAsignado[];periodosCursados?:number|null;cambiosPreviosCarrera?:number|null};
const vacia:Input={numeroQuipux:'',identificacionEstudiante:'',nombreEstudiante:'',tipoCambio:'Interno',carreraOrigenId:null,institucionOrigen:null,carreraOrigenExterna:null,carreraReceptoraId:''};
const mensaje=(e:unknown)=>e instanceof Error?e.message:'No se pudo completar la operación.';
export function SolicitudesPage({request}:{request:Request}) {
 const [items,setItems]=useState<Solicitud[]>([]),[carreras,setCarreras]=useState<Carrera[]>([]),[pagina,setPagina]=useState(1),[intento,setIntento]=useState(0);
 const [cargando,setCargando]=useState(true),[errorCarga,setErrorCarga]=useState(''),[aviso,setAviso]=useState(''),[error,setError]=useState('');
 const [form,setForm]=useState<Input|null>(null),[rn05,setRn05]=useState({periodos:'',cambios:''}),[detalle,setDetalle]=useState<Solicitud|null>(null),[ocupado,setOcupado]=useState(false);
 async function leer<T>(path:string,init?:RequestInit):Promise<T> {
  const response=await request(path,init);
  if(!response.ok){let text='No se pudo completar la operación.';try{const problem=await response.json();text=problem.detail??problem.title??text;}catch{/* respuesta sin cuerpo */}throw new Error(text);}
  return response.json() as Promise<T>;
 }
 useEffect(()=>{
  const controller=new AbortController();setCargando(true);setErrorCarga('');
  Promise.all([leer<Solicitud[]>(`/api/solicitudes?pagina=${pagina}`,{signal:controller.signal}),leer<Carrera[]>('/api/solicitudes/carreras',{signal:controller.signal})])
   .then(([s,c])=>{if(!controller.signal.aborted){setItems(s);setCarreras(c);}}).catch(e=>{if(!controller.signal.aborted)setErrorCarga(mensaje(e));}).finally(()=>{if(!controller.signal.aborted)setCargando(false);});
  return ()=>controller.abort();
 // leer usa exclusivamente el request estable del proveedor y los parámetros de esta carga.
 },[request,pagina,intento]);
 async function guardar(event:FormEvent) {
  event.preventDefault();if(!form||ocupado)return;setError('');setOcupado(true);
  try {
   const payload={...form,numeroQuipux:form.numeroQuipux.trim(),identificacionEstudiante:form.identificacionEstudiante.trim(),nombreEstudiante:form.nombreEstudiante.trim(),carreraOrigenId:form.tipoCambio==='Interno'?form.carreraOrigenId:null,institucionOrigen:form.tipoCambio==='Externo'?form.institucionOrigen?.trim():null,carreraOrigenExterna:form.tipoCambio==='Externo'?form.carreraOrigenExterna?.trim():null,
    // HU-19 (D4): opcionales; vacío = sin dato declarado.
    periodosCursados:form.tipoCambio==='Externo'&&rn05.periodos.trim()!==''?Number(rn05.periodos):null,cambiosPreviosCarrera:form.tipoCambio==='Externo'&&rn05.cambios.trim()!==''?Number(rn05.cambios):null};
   const saved=await leer<Solicitud>('/api/solicitudes',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload)});
   setForm(null);setAviso('Solicitud registrada correctamente.');if(pagina===1)setItems(old=>[saved,...old].slice(0,50));else setPagina(1);
  }catch(e){setError(mensaje(e));}finally{setOcupado(false);}
 }
 async function ver(id:string) {if(ocupado)return;setOcupado(true);setError('');try{setDetalle(await leer<Solicitud>(`/api/solicitudes/${id}`));}catch(e){setError(mensaje(e));}finally{setOcupado(false);}}
 async function registrarAsignatura(input:AsignaturaAprobadaInput) {
  if(!detalle)return;setOcupado(true);
  try {
   const saved=await leer<AsignaturaAprobada>(`/api/solicitudes/${detalle.id}/asignaturas-aprobadas`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(input)});
   // Se usa la respuesta del POST: una recarga fallida no debe aparentar que el registro no se guardó.
   setDetalle(actual=>actual&&{...actual,asignaturasAprobadas:[...(actual.asignaturasAprobadas??[]),saved]});
  }finally{setOcupado(false);}
 }
 async function guardarDatosExternos(datos:DatosExternos) {
  if(!detalle)return;setOcupado(true);
  try{const saved=await leer<Solicitud>(`/api/solicitudes/${detalle.id}/datos-cambio-externo`,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(datos)});setDetalle(saved);}
  finally{setOcupado(false);}
 }
 async function asignarDocentes(docenteIds:string[]) {
  if(!detalle)return;setOcupado(true);
  try {
   const saved=await leer<Solicitud>(`/api/solicitudes/${detalle.id}/docentes`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({docenteIds})});
   setDetalle(saved);setItems(old=>old.map(s=>s.id===saved.id?{...s,estado:saved.estado}:s));
  }finally{setOcupado(false);}
 }
 const nombreCarrera=(id:string|null)=>carreras.find(c=>c.id===id)?.nombre??id??'Sin referencia';
 return <section className="catalogo-section" aria-labelledby="solicitudes-title">
  <p className="eyebrow">COORDINACIÓN / SOLICITUDES</p><h1 id="solicitudes-title">Solicitudes de homologación</h1>
  <p>Registra el trámite que el estudiante envió por Quipux al coordinador de carrera. Cada número de trámite identifica una única solicitud.</p>
  {aviso&&<p role="status">{aviso}</p>}{error&&!form&&<p role="alert">{error}</p>}
  {cargando?<p>Cargando solicitudes…</p>:errorCarga?<div role="alert"><p>{errorCarga}</p><button className="app-button app-button--secondary" onClick={()=>setIntento(i=>i+1)}>Reintentar</button></div>:<>
   <button className="app-button app-button--primary" disabled={ocupado||!carreras.length} onClick={()=>{setForm({...vacia});setRn05({periodos:'',cambios:''});setError('');setAviso('');}}>Nueva solicitud</button>
   {!carreras.length&&<p>Se necesitan carreras FISEI vigentes en el catálogo antes de registrar solicitudes.</p>}
   {items.length===0?<p>{pagina===1?'No hay solicitudes registradas.':'No hay más solicitudes en esta página.'}</p>:<div className="solicitudes-table"><table><caption>Solicitudes registradas · Página {pagina}</caption><thead><tr><th>Trámite Quipux</th><th>Estudiante</th><th>Tipo</th><th>Estado</th><th>Consulta</th></tr></thead><tbody>{items.map(s=><tr key={s.id}><td>{s.numeroQuipux}</td><td>{s.nombreEstudiante}</td><td>{s.tipoCambio}</td><td>{s.estado}</td><td><button className="app-button app-button--secondary" disabled={ocupado} onClick={()=>ver(s.id)}>Ver {s.numeroQuipux}</button></td></tr>)}</tbody></table></div>}
   <nav aria-label="Páginas de solicitudes" className="catalogo-actions"><button className="app-button app-button--secondary" disabled={pagina===1||ocupado} onClick={()=>setPagina(p=>p-1)}>Anterior</button><span>Página {pagina}</span><button className="app-button app-button--secondary" disabled={items.length<50||ocupado} onClick={()=>setPagina(p=>p+1)}>Siguiente</button></nav>
  </>}
  {form&&<FormDialog title="Registrar solicitud" titleId="nueva-solicitud" busy={ocupado} onDismiss={()=>setForm(null)}><form onSubmit={guardar}>
   {error&&<p role="alert">{error}</p>}<fieldset disabled={ocupado} className="solicitud-fields"><legend>Datos del trámite recibido</legend>
   <label>Número de trámite Quipux<input required maxLength={100} value={form.numeroQuipux} onChange={e=>setForm({...form,numeroQuipux:e.target.value})}/></label>
   <label>Identificación del estudiante<input required maxLength={50} value={form.identificacionEstudiante} onChange={e=>setForm({...form,identificacionEstudiante:e.target.value})}/></label>
   <label>Nombre del estudiante<input required maxLength={200} value={form.nombreEstudiante} onChange={e=>setForm({...form,nombreEstudiante:e.target.value})}/></label>
   <label>Tipo de cambio<select value={form.tipoCambio} onChange={e=>setForm({...form,tipoCambio:e.target.value as Input['tipoCambio'],carreraOrigenId:null,institucionOrigen:null,carreraOrigenExterna:null})}><option value="Interno">Interno · FISEI-UTA</option><option value="Externo">Externo · Universidad del Ecuador</option></select></label>
   {form.tipoCambio==='Interno'?<label>Carrera de origen<select required value={form.carreraOrigenId??''} onChange={e=>setForm({...form,carreraOrigenId:e.target.value})}><option value="">Seleccione una carrera</option>{carreras.filter(c=>c.id!==form.carreraReceptoraId).map(c=><option key={c.id} value={c.id}>{c.nombre}</option>)}</select></label>:<><label>Universidad de origen (Ecuador)<input required maxLength={200} value={form.institucionOrigen??''} onChange={e=>setForm({...form,institucionOrigen:e.target.value})}/></label><label>Carrera de origen externa<input required maxLength={200} value={form.carreraOrigenExterna??''} onChange={e=>setForm({...form,carreraOrigenExterna:e.target.value})}/></label>
    <label>Períodos cursados (opcional)<input type="number" min={0} max={100} step={1} value={rn05.periodos} onChange={e=>setRn05({...rn05,periodos:e.target.value})}/></label>
    <label>Cambios previos de carrera (opcional)<input type="number" min={0} max={100} step={1} value={rn05.cambios} onChange={e=>setRn05({...rn05,cambios:e.target.value})}/></label></>}
   <label>Carrera receptora<select required value={form.carreraReceptoraId} onChange={e=>setForm({...form,carreraReceptoraId:e.target.value})}><option value="">Seleccione una carrera</option>{carreras.filter(c=>c.id!==form.carreraOrigenId).map(c=><option key={c.id} value={c.id}>{c.nombre}</option>)}</select></label>
   </fieldset><div className="catalogo-actions"><button className="app-button app-button--primary" disabled={ocupado}>{ocupado?'Registrando…':'Registrar solicitud'}</button><button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={()=>setForm(null)}>Cancelar</button></div>
  </form></FormDialog>}
  {detalle&&<FormDialog title={`Solicitud ${detalle.numeroQuipux}`} titleId="detalle-solicitud" busy={ocupado} onDismiss={()=>setDetalle(null)}><dl>
   <dt>Estudiante</dt><dd>{detalle.nombreEstudiante} · {detalle.identificacionEstudiante}</dd><dt>Tipo de cambio</dt><dd>{detalle.tipoCambio}</dd><dt>Origen</dt><dd>{detalle.tipoCambio==='Interno'?nombreCarrera(detalle.carreraOrigenId):`${detalle.institucionOrigen} · ${detalle.carreraOrigenExterna}`}</dd><dt>Carrera receptora</dt><dd>{nombreCarrera(detalle.carreraReceptoraId)}</dd><dt>Estado</dt><dd>{detalle.estado}</dd><dt>Fecha de registro</dt><dd>{new Date(detalle.fechaRegistro).toLocaleString('es-EC')}</dd>
  </dl>{detalle.tipoCambio==='Externo'&&<DatosCambioExterno key={`${detalle.id}/${detalle.estado}`} datos={{periodosCursados:detalle.periodosCursados??null,cambiosPreviosCarrera:detalle.cambiosPreviosCarrera??null}} editable={detalle.estado==='Registrada'} ocupado={ocupado} guardar={guardarDatosExternos}/>}<DocentesAsignados estado={detalle.estado} docentes={detalle.docentesAsignados??[]} ocupado={ocupado} cargarDisponibles={()=>leer<DocenteDisponible[]>(`/api/solicitudes/${detalle.id}/docentes-disponibles`)} asignar={asignarDocentes}/><AsignaturasAprobadas tipoCambio={detalle.tipoCambio} asignaturas={detalle.asignaturasAprobadas??[]} ocupado={ocupado} cargarOrigen={()=>leer<AsignaturaCatalogo[]>(`/api/solicitudes/${detalle.id}/asignaturas-origen`)} registrar={registrarAsignatura}/><button className="app-button app-button--secondary" disabled={ocupado} onClick={()=>setDetalle(null)}>Cerrar detalle</button></FormDialog>}
 </section>;
}
