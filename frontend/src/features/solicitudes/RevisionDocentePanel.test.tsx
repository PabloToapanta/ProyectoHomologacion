import {act,render,screen,within} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {RevisionDocentePanel} from './RevisionDocentePanel';

const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const ruta='/api/homologaciones/solicitudes/sol-a/revisiones-docentes';
const otro={solicitudId:'sol-a',asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',docenteId:'d2',docente:'docente.luis',postura:'Ajuste',decision:'Homologable',
 observacion:'Los contenidos coinciden.',estadoAgente:'Incompleto',sugerenciaAgente:null,fecha:'2026-11-04T17:00:00Z'};
const deOtraPareja={...otro,asignaturaReceptoraId:'r9',docente:'docente.rosa'};
function servidor(put:(body:unknown)=>Response=b=>json({...otro,...(b as object),docenteId:'d1',docente:'docente.ana'})) {
 return vi.fn(async(path:string,init?:RequestInit)=>path!==ruta?json({},500):init?.method==='PUT'?put(JSON.parse(String(init.body))):json([otro,deOtraPareja]));
}

it('sin sugerencia del agente solo permite ajuste con decisión y observación, y muestra el análisis del otro docente',async()=>{
 const user=userEvent.setup();const request=servidor();
 render(<RevisionDocentePanel request={request} solicitudId="sol-a" aprobadaId="a1" receptoraId="r1" sugerenciaAgente={null}/>);
 const lista=await screen.findByRole('list',{name:'Análisis de los docentes'});
 expect(within(lista).getByText(/docente\.luis/)).toHaveTextContent('Ajuste · Homologable');
 expect(within(lista).queryByText(/docente\.rosa/)).not.toBeInTheDocument();
 expect(screen.getByLabelText('De acuerdo con el agente')).toBeDisabled();
 await user.click(screen.getByLabelText('Ajuste con mi decisión'));
 await user.selectOptions(screen.getByLabelText('Mi decisión'),'NoHomologable');
 await user.type(screen.getByLabelText('Observación'),'Faltan contenidos de la receptora.');
 await user.click(screen.getByRole('button',{name:'Guardar mi análisis'}));
 expect(await screen.findByText('Su análisis quedó registrado.')).toBeInTheDocument();
 const put=request.mock.calls.find(c=>c[1]?.method==='PUT')!;
 expect(JSON.parse(String(put[1]!.body))).toEqual({asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',postura:'Ajuste',decision:'NoHomologable',observacion:'Faltan contenidos de la receptora.'});
});

it('con sugerencia del agente registra De acuerdo sin enviar decisión',async()=>{
 const user=userEvent.setup();const request=servidor();
 render(<RevisionDocentePanel request={request} solicitudId="sol-a" aprobadaId="a1" receptoraId="r1" sugerenciaAgente="Homologable"/>);
 await user.click(await screen.findByLabelText('De acuerdo con el agente'));
 expect(screen.queryByLabelText('Mi decisión')).not.toBeInTheDocument();
 expect(screen.getByText(/Sugerencia del agente: Homologable/)).toBeInTheDocument();
 await user.click(screen.getByRole('button',{name:'Guardar mi análisis'}));
 await screen.findByText('Su análisis quedó registrado.');
 const put=request.mock.calls.find(c=>c[1]?.method==='PUT')!;
 expect(JSON.parse(String(put[1]!.body))).toEqual({asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',postura:'DeAcuerdo',decision:null,observacion:null});
});

it('un rechazo del servidor se muestra sin anunciar el registro',async()=>{
 const user=userEvent.setup();
 render(<RevisionDocentePanel request={servidor(()=>json({title:'El ajuste requiere una observación.'},400))} solicitudId="sol-a" aprobadaId="a1" receptoraId="r1" sugerenciaAgente="Homologable"/>);
 await user.click(await screen.findByLabelText('De acuerdo con el agente'));
 await user.click(screen.getByRole('button',{name:'Guardar mi análisis'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('El ajuste requiere una observación.');
 expect(screen.queryByText('Su análisis quedó registrado.')).not.toBeInTheDocument();
});

it('no anuncia en otra pareja el guardado pendiente de la pareja anterior',async()=>{
 let terminar!:(response:Response)=>void;
 const pendiente=new Promise<Response>(resolve=>{terminar=resolve;});
 const request=vi.fn(async(_path:string,init?:RequestInit)=>init?.method==='PUT'?pendiente:json([]));
 const user=userEvent.setup();
 const props={request,solicitudId:'sol-a',aprobadaId:'a1',receptoraId:'r1',sugerenciaAgente:'Homologable'};
 const {rerender}=render(<RevisionDocentePanel {...props}/>);
 await user.click(await screen.findByLabelText('De acuerdo con el agente'));
 await user.click(screen.getByRole('button',{name:'Guardar mi análisis'}));
 rerender(<RevisionDocentePanel {...props} receptoraId="r2"/>);
 await act(async()=>{terminar(json({...otro,docenteId:'d1'}));await pendiente;});
 expect(screen.queryByText('Su análisis quedó registrado.')).not.toBeInTheDocument();
 expect(screen.queryByRole('list',{name:'Análisis de los docentes'})).not.toBeInTheDocument();
});
