import {useEffect,useState} from 'react';
import type {FormEvent} from 'react';
import type {Carrera,CatalogoRequest} from './carreras-api';

type Tematica={nombre:string;contenidos:string[]};
type UnidadBorrador={numero:number;nombre:string;horas:number|null;resultadoAprendizaje:string;tematicas:Tematica[]};
type Borrador={codigo:string|null;nombre:string|null;nivel:number|null;creditos:number|null;horas:number|null;unidades:UnidadBorrador[];faltantes:string[];advertencias:string[]};
type ResultadoArchivo={archivo:string;estado:'Borrador'|'SinTexto'|'Invalido';mensaje:string;borrador:Borrador|null};
type Lote={carreraId:string;archivos:ResultadoArchivo[]};
/** Edición en texto: un campo vacío sigue vacío hasta que el Administrador lo complete. */
type TematicaEditable={nombre:string;contenidos:string};
type UnidadEditable={numero:string;nombre:string;horas:string;resultadoAprendizaje:string;tematicas:TematicaEditable[]};
type Edicion={codigo:string;nombre:string;nivel:string;creditos:string;horas:string;unidades:UnidadEditable[]};

const texto=(v:string|number|null)=>v===null?'':String(v);
const numero=(v:string)=>v.trim()===''?null:Number(v);
async function problema(response:Response,defecto:string) {
 if(response.status===401)return 'Tu sesión ha finalizado. Inicia sesión de nuevo.';
 if(response.status===403)return 'Solo el Administrador puede cargar programas analíticos.';
 try{const p=await response.json() as {detail?:string;title?:string;errors?:Record<string,string[]>};return p.detail??(p.errors?Object.values(p.errors).flat().join(' '):undefined)??p.title??defecto;}catch{return defecto;}
}
function editable(b:Borrador):Edicion {
 return {codigo:texto(b.codigo),nombre:texto(b.nombre),nivel:texto(b.nivel),creditos:texto(b.creditos),horas:texto(b.horas),
  unidades:b.unidades.map(u=>({numero:String(u.numero),nombre:u.nombre,horas:texto(u.horas),resultadoAprendizaje:u.resultadoAprendizaje,
   tematicas:u.tematicas.map(t=>({nombre:t.nombre,contenidos:t.contenidos.join('\n')}))}))};
}

/** HU-08: carga masiva de programas analíticos FISEI. Nada se guarda sin la revisión y confirmación del Administrador. */
export function ImportacionProgramasPage({request}:{request:CatalogoRequest}) {
 const [carreras,setCarreras]=useState<Carrera[]>([]),[carrera,setCarrera]=useState(''),[archivos,setArchivos]=useState<File[]>([]);
 const [lote,setLote]=useState<Lote|null>(null),[archivosLote,setArchivosLote]=useState<File[]>([]),[confirmados,setConfirmados]=useState<number[]>([]);
 const [revisando,setRevisando]=useState<number|null>(null),[edicion,setEdicion]=useState<Edicion|null>(null);
 const [ocupado,setOcupado]=useState(false),[error,setError]=useState(''),[aviso,setAviso]=useState('');
 useEffect(()=>{
  const controller=new AbortController();
  request('/api/carreras',{signal:controller.signal}).then(async r=>{if(!r.ok)throw new Error(await problema(r,'No se pudieron cargar las carreras.'));return r.json() as Promise<Carrera[]>;})
   .then(c=>{if(!controller.signal.aborted)setCarreras(c.filter(x=>x.vigencia));}).catch(e=>{if(!controller.signal.aborted)setError(e instanceof Error?e.message:'No se pudieron cargar las carreras.');});
  return ()=>controller.abort();
 },[request]);

 async function analizar(event:FormEvent) {
  event.preventDefault();if(ocupado||!carrera||archivos.length===0)return;
  setOcupado(true);setError('');setAviso('');setLote(null);setRevisando(null);setEdicion(null);setConfirmados([]);
  const form=new FormData();form.append('carreraId',carrera);archivos.forEach(a=>form.append('archivos',a,a.name));
  try{
   const r=await request('/api/importaciones-programas/analisis',{method:'POST',body:form});
   if(!r.ok)throw new Error(await problema(r,'No se pudieron analizar los programas.'));
   setLote(await r.json() as Lote);setArchivosLote(archivos);
  }catch(e){setError(e instanceof Error?e.message:'No se pudieron analizar los programas.');}finally{setOcupado(false);}
 }
 function revisar(i:number){const b=lote?.archivos[i].borrador;if(!b)return;setRevisando(i);setEdicion(editable(b));setError('');setAviso('');}
 async function confirmar(event:FormEvent) {
  event.preventDefault();if(ocupado||revisando===null||!edicion||!lote)return;
  const resultado=lote.archivos[revisando];const archivo=archivosLote[revisando];
  setOcupado(true);setError('');setAviso('');
  const programa={carreraId:lote.carreraId,codigo:edicion.codigo.trim(),nombre:edicion.nombre.trim(),nivel:numero(edicion.nivel),creditos:numero(edicion.creditos),horas:numero(edicion.horas),
   unidades:edicion.unidades.map(u=>({numero:numero(u.numero),nombre:u.nombre.trim(),horas:numero(u.horas),resultadoAprendizaje:u.resultadoAprendizaje.trim(),
    tematicas:u.tematicas.map(t=>({nombre:t.nombre.trim(),contenidos:t.contenidos.split('\n').map(c=>c.trim()).filter(c=>c.length>0)}))}))};
  const form=new FormData();form.append('programa',JSON.stringify(programa));form.append('archivo',archivo,archivo.name);
  try{
   const r=await request('/api/importaciones-programas/confirmacion',{method:'POST',body:form});
   if(!r.ok)throw new Error(await problema(r,'No se pudo guardar el programa.'));
   setConfirmados(c=>[...c,revisando]);setRevisando(null);setEdicion(null);setAviso(`${resultado.archivo} guardado en el catálogo.`);
  }catch(e){setError(e instanceof Error?e.message:'No se pudo guardar el programa.');}finally{setOcupado(false);}
 }
 const actualizarUnidad=(i:number,cambio:Partial<UnidadEditable>)=>setEdicion(e=>e&&{...e,unidades:e.unidades.map((u,j)=>j===i?{...u,...cambio}:u)});
 const actualizarTematica=(i:number,k:number,cambio:Partial<TematicaEditable>)=>actualizarUnidad(i,{tematicas:edicion!.unidades[i].tematicas.map((t,j)=>j===k?{...t,...cambio}:t)});
 const tematicaVacia=():TematicaEditable=>({nombre:'',contenidos:''});
 function agregarUnidad(){setEdicion(e=>e&&{...e,unidades:[...e.unidades,{numero:String(Math.max(0,...e.unidades.map(u=>Number(u.numero)||0))+1),nombre:'',horas:'',resultadoAprendizaje:'',tematicas:[tematicaVacia()]}]});}
 const borrador=revisando===null?null:lote?.archivos[revisando];

 return <section className="catalogo-section" aria-labelledby="importacion-title">
  <p className="eyebrow">CATÁLOGO / PROGRAMAS ANALÍTICOS</p><h1 id="importacion-title">Carga de programas analíticos</h1>
  <p>Carga los PDF del plan analítico FISEI. El sistema extrae un borrador de cada archivo sin guardarlo; revísalo, corrígelo y confírmalo. El PDF se conserva junto a la asignatura confirmada. No se aplica OCR ni se completan datos por suposición.</p>
  {error&&<p role="alert">{error}</p>}{aviso&&<p role="status">{aviso}</p>}
  <form onSubmit={analizar}><fieldset disabled={ocupado} className="solicitud-fields"><legend>Archivos del lote</legend>
   <label>Carrera<select required value={carrera} onChange={e=>setCarrera(e.target.value)}><option value="">Seleccione una carrera</option>{carreras.map(c=><option key={c.id} value={c.id}>{c.nombre}</option>)}</select></label>
   <label>Programas analíticos (PDF)<input type="file" multiple accept="application/pdf,.pdf" onChange={e=>setArchivos(Array.from(e.target.files??[]))}/></label>
   <p>Hasta 20 archivos de 5 MB cada uno y 25 MB por lote.</p>
  </fieldset><button className="app-button app-button--primary" disabled={ocupado||!carrera||archivos.length===0}>{ocupado&&!edicion?'Analizando…':'Analizar programas'}</button></form>
  {lote&&<ul aria-label="Resultado por archivo">{lote.archivos.map((a,i)=><li key={`${i}-${a.archivo}`}>
   <span>{a.archivo} · {confirmados.includes(i)?'Confirmado':a.estado} · {a.mensaje}</span>
   {a.borrador&&!confirmados.includes(i)&&<button className="app-button app-button--secondary" disabled={ocupado} onClick={()=>revisar(i)}>Revisar {a.archivo}</button>}
  </li>)}</ul>}
  {borrador&&edicion&&<section aria-label={`Revisión de ${borrador.archivo}`}><h2>Revisión de {borrador.archivo}</h2>
   {borrador.borrador!.faltantes.length>0&&<><h3>Datos no encontrados</h3><ul aria-label="Datos no encontrados">{borrador.borrador!.faltantes.map(f=><li key={f}>{f}</li>)}</ul></>}
   {borrador.borrador!.advertencias.length>0&&<ul aria-label="Advertencias">{borrador.borrador!.advertencias.map(a=><li key={a}>{a}</li>)}</ul>}
   <form onSubmit={confirmar}><fieldset disabled={ocupado} className="solicitud-fields"><legend>Asignatura</legend>
    <label>Código<input required maxLength={50} value={edicion.codigo} onChange={e=>setEdicion({...edicion,codigo:e.target.value})}/></label>
    <label>Nombre de la asignatura<input required maxLength={200} value={edicion.nombre} onChange={e=>setEdicion({...edicion,nombre:e.target.value})}/></label>
    <label>Nivel<input type="number" required min={1} step={1} value={edicion.nivel} onChange={e=>setEdicion({...edicion,nivel:e.target.value})}/></label>
    <label>Créditos<input type="number" required min={0.01} step={0.01} value={edicion.creditos} onChange={e=>setEdicion({...edicion,creditos:e.target.value})}/></label>
    <label>Horas totales<input type="number" required min={1} step={1} value={edicion.horas} onChange={e=>setEdicion({...edicion,horas:e.target.value})}/></label>
   </fieldset>
   {edicion.unidades.map((u,i)=><fieldset key={i} disabled={ocupado} className="solicitud-fields"><legend>Unidad {u.numero}</legend>
    <label>{`Número de la unidad ${i+1}`}<input type="number" required min={1} step={1} value={u.numero} onChange={e=>actualizarUnidad(i,{numero:e.target.value})}/></label>
    <label>{`Nombre de la unidad ${u.numero}`}<input required maxLength={200} value={u.nombre} onChange={e=>actualizarUnidad(i,{nombre:e.target.value})}/></label>
    <label>{`Horas de la unidad ${u.numero}`}<input type="number" required min={1} step={1} value={u.horas} onChange={e=>actualizarUnidad(i,{horas:e.target.value})}/></label>
    <label>{`Resultado de aprendizaje de la unidad ${u.numero}`}<textarea required maxLength={4000} value={u.resultadoAprendizaje} onChange={e=>actualizarUnidad(i,{resultadoAprendizaje:e.target.value})}/></label>
    {u.tematicas.map((t,k)=><div key={k}>
     <label>{`Temática ${u.numero}.${k+1}`}<input required maxLength={200} value={t.nombre} onChange={e=>actualizarTematica(i,k,{nombre:e.target.value})}/></label>
     <label>{`Contenidos de ${u.numero}.${k+1} (uno por línea)`}<textarea required value={t.contenidos} onChange={e=>actualizarTematica(i,k,{contenidos:e.target.value})}/></label>
     <button type="button" className="app-button app-button--secondary" onClick={()=>actualizarUnidad(i,{tematicas:u.tematicas.filter((_,j)=>j!==k)})}>{`Eliminar temática ${u.numero}.${k+1}`}</button>
    </div>)}
    <button type="button" className="app-button app-button--secondary" disabled={u.tematicas.length>=100} onClick={()=>actualizarUnidad(i,{tematicas:[...u.tematicas,tematicaVacia()]})}>{`Añadir temática a unidad ${u.numero}`}</button>
    <button type="button" className="app-button app-button--secondary" onClick={()=>setEdicion(e=>e&&{...e,unidades:e.unidades.filter((_,j)=>j!==i)})}>{`Eliminar unidad ${u.numero}`}</button>
   </fieldset>)}
   <button type="button" className="app-button app-button--secondary" disabled={ocupado||edicion.unidades.length>=50} onClick={agregarUnidad}>Añadir unidad</button>
   <div className="catalogo-actions"><button className="app-button app-button--primary" disabled={ocupado||edicion.unidades.length===0||edicion.unidades.some(u=>u.tematicas.length===0)}>{ocupado?'Guardando…':'Confirmar en el catálogo'}</button>
    <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={()=>{setRevisando(null);setEdicion(null);}}>Cancelar revisión</button></div>
   </form>
  </section>}
 </section>;
}
