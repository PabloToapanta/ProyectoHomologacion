import {render,screen,within} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {InformePanel} from './InformePanel';

const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const ruta='/api/homologaciones/solicitudes/sol-a/informe';
const aprobadas=[{id:'a1',nombreAsignatura:'Álgebra sintética'},{id:'a2',nombreAsignatura:'Física sintética'}];
const informe={solicitudId:'sol-a',tipoCambio:'Interno',fecha:'2026-11-04T20:00:00Z',generadoPorId:'d1',generadoPor:'docente.ana',
 parejas:[{asignaturaAprobadaId:'a1',asignaturaReceptoraId:'r1',receptoraCodigo:'R-01',receptoraNombre:'Receptora sintética',
  agente:{estado:'Completo',sugerencia:'Homologable',porcentajeAcademico:90,porcentajeHoras:85,porcentajeCreditos:100},
  docentes:[{docenteId:'d1',docente:'docente.ana',postura:'DeAcuerdo',decision:'Homologable',observacion:null,fecha:'2026-11-04T18:00:00Z'},
   {docenteId:'d2',docente:'docente.luis',postura:'Ajuste',decision:'NoHomologable',observacion:'Faltan contenidos.',fecha:'2026-11-04T18:30:00Z'}],
  sugerenciaFinal:'SinAcuerdo'}],
 aprobadasSinAnalisis:['a2'],convalidar:0,noConvalidar:0,sinAcuerdo:1,
 advertencias:['Informe técnico para el coordinador de carrera; no es una resolución oficial.','Hay parejas sin acuerdo entre los docentes: decide el coordinador de carrera.']};

it('sin informe ofrece generarlo y muestra agente, docentes y sugerencia final',async()=>{
 const user=userEvent.setup();
 const request=vi.fn(async(path:string,init?:RequestInit)=>path!==ruta?json({},500):init?.method==='POST'?json(informe,201):json({title:'El informe de esta solicitud aún no se ha generado.'},404));
 render(<InformePanel request={request} solicitudId="sol-a" aprobadas={aprobadas}/>);
 expect(await screen.findByText(/aún no se ha generado/)).toBeInTheDocument();
 await user.click(screen.getByRole('button',{name:'Generar informe'}));
 const tabla=await screen.findByRole('table',{name:'Informe de homologación por pareja'});
 const fila=within(tabla).getByRole('row',{name:/Álgebra sintética/});
 for(const texto of ['R-01','Receptora sintética','Completo · Homologable','docente.ana: De acuerdo · Homologable','docente.luis: Ajuste · No homologable · Faltan contenidos.','Sin acuerdo'])expect(fila).toHaveTextContent(texto);
 expect(screen.getByText(/Física sintética/)).toBeInTheDocument();
 expect(screen.getByText(/decide el coordinador/)).toBeInTheDocument();
 expect(screen.queryByRole('button',{name:'Generar informe'})).not.toBeInTheDocument();
 expect(request.mock.calls.filter(c=>c[1]?.method==='POST')).toHaveLength(1);
});

it('un informe ya generado se muestra sin ofrecer generarlo otra vez',async()=>{
 render(<InformePanel request={vi.fn(async()=>json(informe))} solicitudId="sol-a" aprobadas={aprobadas}/>);
 expect(await screen.findByRole('table',{name:'Informe de homologación por pareja'})).toBeInTheDocument();
 expect(screen.getByText(/Generado por docente\.ana/)).toBeInTheDocument();
 expect(screen.queryByRole('button',{name:'Generar informe'})).not.toBeInTheDocument();
});

it('si faltan análisis docentes muestra el motivo y no un informe',async()=>{
 const user=userEvent.setup();
 const request=vi.fn(async(_path:string,init?:RequestInit)=>init?.method==='POST'?json({title:'Falta el análisis de un docente en las parejas con receptora: R-01.'},400):json({title:'No generado'},404));
 render(<InformePanel request={request} solicitudId="sol-a" aprobadas={aprobadas}/>);
 await user.click(await screen.findByRole('button',{name:'Generar informe'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('Falta el análisis de un docente');
 expect(screen.queryByRole('table')).not.toBeInTheDocument();
});
