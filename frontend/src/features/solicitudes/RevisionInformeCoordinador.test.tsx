import {render,screen,within} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {expect,it,vi} from 'vitest';
import {RevisionInformeCoordinador} from './RevisionInformeCoordinador';

const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status,headers:{'Content-Type':'application/json'}});
const base='/api/homologaciones/solicitudes/sol-1/informe';
const docente=(id:string,nombre:string,decision:string)=>({docenteId:id,docente:nombre,postura:'Ajuste',decision,observacion:`Observación de ${nombre}`,fecha:'2026-11-20T10:00:00Z'});
const pareja=(aprobada:string,receptora:string,codigo:string,final:string)=>({asignaturaAprobadaId:aprobada,asignaturaReceptoraId:receptora,receptoraCodigo:codigo,receptoraNombre:`Receptora ${codigo}`,
 agente:{estado:'Completo',sugerencia:'Homologable',porcentajeAcademico:85,porcentajeHoras:90,porcentajeCreditos:100},
 docentes:[docente('d1','docente.ana','Homologable'),docente('d2','docente.luis',final==='SinAcuerdo'?'NoHomologable':'Homologable')],sugerenciaFinal:final});
const informe={solicitudId:'sol-1',tipoCambio:'Interno',fecha:'2026-11-20T12:00:00Z',generadoPor:'docente.ana',
 parejas:[pareja('a1','r1','R-01','SinAcuerdo'),pareja('a2','r2','R-02','Convalidar')],aprobadasSinAnalisis:[],convalidar:1,noConvalidar:0,sinAcuerdo:1,
 advertencias:['Informe técnico para el coordinador de carrera; no es una resolución oficial.']};
const revision=(extra:object={})=>({informe,remitidoEl:'2026-11-21T09:00:00Z',remitidoPor:'docente.luis',decisiones:[],decisionesPendientes:1,finalizacion:null,...extra});
const decision={solicitudId:'sol-1',asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',resultado:'NoConvalidar',justificacion:'No cubre los resultados de aprendizaje.',coordinadorId:'c1',coordinador:'coordinadora.eva',fecha:'2026-11-23T15:00:00Z'};

it('HU-33 CA1: muestra el informe remitido con el agente, ambos docentes y lo que falta decidir',async()=>{
 render(<RevisionInformeCoordinador request={vi.fn(async()=>json(revision()))} solicitudId="sol-1" aprobadas={[{id:'a1',nombreAsignatura:'Álgebra'}]}/>);
 const tabla=await screen.findByRole('table',{name:'Revisión del informe por pareja'});
 const filas=within(tabla).getAllByRole('row');
 expect(filas).toHaveLength(3);
 expect(filas[1]).toHaveTextContent('Álgebra');expect(filas[1]).toHaveTextContent('R-01');
 expect(filas[1]).toHaveTextContent('85 %');expect(filas[1]).toHaveTextContent('docente.ana');expect(filas[1]).toHaveTextContent('docente.luis');
 expect(filas[1]).toHaveTextContent('Sin acuerdo');
 expect(screen.getByText(/Remitido por docente\.luis/)).toBeInTheDocument();
 expect(screen.getByText(/no es una resolución oficial/)).toBeInTheDocument();
 // CA2: no se puede finalizar mientras falte una decisión.
 expect(screen.getByRole('button',{name:'Finalizar solicitud'})).toBeDisabled();
 expect(screen.getByText(/Falta decidir 1 pareja/)).toBeInTheDocument();
});

it('HU-33 CA2–CA3: registra la decisión motivada y finaliza la solicitud',async()=>{
 const user=userEvent.setup();
 let estado=revision();
 const request=vi.fn(async(path:string,init?:RequestInit)=>{
  if(path===`${base}/decisiones`&&init?.method==='PUT'){estado=revision({decisiones:[decision],decisionesPendientes:0});return json(decision);}
  if(path===`${base}/finalizacion`&&init?.method==='POST'){const fin={solicitudId:'sol-1',fecha:'2026-11-23T16:00:00Z',finalizadoPorId:'c1',finalizadoPor:'coordinadora.eva'};estado=revision({decisiones:[decision],decisionesPendientes:0,finalizacion:fin});return json(fin);}
  return json(estado);
 });
 const onFinalizada=vi.fn();
 render(<RevisionInformeCoordinador request={request} solicitudId="sol-1" aprobadas={[]} onFinalizada={onFinalizada}/>);
 const form=await screen.findByRole('group',{name:'Decisión del coordinador · R-01'});
 await user.selectOptions(within(form).getByLabelText('Decisión'),'NoConvalidar');
 await user.type(within(form).getByLabelText('Justificación'),'No cubre los resultados de aprendizaje.');
 await user.click(within(form).getByRole('button',{name:'Guardar decisión'}));
 const put=request.mock.calls.find(c=>c[1]?.method==='PUT');
 expect(JSON.parse(String(put?.[1]?.body))).toEqual({asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',resultado:'NoConvalidar',justificacion:'No cubre los resultados de aprendizaje.'});
 expect(await screen.findByText(/Decidido por coordinadora\.eva/)).toBeInTheDocument();
 const finalizar=screen.getByRole('button',{name:'Finalizar solicitud'});
 expect(finalizar).toBeEnabled();
 await user.click(finalizar);
 expect(await screen.findByRole('status')).toHaveTextContent('Solicitud finalizada por coordinadora.eva');
 expect(onFinalizada).toHaveBeenCalledWith('Finalizada');
 // Tras finalizar no se ofrecen cambios.
 expect(screen.queryByRole('button',{name:'Guardar decisión'})).not.toBeInTheDocument();
 expect(screen.queryByRole('button',{name:'Finalizar solicitud'})).not.toBeInTheDocument();
});

it('HU-33: una decisión sin justificación muestra el motivo del servidor y no se registra',async()=>{
 const user=userEvent.setup();
 const request=vi.fn(async(_path:string,init?:RequestInit)=>init?.method==='PUT'?json({title:'La decisión requiere una justificación.'},400):json(revision()));
 render(<RevisionInformeCoordinador request={request} solicitudId="sol-1" aprobadas={[]}/>);
 const form=await screen.findByRole('group',{name:'Decisión del coordinador · R-01'});
 await user.click(within(form).getByRole('button',{name:'Guardar decisión'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('La decisión requiere una justificación.');
 expect(screen.getByRole('button',{name:'Finalizar solicitud'})).toBeDisabled();
});

it('HU-33: sin informe remitido lo indica y no ofrece acciones',async()=>{
 render(<RevisionInformeCoordinador request={vi.fn(async()=>json({title:'El informe de esta solicitud aún no ha sido remitido al coordinador de carrera.'},404))} solicitudId="sol-1" aprobadas={[]}/>);
 expect(await screen.findByText(/aún no ha sido remitido/)).toBeInTheDocument();
 expect(screen.queryByRole('button',{name:'Finalizar solicitud'})).not.toBeInTheDocument();
});

it('HU-33: una solicitud finalizada muestra las decisiones y la finalización sin formularios',async()=>{
 const fin={solicitudId:'sol-1',fecha:'2026-11-23T16:00:00Z',finalizadoPorId:'c1',finalizadoPor:'coordinadora.eva'};
 render(<RevisionInformeCoordinador request={vi.fn(async()=>json(revision({decisiones:[decision],decisionesPendientes:0,finalizacion:fin})))} solicitudId="sol-1" aprobadas={[]}/>);
 expect(await screen.findByRole('status')).toHaveTextContent('Solicitud finalizada por coordinadora.eva');
 expect(screen.getByText(/No convalidar · No cubre los resultados de aprendizaje\./)).toBeInTheDocument();
 expect(screen.queryByRole('group',{name:/Decisión del coordinador/})).not.toBeInTheDocument();
});
