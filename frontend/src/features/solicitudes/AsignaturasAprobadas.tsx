import {useState} from 'react';
import type {FormEvent} from 'react';
export type AsignaturaCatalogo={id:string;codigo:string;nombre:string;nivel:number;creditos:number;horas:number};
export type AsignaturaAprobadaInput=Record<string,string|number>;
export type AsignaturaAprobada={id:string;asignaturaOrigenId:string|null;codigoAsignatura:string|null;nombreAsignatura:string;creditos:number;sistemaCreditos:string|null;horas:number;unidadHoras:string|null;referenciaPrograma:string|null;nota:number;notaMaximaEscala:number;notaMinimaAprobacion:number;estado:string;fechaAprobacion:string;numeroMatricula:string;intentoMatricula:number};
type Campos={asignaturaOrigenId:string;nombreAsignatura:string;creditos:string;sistemaCreditos:string;horas:string;unidadHoras:string;referenciaPrograma:string;notaMaximaEscala:string;notaMinimaAprobacion:string;nota:string;fechaAprobacion:string;numeroMatricula:string;intentoMatricula:string};
const vacios:Campos={asignaturaOrigenId:'',nombreAsignatura:'',creditos:'',sistemaCreditos:'',horas:'',unidadHoras:'',referenciaPrograma:'',notaMaximaEscala:'',notaMinimaAprobacion:'',nota:'',fechaAprobacion:'',numeroMatricula:'',intentoMatricula:''};
const intentos=['Primera','Segunda','Tercera'];
// La fecha académica no tiene hora: se muestra tal cual para no desplazarla por zona horaria.
const fecha=(valor:string)=>valor.split('-').reverse().join('/');
const mensaje=(e:unknown)=>e instanceof Error?e.message:'No se pudo completar la operación.';
export function AsignaturasAprobadas({tipoCambio,asignaturas,ocupado,cargarOrigen,registrar}:{tipoCambio:'Interno'|'Externo';asignaturas:AsignaturaAprobada[];ocupado:boolean;cargarOrigen:()=>Promise<AsignaturaCatalogo[]>;registrar:(input:AsignaturaAprobadaInput)=>Promise<void>}) {
 const [campos,setCampos]=useState<Campos|null>(null),[origen,setOrigen]=useState<AsignaturaCatalogo[]>([]),[error,setError]=useState(''),[aviso,setAviso]=useState('');
 const interno=tipoCambio==='Interno';
 const campo=(nombre:keyof Campos)=>({value:campos?.[nombre]??'',onChange:(e:{target:{value:string}})=>setCampos(actual=>actual&&{...actual,[nombre]:e.target.value})});
 async function agregar() {
  setError('');setAviso('');
  try {if(interno)setOrigen(await cargarOrigen());setCampos({...vacios});}catch(e){setError(mensaje(e));}
 }
 async function guardar(event:FormEvent) {
  event.preventDefault();if(!campos||ocupado)return;setError('');
  const comunes={nota:Number(campos.nota),fechaAprobacion:campos.fechaAprobacion,numeroMatricula:campos.numeroMatricula.trim(),intentoMatricula:Number(campos.intentoMatricula)};
  const input=interno?{asignaturaOrigenId:campos.asignaturaOrigenId,...comunes}:{nombreAsignatura:campos.nombreAsignatura.trim(),creditos:Number(campos.creditos),sistemaCreditos:campos.sistemaCreditos.trim(),horas:Number(campos.horas),unidadHoras:campos.unidadHoras.trim(),referenciaPrograma:campos.referenciaPrograma.trim(),notaMaximaEscala:Number(campos.notaMaximaEscala),notaMinimaAprobacion:Number(campos.notaMinimaAprobacion),...comunes};
  try {await registrar(input);setCampos(null);setAviso('Asignatura registrada.');}catch(e){setError(mensaje(e));}
 }
 const elegida=origen.find(a=>a.id===campos?.asignaturaOrigenId);
 return <section aria-labelledby="asignaturas-aprobadas">
  <h3 id="asignaturas-aprobadas">Asignaturas aprobadas</h3>
  <p>Registrar una asignatura no implica elegibilidad: la aprobación y la antigüedad se validan por separado.</p>
  {aviso&&<p role="status">{aviso}</p>}
  {asignaturas.length===0?<p>Sin asignaturas aprobadas registradas.</p>:<div className="solicitudes-table"><table><thead><tr><th>Asignatura</th><th>Créditos</th><th>Horas</th><th>Nota</th><th>Estado</th><th>Fecha</th><th>N.º matrícula</th><th>Intento</th></tr></thead><tbody>{asignaturas.map(a=><tr key={a.id}><td>{a.codigoAsignatura&&`${a.codigoAsignatura} · `}{a.nombreAsignatura}</td><td>{a.creditos}{a.sistemaCreditos&&` (${a.sistemaCreditos})`}</td><td>{a.horas}{a.unidadHoras&&` (${a.unidadHoras})`}</td><td>{a.nota} / {a.notaMaximaEscala}</td><td>{a.estado}</td><td>{fecha(a.fechaAprobacion)}</td><td>{a.numeroMatricula}</td><td>{intentos[a.intentoMatricula-1]}</td></tr>)}</tbody></table></div>}
  {!campos&&error&&<p role="alert">{error}</p>}
  {campos?<form onSubmit={guardar}>
   {error&&<p role="alert">{error}</p>}<fieldset disabled={ocupado} className="solicitud-fields"><legend>Datos de la asignatura</legend>
   {interno?<><label>Asignatura de origen<select required {...campo('asignaturaOrigenId')}><option value="">Seleccione una asignatura</option>{origen.map(a=><option key={a.id} value={a.id}>{a.codigo} · {a.nombre}</option>)}</select></label>{elegida?<p>{elegida.creditos} créditos · {elegida.horas} horas</p>:!origen.length&&<p>La carrera de origen no tiene asignaturas en el catálogo.</p>}</>:<>
    <label>Nombre de la asignatura<input required maxLength={200} {...campo('nombreAsignatura')}/></label>
    <label>Referencia al programa analítico<input required maxLength={200} {...campo('referenciaPrograma')}/></label>
    <label>Créditos<input required type="number" min="0.01" step="0.01" {...campo('creditos')}/></label>
    <label>Sistema de créditos<input required maxLength={100} {...campo('sistemaCreditos')}/></label>
    <label>Horas<input required type="number" min="1" step="1" {...campo('horas')}/></label>
    <label>Unidad de horas<input required maxLength={100} {...campo('unidadHoras')}/></label>
    <label>Nota máxima de la escala<input required type="number" min="0.01" max="999.99" step="0.01" {...campo('notaMaximaEscala')}/></label>
    <label>Nota mínima de aprobación<input required type="number" min="0.01" step="0.01" {...campo('notaMinimaAprobacion')}/></label>
   </>}
   <label>Nota<input required type="number" min="0" max={interno?'10':campos.notaMaximaEscala||undefined} step="0.01" {...campo('nota')}/></label>
   <label>Fecha de aprobación<input required type="date" {...campo('fechaAprobacion')}/></label>
   <label>Número de matrícula<input required maxLength={50} {...campo('numeroMatricula')}/></label>
   <label>Intento de matrícula<select required {...campo('intentoMatricula')}><option value="">Seleccione el intento</option>{intentos.map((nombre,i)=><option key={nombre} value={i+1}>{nombre}</option>)}</select></label>
   </fieldset><div className="catalogo-actions"><button className="app-button app-button--primary" disabled={ocupado}>{ocupado?'Registrando…':'Registrar asignatura'}</button><button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={()=>{setCampos(null);setError('');}}>Cancelar registro</button></div>
  </form>:<button className="app-button app-button--primary" disabled={ocupado} onClick={agregar}>Agregar asignatura aprobada</button>}
 </section>;
}
