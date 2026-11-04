import {useState} from 'react';
import type {FormEvent} from 'react';

export type DatosExternos={periodosCursados:number|null;cambiosPreviosCarrera:number|null};
const texto=(v:number|null)=>v===null?'':String(v);
const numero=(v:string)=>v.trim()===''?null:Number(v);
const dato=(v:number|null|undefined)=>v===null||v===undefined?'Sin dato':String(v);

/** HU-19 (D4): datos de RN-05 declarados por el Coordinador; se corrigen solo antes de asignar docentes. */
export function DatosCambioExterno({datos,editable,ocupado,guardar}:{datos:DatosExternos;editable:boolean;ocupado:boolean;guardar:(datos:DatosExternos)=>Promise<void>}) {
 const [periodos,setPeriodos]=useState(texto(datos.periodosCursados)),[cambios,setCambios]=useState(texto(datos.cambiosPreviosCarrera));
 const [error,setError]=useState(''),[aviso,setAviso]=useState('');
 async function enviar(event:FormEvent) {
  event.preventDefault();if(ocupado)return;setError('');setAviso('');
  try{await guardar({periodosCursados:numero(periodos),cambiosPreviosCarrera:numero(cambios)});setAviso('Datos del cambio externo guardados.');}
  catch(e){setError(e instanceof Error?e.message:'No se guardaron los datos del cambio externo.');}
 }
 return <section aria-labelledby="datos-externos-titulo">
  <h3 id="datos-externos-titulo">Datos del cambio externo (RN-05)</h3>
  <dl><dt>Períodos cursados</dt><dd>{dato(datos.periodosCursados)}</dd><dt>Cambios previos de carrera</dt><dd>{dato(datos.cambiosPreviosCarrera)}</dd></dl>
  {editable?<form onSubmit={enviar}>
   <p>Declárelos desde el expediente. Un dato vacío queda sin declarar y el análisis lo informa como información incompleta.</p>
   {error&&<p role="alert">{error}</p>}{aviso&&<p role="status">{aviso}</p>}
   <fieldset disabled={ocupado} className="solicitud-fields"><legend>Corregir antes de asignar docentes</legend>
    <label>Períodos cursados<input type="number" min={0} max={100} step={1} value={periodos} onChange={e=>setPeriodos(e.target.value)}/></label>
    <label>Cambios previos de carrera<input type="number" min={0} max={100} step={1} value={cambios} onChange={e=>setCambios(e.target.value)}/></label>
   </fieldset>
   <button className="app-button app-button--secondary" disabled={ocupado}>Guardar datos del cambio externo</button>
  </form>:<p>Con docentes asignados estos datos ya no se modifican.</p>}
 </section>;
}
