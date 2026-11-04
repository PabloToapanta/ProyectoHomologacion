import {render,screen,within} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {SolicitudesDocentePage} from './SolicitudesDocentePage';
const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const solicitud={id:'sol-a',numeroQuipux:'UTA-030',identificacionEstudiante:'EST-01',nombreEstudiante:'Estudiante sintético',tipoCambio:'Interno',carreraOrigenId:'origen',institucionOrigen:null,carreraOrigenExterna:null,carreraReceptoraId:'receptora',estado:'Docentes asignados',fechaRegistro:'2026-10-28T12:00:00Z'};
const detalle={...solicitud,asignaturasAprobadas:[{id:'asig-1',nombreAsignatura:'Álgebra sintética',creditos:3,horas:144,nota:8.55,estado:'Aprobada'}],docentesAsignados:[{id:'d1',docenteId:'ana',nombreUsuario:'docente.ana',orden:1},{id:'d2',docenteId:'luis',nombreUsuario:'docente.luis',orden:2}]};
const pendiente={estado:'Pendiente',mensaje:'El análisis automatizado aún no se ha ejecutado para esta solicitud.',resultado:null};
const resultado={porcentajeAcademico:85.5,porcentajeHoras:90,porcentajeCreditos:null,sugerencia:'Homologable',justificacion:'Los contenidos sintéticos coinciden.',evidencias:['R1 ↔ O1: Matrices']};
function servidor(analisis:unknown=pendiente,detalleStatus=200) {
 return vi.fn(async(path:string,_init?:RequestInit)=>path==='/api/solicitudes/asignadas'?json([solicitud]):path==='/api/solicitudes/asignadas/sol-a'?(detalleStatus===200?json({solicitud:detalle,analisis}):json({title:'Solicitud no encontrada entre sus asignaciones.'},detalleStatus)):json({title:'Ruta no esperada'},500));
}
async function abrir(user:ReturnType<typeof userEvent.setup>) {
 await user.click(await screen.findByRole('button',{name:'Ver UTA-030'}));
 return screen.findByRole('dialog',{name:'Solicitud UTA-030'});
}
it('lista solo lo que devuelve la API de asignadas y consulta el detalle con su análisis pendiente',async()=>{
 const user=userEvent.setup();const request=servidor();render(<SolicitudesDocentePage request={request}/>);
 expect(await screen.findByRole('cell',{name:'UTA-030'})).toBeInTheDocument();
 const dialogo=await abrir(user);
 expect(within(dialogo).getByText('docente.ana')).toBeInTheDocument();expect(within(dialogo).getByText(/Álgebra sintética/)).toBeInTheDocument();
 expect(within(dialogo).getByRole('heading',{name:'Análisis pendiente'})).toBeInTheDocument();
 expect(within(dialogo).queryByText(/Sugerencia/)).not.toBeInTheDocument();
 expect(request.mock.calls.map(c=>c[0])).toEqual(['/api/solicitudes/asignadas','/api/solicitudes/asignadas/sol-a']);
});
it.each([
 ['Fallido',{estado:'Fallido',mensaje:'El agente no respondió.',resultado:null},'El análisis falló'],
 ['Incompleto',{estado:'Incompleto',mensaje:'Falta el programa analítico de origen.',resultado:null},'Información incompleta'],
 ['Completo sin resultado',{estado:'Completo',mensaje:'',resultado:null},'Información incompleta'],
 ['Desconocido',{estado:'Otro',mensaje:'',resultado:resultado},'Información incompleta'],
])('estado %s se muestra explícito y sin recomendación',async(_caso,analisis,titulo)=>{
 const user=userEvent.setup();render(<SolicitudesDocentePage request={servidor(analisis)}/>);const dialogo=await abrir(user);
 expect(within(dialogo).getByRole('heading',{name:titulo})).toBeInTheDocument();
 expect(within(dialogo).queryByText(/Sugerencia/)).not.toBeInTheDocument();expect(within(dialogo).queryByText('Homologable')).not.toBeInTheDocument();
});
it('análisis completo muestra porcentajes, sugerencia, justificación y evidencias sin inventar datos faltantes',async()=>{
 const user=userEvent.setup();render(<SolicitudesDocentePage request={servidor({estado:'Completo',mensaje:'',resultado})}/>);const dialogo=await abrir(user);
 expect(within(dialogo).getByRole('heading',{name:'Resultado del análisis automatizado'})).toBeInTheDocument();
 expect(within(dialogo).getByText('85,5 %')).toBeInTheDocument();expect(within(dialogo).getByText('90 %')).toBeInTheDocument();expect(within(dialogo).getByText('Sin dato')).toBeInTheDocument();
 expect(within(dialogo).getByText('Homologable')).toBeInTheDocument();expect(within(dialogo).getByText('Los contenidos sintéticos coinciden.')).toBeInTheDocument();expect(within(dialogo).getByText('R1 ↔ O1: Matrices')).toBeInTheDocument();
 expect(within(dialogo).getByText(/no es la decisión final/)).toBeInTheDocument();
});
it('la consulta es de solo lectura: sin acciones de revisión, informe ni remisión',async()=>{
 const user=userEvent.setup();const request=servidor({estado:'Completo',mensaje:'',resultado});render(<SolicitudesDocentePage request={request}/>);const dialogo=await abrir(user);
 expect(within(dialogo).getAllByRole('button').map(b=>b.getAttribute('aria-label')??b.textContent)).toEqual(['Cerrar ventana','Cerrar detalle']);
 expect(request.mock.calls.every(c=>!c[1]?.method||c[1].method==='GET')).toBe(true);
});
it('detalle rechazado por la API muestra el error sin datos de otra solicitud',async()=>{
 const user=userEvent.setup();render(<SolicitudesDocentePage request={servidor(pendiente,404)}/>);
 await user.click(await screen.findByRole('button',{name:'Ver UTA-030'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('Solicitud no encontrada entre sus asignaciones.');expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});
it('sin asignaciones lo indica y un fallo de carga permite reintentar',async()=>{
 const user=userEvent.setup();let falla=true;
 const request=vi.fn(async(_path:string,_init?:RequestInit)=>falla?json({title:'Servicio no disponible.'},503):json([]));
 render(<SolicitudesDocentePage request={request}/>);expect(await screen.findByRole('alert')).toHaveTextContent('Servicio no disponible.');
 falla=false;await user.click(screen.getByRole('button',{name:'Reintentar'}));expect(await screen.findByText('No tiene solicitudes asignadas.')).toBeInTheDocument();
});
