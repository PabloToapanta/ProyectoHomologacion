import {render,screen} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {AnalisisInternoPanel} from './AnalisisInternoPanel';

const pendiente={estado:'Pendiente',mensaje:'Sin ejecutar',resultado:null};
const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const ruta='/api/homologaciones/solicitudes/sol-a/analisis-interno';
const aprobadas=[{id:'a1',nombreAsignatura:'Origen sintético'}];
it('recupera el último análisis guardado al abrir el detalle sin volver a ejecutarlo',async()=>{
 const request=vi.fn(async(path:string)=>json(path.endsWith('/opciones')?[{id:'r1',codigo:'R1',nombre:'Receptora sintética',nivel:1}]:[{fecha:'2026-11-03T12:00:00Z',analisis:{...pendiente,estado:'Fallido',mensaje:'Modelo no disponible',asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',advertencias:['Revisión humana necesaria']}}]));
 render(<AnalisisInternoPanel request={request} solicitudId="sol-a" aprobadas={aprobadas} inicial={pendiente}/>);
 expect(await screen.findByText('Modelo no disponible')).toBeInTheDocument();
 expect(screen.queryByText('Sugerencia')).not.toBeInTheDocument();
 expect(request.mock.calls.map(c=>c[0])).toContain(ruta);
});
it('envía únicamente dos identificadores y muestra incompletitud sin recomendación',async()=>{
 const request=vi.fn(async(path:string,init?:RequestInit)=>json(init?.method==='POST'?{...pendiente,estado:'Incompleto',mensaje:'Falta revisión de programas',advertencias:['No es aprobación oficial']}:
  path.endsWith('/opciones')?[{id:'r1',codigo:'R1',nombre:'Receptora sintética',nivel:1}]:[]));
 render(<AnalisisInternoPanel request={request} solicitudId="sol-a" aprobadas={aprobadas} inicial={pendiente}/>);
 const user=userEvent.setup();await user.click(screen.getByRole('button',{name:'Consultar análisis interno'}));
 await user.selectOptions(await screen.findByLabelText('Asignatura aprobada'),'a1');
 await user.selectOptions(screen.getByLabelText('Asignatura receptora'),'r1');
 await user.click(screen.getByRole('button',{name:'Ejecutar análisis y guardar'}));
 expect(await screen.findByText('Falta revisión de programas')).toBeInTheDocument();
 const call=request.mock.calls.find(c=>c[1]?.method==='POST')!;
 expect(JSON.parse(call[1]!.body as string)).toEqual({asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1'});
 expect(screen.queryByText('Sugerencia')).not.toBeInTheDocument();
});
it('error de persistencia no muestra confirmación de guardado',async()=>{
 const request=vi.fn(async(path:string,init?:RequestInit)=>init?.method==='POST'?json({title:'No se confirma ningún guardado'},503):json(path.endsWith('/opciones')?[{id:'r1',codigo:'R1',nombre:'Receptora',nivel:1}]:[]));
 render(<AnalisisInternoPanel request={request} solicitudId="sol-a" aprobadas={aprobadas} inicial={pendiente}/>);
 const user=userEvent.setup();await user.click(screen.getByRole('button',{name:'Consultar análisis interno'}));
 await user.selectOptions(await screen.findByLabelText('Asignatura aprobada'),'a1');await user.selectOptions(screen.getByLabelText('Asignatura receptora'),'r1');
 await user.click(screen.getByRole('button',{name:'Ejecutar análisis y guardar'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('No se confirma ningún guardado');
 expect(screen.queryByText('Análisis guardado.')).not.toBeInTheDocument();
});
// HU-37: reutilización de la explicación del agente y opción de forzar un análisis nuevo.
const receptoraOpciones=[{id:'r1',codigo:'R1',nombre:'Receptora sintética',nivel:1}];
const completo=(agente:unknown)=>({estado:'Completo',mensaje:'Evaluación completada',resultado:{porcentajeAcademico:90,porcentajeHoras:80,porcentajeCreditos:100,sugerencia:'Homologable',justificacion:'Justificación sintética',evidencias:[]},
 asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',advertencias:[],agente});
async function abrirYSeleccionar(){
 const user=userEvent.setup();await user.click(screen.getByRole('button',{name:'Consultar análisis interno'}));
 await user.selectOptions(await screen.findByLabelText('Asignatura aprobada'),'a1');await user.selectOptions(screen.getByLabelText('Asignatura receptora'),'r1');
 return user;
}
it('HU-37 indica que la explicación del agente se reutilizó, con su fecha e id originales',async()=>{
 const agente={explicacionId:'exp-1',reutilizada:true,explicacionOriginalId:'exp-1',fechaOriginal:'2026-11-20T15:00:00Z',modelo:'modelo-sintetico',versionPlantilla:'hu18-v1'};
 const request=vi.fn(async(path:string,init?:RequestInit)=>json(init?.method==='POST'?completo(agente):path.endsWith('/opciones')?receptoraOpciones:[]));
 render(<AnalisisInternoPanel request={request} solicitudId="sol-a" aprobadas={aprobadas} inicial={pendiente}/>);
 const user=await abrirYSeleccionar();
 await user.click(screen.getByRole('button',{name:'Ejecutar análisis y guardar'}));
 const nota=await screen.findByText(/^Reutilizado ·/);
 expect(nota).toHaveTextContent(new Date('2026-11-20T15:00:00Z').toLocaleString('es-EC'));
 expect(nota).toHaveTextContent('análisis exp-1');
});
it('HU-37 una explicación nueva no se marca como reutilizada',async()=>{
 const agente={explicacionId:'exp-2',reutilizada:false,explicacionOriginalId:null,fechaOriginal:null,modelo:'modelo-sintetico',versionPlantilla:'hu18-v1'};
 const request=vi.fn(async(path:string,init?:RequestInit)=>json(init?.method==='POST'?completo(agente):path.endsWith('/opciones')?receptoraOpciones:[]));
 render(<AnalisisInternoPanel request={request} solicitudId="sol-a" aprobadas={aprobadas} inicial={pendiente}/>);
 const user=await abrirYSeleccionar();
 await user.click(screen.getByRole('button',{name:'Ejecutar análisis y guardar'}));
 expect(await screen.findByText('Análisis guardado.')).toBeInTheDocument();
 expect(screen.queryByText(/^Reutilizado ·/)).not.toBeInTheDocument();
});
it('HU-37 forzar un análisis nuevo exige el motivo y lo envía',async()=>{
 const request=vi.fn(async(path:string,init?:RequestInit)=>json(init?.method==='POST'?completo(null):path.endsWith('/opciones')?receptoraOpciones:[]));
 render(<AnalisisInternoPanel request={request} solicitudId="sol-a" aprobadas={aprobadas} inicial={pendiente}/>);
 const user=await abrirYSeleccionar();
 await user.click(screen.getByLabelText('Forzar un análisis nuevo'));
 const ejecutar=screen.getByRole('button',{name:'Ejecutar análisis y guardar'});
 expect(ejecutar).toBeDisabled();
 await user.type(screen.getByLabelText('Motivo'),'  Se revisó el programa receptor ');
 expect(ejecutar).toBeEnabled();
 await user.click(ejecutar);
 await screen.findByText('Análisis guardado.');
 const call=request.mock.calls.find(c=>c[1]?.method==='POST')!;
 expect(JSON.parse(call[1]!.body as string)).toEqual({asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',forzarNuevo:true,motivo:'Se revisó el programa receptor'});
 expect(screen.getByLabelText('Forzar un análisis nuevo')).not.toBeChecked();
});
it('HU-37 el análisis externo no ofrece forzar porque no usa el agente',async()=>{
 const request=vi.fn(async(path:string)=>json(path.endsWith('/opciones')?receptoraOpciones:path.endsWith('/requisitos-externos')?{estado:'Incompleto',periodosCursados:null,cambiosPreviosCarrera:null,asignaturasAprobadas:0,asignaturasConAnalisisCompleto:0,asignaturasHomologables:0,incumplimientos:[],faltantes:[],advertencias:[]}:[]));
 render(<AnalisisInternoPanel request={request} solicitudId="sol-a" aprobadas={aprobadas} inicial={pendiente} tipo="Externo"/>);
 const user=userEvent.setup();await user.click(screen.getByRole('button',{name:'Consultar análisis externo'}));
 await screen.findByLabelText('Asignatura aprobada');
 expect(screen.queryByLabelText('Forzar un análisis nuevo')).not.toBeInTheDocument();
});
