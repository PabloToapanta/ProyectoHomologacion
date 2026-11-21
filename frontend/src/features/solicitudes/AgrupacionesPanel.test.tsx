import {render,screen} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {expect,it,vi} from 'vitest';
import {AgrupacionesPanel} from './AgrupacionesPanel';
const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const opciones={origenes:[{id:'o',horas:100,creditos:4,estado:'Aprobada',elegible:true,motivos:[]}],
 receptoras:[{id:'r1',codigo:'R1',nombre:'Destino uno',nivel:1,horas:60,creditos:2.5},{id:'r2',codigo:'R2',nombre:'Destino dos',nivel:2,horas:40,creditos:1.5}],
 vinculos:[{asignaturaAprobadaId:'o',asignaturaReceptoraId:'r1',habilitado:true,motivo:'Vigente'},{asignaturaAprobadaId:'o',asignaturaReceptoraId:'r2',habilitado:true,motivo:'Vigente'}]};
const aprobadas=[{id:'o',nombreAsignatura:'Origen sintético'}];
const grupo={grupoId:'g',version:'"v1"',congelado:false,origenes:['o'],receptoras:['r1','r2'],aportes:[{asignaturaAprobadaId:'o',asignaturaReceptoraId:'r1',horasAportadas:60,creditosAportados:2.5},{asignaturaAprobadaId:'o',asignaturaReceptoraId:'r2',horasAportadas:40,creditosAportados:1.5}],ultimoAnalisis:null};
it('tabla 1:N conserva aportes explícitos y muestra saldos sin duplicar cantidades',async()=>{
 const request=vi.fn(async(path:string,init?:RequestInit)=>init?.method==='POST'?json(grupo,201):json(path.endsWith('/opciones')?opciones:[]));
 render(<AgrupacionesPanel request={request} solicitudId="s" aprobadas={aprobadas}/>);const user=userEvent.setup();
 await user.click(screen.getByRole('button',{name:'Abrir agrupaciones'}));
 await user.click(await screen.findByLabelText('Vínculo Origen sintético a R1'));await user.click(screen.getByLabelText('Vínculo Origen sintético a R2'));
 for(const [label,value] of [['Horas o a R1','60'],['Créditos o a R1','2.5'],['Horas o a R2','40'],['Créditos o a R2','1.5']])await user.type(screen.getByLabelText(label),value);
 expect(screen.getByText('Saldo: 0 horas · 0 créditos')).toBeInTheDocument();
 await user.click(screen.getByRole('button',{name:'Guardar borrador'}));
 const call=request.mock.calls.find(x=>x[1]?.method==='POST')!;const body=JSON.parse(call[1]!.body as string);
 expect(body.aportes).toHaveLength(2);expect(body.aportes[0].horasAportadas).toBe(60);expect(body).not.toHaveProperty('evidencias');
});
it('celda sin revisión vigente no es seleccionable y explica el motivo',async()=>{
 const request=vi.fn(async(path:string)=>json(path.endsWith('/opciones')?{...opciones,vinculos:opciones.vinculos.map(v=>({...v,habilitado:false,motivo:'Falta revisión vigente'}))}:[]));
 render(<AgrupacionesPanel request={request} solicitudId="s" aprobadas={aprobadas}/>);await userEvent.click(screen.getByRole('button',{name:'Abrir agrupaciones'}));
 expect(await screen.findByLabelText('Vínculo Origen sintético a R1')).toBeDisabled();expect(screen.getAllByText('Falta revisión vigente')).toHaveLength(2);
});
it('409 recarga composición nueva y no mezcla los aportes anteriores',async()=>{
 let lecturas=0;
 const request=vi.fn(async(path:string,init?:RequestInit)=>init?.method==='PUT'?json({title:'Versión obsoleta'},409):json(path.endsWith('/opciones')?opciones:path.endsWith('/g')?(++lecturas===1?grupo:{...grupo,version:'"v2"',aportes:grupo.aportes.map(a=>({...a,horasAportadas:20}))}):path.endsWith('/revisiones')?[]:[grupo]));
 render(<AgrupacionesPanel request={request} solicitudId="s" aprobadas={aprobadas}/>);const user=userEvent.setup();await user.click(screen.getByRole('button',{name:'Abrir agrupaciones'}));
 await user.click(await screen.findByRole('button',{name:'Abrir grupo g'}));await user.click(screen.getByRole('button',{name:'Guardar borrador'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('recargó');expect(screen.getByLabelText('Horas o a R1')).toHaveValue(20);
 const call=request.mock.calls.find(x=>x[1]?.method==='PUT')!;expect(new Headers(call[1]?.headers).get('If-Match')).toBe('"v1"');
});
