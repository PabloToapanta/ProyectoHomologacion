import {useState} from 'react';
import type {FormEvent} from 'react';
export type DocenteDisponible={id:string;nombreUsuario:string};
export type DocenteAsignado={id:string;docenteId:string;nombreUsuario:string;orden:number};
const mensaje=(e:unknown)=>e instanceof Error?e.message:'No se pudo completar la operación.';
export function DocentesAsignados({estado,docentes,ocupado,cargarDisponibles,asignar}:{estado:string;docentes:DocenteAsignado[];ocupado:boolean;cargarDisponibles:()=>Promise<DocenteDisponible[]>;asignar:(ids:string[])=>Promise<void>}) {
 const [disponibles,setDisponibles]=useState<DocenteDisponible[]|null>(null),[primero,setPrimero]=useState(''),[segundo,setSegundo]=useState('');
 const [error,setError]=useState(''),[aviso,setAviso]=useState('');
 async function abrir() {
  setError('');setAviso('');
  try {setDisponibles(await cargarDisponibles());setPrimero('');setSegundo('');}catch(e){setError(mensaje(e));}
 }
 async function guardar(event:FormEvent) {
  event.preventDefault();if(ocupado)return;setError('');
  try {await asignar([primero,segundo]);setDisponibles(null);setAviso('Docentes asignados correctamente.');}catch(e){setError(mensaje(e));}
 }
 const opciones=(excluir:string)=>disponibles?.filter(d=>d.id!==excluir).map(d=><option key={d.id} value={d.id}>{d.nombreUsuario}</option>);
 return <section aria-labelledby="docentes-asignados">
  <h3 id="docentes-asignados">Docentes asignados</h3>
  {aviso&&<p role="status">{aviso}</p>}
  {docentes.length===0?<p>Sin docentes asignados.</p>:<ul aria-label="Docentes asignados">{docentes.map(d=><li key={d.id}>{d.nombreUsuario}</li>)}</ul>}
  {!disponibles&&error&&<p role="alert">{error}</p>}
  {estado==='Registrada'&&(disponibles?<form onSubmit={guardar}>
   {error&&<p role="alert">{error}</p>}
   {disponibles.length<2&&<p>Se necesitan al menos dos docentes activos para asignar la solicitud.</p>}
   <fieldset disabled={ocupado} className="solicitud-fields"><legend>Dos docentes distintos</legend>
    <label>Primer docente<select required value={primero} onChange={e=>setPrimero(e.target.value)}><option value="">Seleccione un docente</option>{opciones(segundo)}</select></label>
    <label>Segundo docente<select required value={segundo} onChange={e=>setSegundo(e.target.value)}><option value="">Seleccione un docente</option>{opciones(primero)}</select></label>
   </fieldset><div className="catalogo-actions"><button className="app-button app-button--primary" disabled={ocupado||disponibles.length<2}>{ocupado?'Guardando…':'Guardar asignación'}</button><button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={()=>{setDisponibles(null);setError('');}}>Cancelar asignación</button></div>
  </form>:<button className="app-button app-button--primary" disabled={ocupado} onClick={abrir}>Asignar docentes</button>)}
 </section>;
}
