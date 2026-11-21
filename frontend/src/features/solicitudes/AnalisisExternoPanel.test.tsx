import {render,screen,within} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {AnalisisInternoPanel} from './AnalisisInternoPanel';

// HU-19: el mismo panel opera sobre la ruta externa y añade RN-05 externo.
const pendiente={estado:'Pendiente',mensaje:'Sin ejecutar',resultado:null};
const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const ruta='/api/homologaciones/solicitudes/sol-e/analisis-externo';
const aprobadas=[{id:'a1',nombreAsignatura:'Externa sintética'}];
const incompleto={...pendiente,estado:'Incompleto',mensaje:'El criterio académico requiere el programa externo registrado.',asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',advertencias:['No es una aprobación oficial.']};
const requisitos={estado:'Incompleto',periodosCursados:null,cambiosPreviosCarrera:0,asignaturasAprobadas:1,asignaturasConAnalisisCompleto:0,asignaturasHomologables:0,
 incumplimientos:[],faltantes:['períodos cursados no declarados por el Coordinador'],advertencias:['No verifica gratuidad ni autoriza el cambio de carrera; no es una aprobación oficial.']};
function servidor(requisitosRespuesta:unknown=requisitos) {
 return vi.fn(async(path:string,init?:RequestInit)=>init?.method==='POST'?json(incompleto):path.endsWith('/opciones')?json([{id:'r1',codigo:'R1',nombre:'Receptora sintética',nivel:1}]):
  path.endsWith('/requisitos-externos')?json(requisitosRespuesta):json([]));
}

it('usa la ruta externa, envía dos identificadores y no muestra sugerencia',async()=>{
 const request=servidor();
 render(<AnalisisInternoPanel tipo="Externo" request={request} solicitudId="sol-e" aprobadas={aprobadas} inicial={pendiente}/>);
 const user=userEvent.setup();await user.click(screen.getByRole('button',{name:'Consultar análisis externo'}));
 await user.selectOptions(await screen.findByLabelText('Asignatura aprobada'),'a1');
 await user.selectOptions(screen.getByLabelText('Asignatura receptora'),'r1');
 await user.click(screen.getByRole('button',{name:'Ejecutar análisis y guardar'}));
 expect(await screen.findByText('El criterio académico requiere el programa externo registrado.')).toBeInTheDocument();
 const post=request.mock.calls.find(c=>c[1]?.method==='POST')!;
 expect(post[0]).toBe(ruta);
 expect(JSON.parse(post[1]!.body as string)).toEqual({asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1'});
 expect(screen.queryByText('Sugerencia')).not.toBeInTheDocument();
 expect(request.mock.calls.some(c=>String(c[0]).includes('analisis-interno'))).toBe(false);
 expect(screen.getByText(/no invoca al agente/i)).toBeInTheDocument();
});

it('muestra RN-05 externo con faltantes sin presentarlos como incumplimiento',async()=>{
 render(<AnalisisInternoPanel tipo="Externo" request={servidor()} solicitudId="sol-e" aprobadas={aprobadas} inicial={pendiente}/>);
 await userEvent.setup().click(screen.getByRole('button',{name:'Consultar análisis externo'}));
 const panel=await screen.findByRole('region',{name:'Requisitos del cambio externo'});
 expect(within(panel).getByText('Información incompleta')).toBeInTheDocument();
 expect(within(panel).getByText('períodos cursados no declarados por el Coordinador')).toBeInTheDocument();
 expect(within(panel).getByText('Sin dato')).toBeInTheDocument();
 expect(within(panel).queryByText('Incumplimientos')).not.toBeInTheDocument();
});

it('un dato declarado bajo el mínimo se muestra como no cumple',async()=>{
 render(<AnalisisInternoPanel tipo="Externo" request={servidor({...requisitos,estado:'NoCumple',cambiosPreviosCarrera:1,incumplimientos:['El cambio externo requiere no tener cambios previos de carrera.']})} solicitudId="sol-e" aprobadas={aprobadas} inicial={pendiente}/>);
 await userEvent.setup().click(screen.getByRole('button',{name:'Consultar análisis externo'}));
 const panel=await screen.findByRole('region',{name:'Requisitos del cambio externo'});
 expect(within(panel).getByText('No cumple')).toBeInTheDocument();
 expect(within(panel).getByText('El cambio externo requiere no tener cambios previos de carrera.')).toBeInTheDocument();
});
