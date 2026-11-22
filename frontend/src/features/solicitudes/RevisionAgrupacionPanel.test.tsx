import {render,screen} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {RevisionAgrupacionPanel} from './RevisionAgrupacionPanel';
const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
it('revisión propia del grupo envía postura sin identidad y conserva una entrada por docente',async()=>{
 const request=vi.fn(async(_path:string,init?:RequestInit)=>json(init?.method==='PUT'?{docenteId:'d',docente:'Docente sintético',postura:'Ajuste',decision:'NoHomologable',observacion:'Falta evidencia'}:[]));
 render(<RevisionAgrupacionPanel request={request} ruta="/grupos/g/revisiones" sugerencia="InformacionIncompleta"/>);
 expect(screen.getByRole('option',{name:'De acuerdo con la sugerencia'})).toBeDisabled();
 const user=userEvent.setup();await user.selectOptions(screen.getByLabelText('Decisión del grupo'),'NoHomologable');await user.type(screen.getByLabelText('Observación del grupo'),'Falta evidencia');
 await user.click(screen.getByRole('button',{name:'Guardar mi revisión del grupo'}));expect(await screen.findByText('Revisión del grupo guardada.')).toBeInTheDocument();
 expect(JSON.parse(request.mock.calls.find(x=>x[1]?.method==='PUT')![1]!.body as string)).toEqual({postura:'Ajuste',decision:'NoHomologable',observacion:'Falta evidencia'});
 await user.click(screen.getByRole('button',{name:'Guardar mi revisión del grupo'}));expect(await screen.findByRole('list',{name:'Revisiones del grupo'})).toHaveTextContent('Docente sintético');
 expect(screen.getByRole('list',{name:'Revisiones del grupo'}).children).toHaveLength(1);
});
it('informe cerrado conserva el rechazo 409 sin confirmar una revisión',async()=>{
 const request=vi.fn(async(_path:string,init?:RequestInit)=>init?.method==='PUT'?json({title:'El informe ya fue generado; revisiones cerradas.'},409):json([]));
 render(<RevisionAgrupacionPanel request={request} ruta="/grupos/g/revisiones" sugerencia="Homologable"/>);
 const user=userEvent.setup();await user.selectOptions(screen.getByLabelText('Postura del grupo'),'DeAcuerdo');await user.click(screen.getByRole('button',{name:'Guardar mi revisión del grupo'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('revisiones cerradas');expect(screen.queryByText('Revisión del grupo guardada.')).not.toBeInTheDocument();
});
