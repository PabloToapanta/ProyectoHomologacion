import {render,screen,within} from '@testing-library/react';
import {it,expect,vi} from 'vitest';
import {HistorialInforme} from './HistorialInforme';

const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const ruta='/api/homologaciones/solicitudes/sol-1/informe/historial';

it('HU-25: muestra los eventos del informe en el orden recibido, con fecha, usuario y rol',async()=>{
 const request=vi.fn(async(path:string)=>path===ruta?json([
  {id:'e1',tipo:'Generado',fecha:'2026-11-20T15:00:00Z',usuarioId:'d1',usuario:'docente.ana',rol:'Docente'},
  {id:'e2',tipo:'Remitido',fecha:'2026-11-20T16:00:00Z',usuarioId:'d1',usuario:'docente.ana',rol:'Docente'}]):json({},500));
 render(<HistorialInforme request={request} solicitudId="sol-1"/>);
 const lista=await screen.findByRole('list',{name:'Historial del informe'});
 const items=within(lista).getAllByRole('listitem');
 expect(items).toHaveLength(2);
 expect(items[0]).toHaveTextContent('Informe generado');expect(items[0]).toHaveTextContent('docente.ana (Docente)');
 expect(items[1]).toHaveTextContent('Informe remitido al coordinador');
 expect(request).toHaveBeenCalledWith(ruta,expect.objectContaining({signal:expect.anything()}));
});

it('HU-25: sin eventos indica que el informe aún no se ha generado',async()=>{
 render(<HistorialInforme request={vi.fn(async()=>json([]))} solicitudId="sol-1"/>);
 expect(await screen.findByText(/Sin eventos registrados/)).toBeInTheDocument();
});

it('HU-25: un error del servicio se informa sin inventar eventos',async()=>{
 render(<HistorialInforme request={vi.fn(async()=>json({title:'No fue posible guardar o recuperar el análisis.'},503))} solicitudId="sol-1"/>);
 expect(await screen.findByRole('alert')).toHaveTextContent('No fue posible');
 expect(screen.queryByRole('list',{name:'Historial del informe'})).not.toBeInTheDocument();
});
