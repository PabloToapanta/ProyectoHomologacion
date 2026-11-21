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
