import {render,screen} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {expect,it,vi} from 'vitest';
import {AdjuntosSolicitud} from './AdjuntosSolicitud';

const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status,headers:{'Content-Type':'application/json'}});
const metadata={id:'adj-1',solicitudId:'sol-1',tipo:'Sílabo',nombreArchivo:'silabo.pdf',tamanoBytes:1024,huellaSha256:'A'.repeat(64),subidoPor:'coordinador-sintetico',fechaSubidaUtc:'2026-11-15T12:00:00Z'};

it('permite adjuntar el PDF, lista sus metadatos y permite descargarlo',async()=>{
 const user=userEvent.setup();
 const request=vi.fn(async(_path:string,init?:RequestInit)=>init?.method==='POST'?json(metadata,201):_path.endsWith('/adj-1')?new Response('%PDF-1.7\n', {status:200,headers:{'Content-Type':'application/pdf'}}):json([metadata]));
 render(<AdjuntosSolicitud solicitudId="sol-1" request={request} puedeSubir/>);
 expect(await screen.findByText(/silabo\.pdf/)).toBeInTheDocument();
 await user.selectOptions(screen.getByLabelText('Tipo de documento'),'Sílabo');
 await user.upload(screen.getByLabelText('Archivo PDF'),new File(['%PDF-1.7\n'], 'silabo.pdf',{type:'application/pdf'}));
 expect((screen.getByLabelText('Archivo PDF') as HTMLInputElement).files).toHaveLength(1);
 await user.click(screen.getByRole('button',{name:'Adjuntar PDF'}));
 expect(request.mock.calls.some(([path,init])=>path==='/api/solicitudes/sol-1/adjuntos'&&init?.method==='POST')).toBe(true);
 expect(await screen.findByRole('status')).toHaveTextContent('Adjunto guardado en el expediente.');
 const post=request.mock.calls.find(call=>call[1]?.method==='POST');
 expect(post?.[1]?.body).toBeInstanceOf(FormData);
 expect((post?.[1]?.body as FormData).get('tipo')).toBe('Sílabo');
 const click=vi.spyOn(HTMLAnchorElement.prototype,'click').mockImplementation(()=>{});
 vi.stubGlobal('URL',{createObjectURL:vi.fn(()=> 'blob:adjunto-sintetico'),revokeObjectURL:vi.fn()});
 await user.click(screen.getByRole('button',{name:'Descargar'}));
 expect(click).toHaveBeenCalledOnce();click.mockRestore();
 expect(request).toHaveBeenCalledWith('/api/solicitudes/sol-1/adjuntos/adj-1');
});

it('no ofrece carga en vista docente ni en solicitud finalizada',async()=>{
 const request=vi.fn(async()=>json([metadata]));
 const {rerender}=render(<AdjuntosSolicitud solicitudId="sol-1" request={request} puedeSubir={false}/>);
 expect(await screen.findByText(/silabo\.pdf/)).toBeInTheDocument();
 expect(screen.queryByRole('button',{name:'Adjuntar PDF'})).not.toBeInTheDocument();
 rerender(<AdjuntosSolicitud solicitudId="sol-1" request={request} puedeSubir finalizada/>);
 expect(screen.queryByRole('button',{name:'Adjuntar PDF'})).not.toBeInTheDocument();
});
