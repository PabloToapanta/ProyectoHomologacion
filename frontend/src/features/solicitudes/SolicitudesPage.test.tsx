import {render,screen} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {SolicitudesPage} from './SolicitudesPage';
const carreras=[{id:'origen',nombre:'TI sintética'},{id:'receptora',nombre:'Software sintética'}];
const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const registrada={id:'sol-1',numeroQuipux:'UTA-001',identificacionEstudiante:'EST-01',nombreEstudiante:'Estudiante sintético',tipoCambio:'Interno',carreraOrigenId:'origen',carreraReceptoraId:'receptora',estado:'Registrada',fechaRegistro:'2026-10-28T12:00:00Z'};
async function completar(user:ReturnType<typeof userEvent.setup>) {
 await user.click(await screen.findByRole('button',{name:'Nueva solicitud'}));
 await user.type(screen.getByLabelText('Número de trámite Quipux'),'UTA-001');await user.type(screen.getByLabelText('Identificación del estudiante'),'EST-01');await user.type(screen.getByLabelText('Nombre del estudiante'),'Estudiante sintético');
 await user.selectOptions(screen.getByLabelText('Carrera de origen'),'origen');await user.selectOptions(screen.getByLabelText('Carrera receptora'),'receptora');
}
it('abre registro por botón, registra y consulta detalle',async()=>{
 const user=userEvent.setup();const request=vi.fn(async(path:string,init?:RequestInit)=>path.endsWith('/carreras')?json(carreras):init?.method==='POST'?json(registrada,201):path.endsWith('/sol-1')?json(registrada):json([]));
 render(<SolicitudesPage request={request}/>);expect(screen.queryByLabelText('Número de trámite Quipux')).not.toBeInTheDocument();await completar(user);await user.click(screen.getByRole('button',{name:'Registrar solicitud'}));
 expect(await screen.findByRole('status')).toHaveTextContent('Solicitud registrada correctamente');expect(screen.queryByLabelText('Número de trámite Quipux')).not.toBeInTheDocument();
 await user.click(screen.getByRole('button',{name:'Ver UTA-001'}));expect(await screen.findByRole('heading',{name:'Solicitud UTA-001'})).toBeInTheDocument();
 expect(request).toHaveBeenCalledWith('/api/solicitudes',expect.objectContaining({method:'POST'}));
});
it('duplicado 409 conserva datos y no anuncia éxito',async()=>{
 const user=userEvent.setup();const request=vi.fn(async(path:string,init?:RequestInit)=>path.endsWith('/carreras')?json(carreras):init?.method==='POST'?json({title:'Ya existe una solicitud con ese número de trámite Quipux.'},409):json([]));
 render(<SolicitudesPage request={request}/>);await completar(user);await user.click(screen.getByRole('button',{name:'Registrar solicitud'}));expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe');expect(screen.getByLabelText('Número de trámite Quipux')).toHaveValue('UTA-001');expect(screen.queryByRole('status')).not.toBeInTheDocument();
});
it('cambio externo muestra origen separado y cancelar no envía',async()=>{
 const user=userEvent.setup();const request=vi.fn(async(path:string,_init?:RequestInit)=>json(path.endsWith('/carreras')?carreras:[]));render(<SolicitudesPage request={request}/>);
 await user.click(await screen.findByRole('button',{name:'Nueva solicitud'}));await user.selectOptions(screen.getByLabelText('Tipo de cambio'),'Externo');expect(screen.getByLabelText('Universidad de origen (Ecuador)')).toBeInTheDocument();expect(screen.getByLabelText('Carrera de origen externa')).toBeInTheDocument();expect(screen.queryByLabelText('Carrera de origen')).not.toBeInTheDocument();await user.click(screen.getByRole('button',{name:'Cancelar'}));expect(request.mock.calls.every(call=>call[1]?.method!=='POST')).toBe(true);
});
it('fallo de carga permite reintentar sin inventar listado',async()=>{
 const request=vi.fn().mockRejectedValue(new Error('Sin conexión'));render(<SolicitudesPage request={request}/>);expect(await screen.findByRole('alert')).toHaveTextContent('Sin conexión');expect(screen.getByRole('button',{name:'Reintentar'})).toBeInTheDocument();expect(screen.queryByText('No hay solicitudes registradas.')).not.toBeInTheDocument();
});
