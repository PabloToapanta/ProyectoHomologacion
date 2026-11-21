import {render,screen,within} from '@testing-library/react';
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
const rutaAsignaturas='/api/solicitudes/sol-1/asignaturas-aprobadas';
const catalogo=[{id:'cat-1',codigo:'COD-1',nombre:'Álgebra sintética',nivel:1,creditos:3,horas:144}];
const algebra={id:'asig-1',asignaturaOrigenId:'cat-1',codigoAsignatura:'COD-1',nombreAsignatura:'Álgebra sintética',creditos:3,sistemaCreditos:null,horas:144,unidadHoras:null,referenciaPrograma:null,nota:8.55,notaMaximaEscala:10,notaMinimaAprobacion:7,estado:'Aprobada',fechaAprobacion:'2025-06-15',numeroMatricula:'MAT-SINTETICA-1',intentoMatricula:2};
const externa={...algebra,id:'asig-2',asignaturaOrigenId:null,codigoAsignatura:null,nombreAsignatura:'Álgebra externa',creditos:4,sistemaCreditos:'Créditos sintéticos',horas:64,unidadHoras:'Horas sintéticas',referenciaPrograma:'PROGRAMA-SINTETICO-1',nota:14,notaMaximaEscala:20,notaMinimaAprobacion:12,intentoMatricula:1};
function servidor(asignaturas:unknown[],responder:()=>Response=()=>json(algebra,201),tipoCambio='Interno',origen:()=>Response=()=>json(catalogo)) {
 return vi.fn(async(path:string,_init?:RequestInit)=>path===rutaAsignaturas?responder():path.endsWith('/asignaturas-origen')?origen():path.endsWith('/carreras')?json(carreras):path==='/api/solicitudes/sol-1'?json({...registrada,tipoCambio,asignaturasAprobadas:asignaturas}):json([{...registrada,tipoCambio}]));
}
async function abrirDetalle(user:ReturnType<typeof userEvent.setup>) {await user.click(await screen.findByRole('button',{name:'Ver UTA-001'}));await screen.findByRole('heading',{name:'Solicitud UTA-001'});}
async function datosComunes(user:ReturnType<typeof userEvent.setup>,nota:string) {
 await user.type(screen.getByLabelText('Nota'),nota);await user.type(screen.getByLabelText('Fecha de aprobación'),'2025-06-15');await user.type(screen.getByLabelText('Número de matrícula'),'MAT-SINTETICA-1');await user.selectOptions(screen.getByLabelText('Intento de matrícula'),'2');
}
async function completarInterna(user:ReturnType<typeof userEvent.setup>) {
 await user.click(screen.getByRole('button',{name:'Agregar asignatura aprobada'}));await user.selectOptions(await screen.findByLabelText('Asignatura de origen'),'cat-1');await datosComunes(user,'8.55');
}
it('el detalle muestra datos, escala, estado e intento y aclara que no implican elegibilidad',async()=>{
 const user=userEvent.setup();render(<SolicitudesPage request={servidor([algebra])}/>);await abrirDetalle(user);
 const fila=screen.getByRole('row',{name:/Álgebra sintética/});for(const texto of ['COD-1','8.55 / 10','Aprobada','15/06/2025','MAT-SINTETICA-1','Segunda','144'])expect(fila).toHaveTextContent(texto);
 expect(screen.getByText(/no implica elegibilidad/i)).toBeInTheDocument();
});
it('sin asignaturas lo indica y el formulario permanece cerrado',async()=>{
 const user=userEvent.setup();render(<SolicitudesPage request={servidor([])}/>);await abrirDetalle(user);
 expect(screen.getByText('Sin asignaturas aprobadas registradas.')).toBeInTheDocument();expect(screen.queryByLabelText('Nota')).not.toBeInTheDocument();
});
it('interna selecciona del catálogo, muestra sus datos y envía solo lo necesario',async()=>{
 const user=userEvent.setup();const request=servidor([]);render(<SolicitudesPage request={request}/>);await abrirDetalle(user);await completarInterna(user);
 expect(screen.getByText('3 créditos · 144 horas')).toBeInTheDocument();expect(screen.queryByLabelText('Sistema de créditos')).not.toBeInTheDocument();expect(screen.getByLabelText('Nota')).toHaveAttribute('max','10');
 await user.click(screen.getByRole('button',{name:'Registrar asignatura'}));
 expect(await screen.findByText('Asignatura registrada.')).toBeInTheDocument();expect(screen.getByRole('row',{name:/Álgebra sintética/})).toBeInTheDocument();expect(screen.queryByLabelText('Nota')).not.toBeInTheDocument();
 const envio=request.mock.calls.find(call=>call[0]===rutaAsignaturas);expect(envio?.[1]?.method).toBe('POST');
 expect(JSON.parse(String(envio?.[1]?.body))).toEqual({asignaturaOrigenId:'cat-1',nota:8.55,fechaAprobacion:'2025-06-15',numeroMatricula:'MAT-SINTETICA-1',intentoMatricula:2});
});
it('externa pide datos y escala de origen y no consulta el catálogo',async()=>{
 const user=userEvent.setup();const request=servidor([],()=>json(externa,201),'Externo');render(<SolicitudesPage request={request}/>);await abrirDetalle(user);
 await user.click(screen.getByRole('button',{name:'Agregar asignatura aprobada'}));expect(screen.queryByLabelText('Asignatura de origen')).not.toBeInTheDocument();
 await user.type(screen.getByLabelText('Nombre de la asignatura'),'Álgebra externa');await user.type(screen.getByLabelText('Créditos'),'4');await user.type(screen.getByLabelText('Sistema de créditos'),'Créditos sintéticos');await user.type(screen.getByLabelText('Horas'),'64');await user.type(screen.getByLabelText('Unidad de horas'),'Horas sintéticas');await user.type(screen.getByLabelText('Referencia al programa analítico'),'PROGRAMA-SINTETICO-1');await user.type(screen.getByLabelText('Nota máxima de la escala'),'20');await user.type(screen.getByLabelText('Nota mínima de aprobación'),'12');await datosComunes(user,'14');
 await user.click(screen.getByRole('button',{name:'Registrar asignatura'}));
 const fila=await screen.findByRole('row',{name:/Álgebra externa/});expect(fila).toHaveTextContent('14 / 20');expect(request.mock.calls.some(call=>call[0].endsWith('/asignaturas-origen'))).toBe(false);
 expect(JSON.parse(String(request.mock.calls.find(call=>call[0]===rutaAsignaturas)?.[1]?.body))).toEqual({nombreAsignatura:'Álgebra externa',creditos:4,sistemaCreditos:'Créditos sintéticos',horas:64,unidadHoras:'Horas sintéticas',referenciaPrograma:'PROGRAMA-SINTETICO-1',nota:14,notaMaximaEscala:20,notaMinimaAprobacion:12,fechaAprobacion:'2025-06-15',numeroMatricula:'MAT-SINTETICA-1',intentoMatricula:2});
});
it('rechazo del servidor conserva los datos y no anuncia éxito',async()=>{
 const user=userEvent.setup();render(<SolicitudesPage request={servidor([],()=>json({title:'La fecha de aprobación no puede ser futura.'},400))}/>);await abrirDetalle(user);await completarInterna(user);
 await user.click(screen.getByRole('button',{name:'Registrar asignatura'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('no puede ser futura');expect(screen.getByLabelText('Número de matrícula')).toHaveValue('MAT-SINTETICA-1');expect(screen.queryByText('Asignatura registrada.')).not.toBeInTheDocument();
});
it('catálogo no disponible informa el error sin abrir el formulario interno',async()=>{
 const user=userEvent.setup();render(<SolicitudesPage request={servidor([],undefined,'Interno',()=>json({title:'No se pudo comprobar el catálogo.'},503))}/>);await abrirDetalle(user);
 await user.click(screen.getByRole('button',{name:'Agregar asignatura aprobada'}));expect(await screen.findByRole('alert')).toHaveTextContent('catálogo');expect(screen.queryByLabelText('Nota')).not.toBeInTheDocument();
});
it('cancelar el registro de asignatura no envía datos',async()=>{
 const user=userEvent.setup();const request=servidor([]);render(<SolicitudesPage request={request}/>);await abrirDetalle(user);await completarInterna(user);
 await user.click(screen.getByRole('button',{name:'Cancelar registro'}));expect(screen.queryByLabelText('Nota')).not.toBeInTheDocument();expect(request.mock.calls.some(call=>call[0]===rutaAsignaturas)).toBe(false);
});
it('fallo de carga permite reintentar sin inventar listado',async()=>{
 const request=vi.fn().mockRejectedValue(new Error('Sin conexión'));render(<SolicitudesPage request={request}/>);expect(await screen.findByRole('alert')).toHaveTextContent('Sin conexión');expect(screen.getByRole('button',{name:'Reintentar'})).toBeInTheDocument();expect(screen.queryByText('No hay solicitudes registradas.')).not.toBeInTheDocument();
});
it('externa registra períodos y cambios previos opcionales y los corrige antes de asignar docentes',async()=>{
 const user=userEvent.setup();
 const externaRegistrada={...registrada,tipoCambio:'Externo',carreraOrigenId:null,institucionOrigen:'Universidad sintética Ecuador',carreraOrigenExterna:'Carrera externa sintética',periodosCursados:null,cambiosPreviosCarrera:0,asignaturasAprobadas:[],docentesAsignados:[]};
 const request=vi.fn(async(path:string,init?:RequestInit)=>path.endsWith('/carreras')?json(carreras):path==='/api/solicitudes'&&init?.method==='POST'?json(externaRegistrada,201):
  path.endsWith('/datos-cambio-externo')?json({...externaRegistrada,periodosCursados:4}):path==='/api/solicitudes/sol-1'?json(externaRegistrada):path.endsWith('/docentes-disponibles')?json([]):json([]));
 render(<SolicitudesPage request={request}/>);
 await user.click(await screen.findByRole('button',{name:'Nueva solicitud'}));
 await user.type(screen.getByLabelText('Número de trámite Quipux'),'UTA-001');await user.type(screen.getByLabelText('Identificación del estudiante'),'EST-01');await user.type(screen.getByLabelText('Nombre del estudiante'),'Estudiante sintético');
 await user.selectOptions(screen.getByLabelText('Tipo de cambio'),'Externo');
 await user.type(screen.getByLabelText('Universidad de origen (Ecuador)'),'Universidad sintética Ecuador');await user.type(screen.getByLabelText('Carrera de origen externa'),'Carrera externa sintética');
 await user.selectOptions(screen.getByLabelText('Carrera receptora'),'receptora');
 await user.type(screen.getByLabelText('Cambios previos de carrera (opcional)'),'0');
 await user.click(screen.getByRole('button',{name:'Registrar solicitud'}));
 await screen.findByRole('status');
 const alta=request.mock.calls.find(c=>c[0]==='/api/solicitudes'&&c[1]?.method==='POST')!;
 const enviado=JSON.parse(String(alta[1]!.body));
 expect(enviado.periodosCursados).toBeNull();expect(enviado.cambiosPreviosCarrera).toBe(0);
 await user.click(screen.getByRole('button',{name:'Ver UTA-001'}));
 const dialogo=await screen.findByRole('dialog',{name:'Solicitud UTA-001'});
 expect(within(dialogo).getByText('Sin dato')).toBeInTheDocument();
 await user.type(within(dialogo).getByLabelText('Períodos cursados'),'4');
 await user.click(within(dialogo).getByRole('button',{name:'Guardar datos del cambio externo'}));
 expect(await within(dialogo).findByText('Datos del cambio externo guardados.')).toBeInTheDocument();
 const put=request.mock.calls.find(c=>c[1]?.method==='PUT')!;
 expect(put[0]).toBe('/api/solicitudes/sol-1/datos-cambio-externo');
 expect(JSON.parse(String(put[1]!.body))).toEqual({periodosCursados:4,cambiosPreviosCarrera:0});
});
