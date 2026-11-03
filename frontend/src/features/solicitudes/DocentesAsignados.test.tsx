import {render,screen,within} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {SolicitudesPage} from './SolicitudesPage';
const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const carreras=[{id:'origen',nombre:'TI sintética'},{id:'receptora',nombre:'Software sintética'}];
const registrada={id:'sol-1',numeroQuipux:'UTA-001',identificacionEstudiante:'EST-01',nombreEstudiante:'Estudiante sintético',tipoCambio:'Interno',carreraOrigenId:'origen',carreraReceptoraId:'receptora',estado:'Registrada',fechaRegistro:'2026-10-28T12:00:00Z',asignaturasAprobadas:[],docentesAsignados:[]};
const docentes=[{id:'d-ana',nombreUsuario:'docente.ana'},{id:'d-luis',nombreUsuario:'docente.luis'},{id:'d-rosa',nombreUsuario:'docente.rosa'}];
const asignada={...registrada,estado:'Docentes asignados',docentesAsignados:[{id:'a1',docenteId:'d-ana',nombreUsuario:'docente.ana',orden:1},{id:'a2',docenteId:'d-luis',nombreUsuario:'docente.luis',orden:2}]};
const ruta='/api/solicitudes/sol-1/docentes';
function servidor(detalle:unknown=registrada,asignar:()=>Response=()=>json(asignada),disponibles:()=>Response=()=>json(docentes)) {
 return vi.fn(async(path:string,_init?:RequestInit)=>path===ruta?asignar():path.endsWith('/docentes-disponibles')?disponibles():path.endsWith('/carreras')?json(carreras):path==='/api/solicitudes/sol-1'?json(detalle):json([registrada]));
}
async function abrir(user:ReturnType<typeof userEvent.setup>) {await user.click(await screen.findByRole('button',{name:'Ver UTA-001'}));return screen.findByRole('heading',{name:'Solicitud UTA-001'});}
async function elegir(user:ReturnType<typeof userEvent.setup>) {
 await user.click(screen.getByRole('button',{name:'Asignar docentes'}));
 await user.selectOptions(await screen.findByLabelText('Primer docente'),'d-ana');await user.selectOptions(screen.getByLabelText('Segundo docente'),'d-luis');
}
it('asigna dos docentes, actualiza el estado del detalle y del listado',async()=>{
 const user=userEvent.setup();const request=servidor();render(<SolicitudesPage request={request}/>);await abrir(user);
 expect(screen.getByText('Sin docentes asignados.')).toBeInTheDocument();await elegir(user);
 expect(within(screen.getByLabelText('Segundo docente')).queryByRole('option',{name:'docente.ana'})).not.toBeInTheDocument();
 await user.click(screen.getByRole('button',{name:'Guardar asignación'}));
 expect(await screen.findByText('Docentes asignados correctamente.')).toBeInTheDocument();
 const lista=screen.getByRole('list',{name:'Docentes asignados'});expect(within(lista).getAllByRole('listitem').map(i=>i.textContent)).toEqual(['docente.ana','docente.luis']);
 expect(screen.queryByRole('button',{name:'Asignar docentes'})).not.toBeInTheDocument();
 expect(screen.getByRole('row',{name:/UTA-001/})).toHaveTextContent('Docentes asignados');
 const envio=request.mock.calls.find(call=>call[0]===ruta);expect(envio?.[1]?.method).toBe('POST');expect(JSON.parse(String(envio?.[1]?.body))).toEqual({docenteIds:['d-ana','d-luis']});
});
it('una solicitud con docentes los muestra sin permitir reasignar',async()=>{
 const user=userEvent.setup();render(<SolicitudesPage request={servidor(asignada)}/>);await abrir(user);
 expect(within(screen.getByRole('list',{name:'Docentes asignados'})).getAllByRole('listitem')).toHaveLength(2);expect(screen.queryByRole('button',{name:'Asignar docentes'})).not.toBeInTheDocument();
});
it('rechazo del servidor conserva la selección y no anuncia éxito',async()=>{
 const user=userEvent.setup();render(<SolicitudesPage request={servidor(registrada,()=>json({title:'Solo pueden asignarse usuarios activos con rol Docente.'},400))}/>);await abrir(user);await elegir(user);
 await user.click(screen.getByRole('button',{name:'Guardar asignación'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('rol Docente');expect(screen.getByLabelText('Primer docente')).toHaveValue('d-ana');expect(screen.queryByText('Docentes asignados correctamente.')).not.toBeInTheDocument();
});
it('si no se pueden consultar los docentes informa el error sin abrir el formulario',async()=>{
 const user=userEvent.setup();render(<SolicitudesPage request={servidor(registrada,undefined,()=>json({title:'No se pudo comprobar los docentes en el servicio de autenticación.'},503))}/>);await abrir(user);
 await user.click(screen.getByRole('button',{name:'Asignar docentes'}));expect(await screen.findByRole('alert')).toHaveTextContent('docentes');expect(screen.queryByLabelText('Primer docente')).not.toBeInTheDocument();
});
it('menos de dos docentes disponibles lo indica y no permite guardar',async()=>{
 const user=userEvent.setup();render(<SolicitudesPage request={servidor(registrada,undefined,()=>json([docentes[0]]))}/>);await abrir(user);
 await user.click(screen.getByRole('button',{name:'Asignar docentes'}));expect(await screen.findByText(/Se necesitan al menos dos docentes activos/)).toBeInTheDocument();expect(screen.getByRole('button',{name:'Guardar asignación'})).toBeDisabled();
});
it('cancelar no envía la asignación',async()=>{
 const user=userEvent.setup();const request=servidor();render(<SolicitudesPage request={request}/>);await abrir(user);await elegir(user);
 await user.click(screen.getByRole('button',{name:'Cancelar asignación'}));expect(screen.queryByLabelText('Primer docente')).not.toBeInTheDocument();expect(request.mock.calls.some(call=>call[0]===ruta)).toBe(false);
});
