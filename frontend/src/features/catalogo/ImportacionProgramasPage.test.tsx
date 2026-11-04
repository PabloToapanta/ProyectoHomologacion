import {render,screen,within} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {it,expect,vi} from 'vitest';
import {ImportacionProgramasPage} from './ImportacionProgramasPage';

const json=(data:unknown,status=200)=>new Response(JSON.stringify(data),{status});
const carreras=[{id:'car-1',nombre:'Software sintética',facultad:'FISEI',vigencia:true,modalidad:null}];
const borrador={codigo:'SINT-01-02',nombre:'ASIGNATURA SINTETICA',nivel:1,creditos:3,horas:144,
 unidades:[{numero:1,nombre:'Tema uno',horas:144,resultadoAprendizaje:'Resultado sintético.',tematicas:[{nombre:'Tema uno',contenidos:['Contenido A','Contenido B']}]}],
 faltantes:[],advertencias:['El plan no indica el nombre de cada unidad: se propone a partir de sus unidades temáticas; revíselo.']};
const lote={carreraId:'car-1',archivos:[
 {archivo:'plan.pdf',estado:'Borrador',mensaje:'Borrador listo para revisar.',borrador},
 {archivo:'escaneado.pdf',estado:'SinTexto',mensaje:'El PDF no contiene texto seleccionable; no se aplica OCR.',borrador:null}]};
const pdf=()=>new File(['%PDF-1.4 sintetico'],'plan.pdf',{type:'application/pdf'});
const otro=()=>new File(['%PDF-1.4 sin texto'],'escaneado.pdf',{type:'application/pdf'});
function servidor(confirmar:()=>Response=()=>json({id:'asig-1',codigo:'SINT-01-02'},201),analisis:unknown=lote) {
 return vi.fn(async(path:string,_init?:RequestInit)=>path==='/api/carreras'?json(carreras):path.endsWith('/analisis')?json(analisis):path.endsWith('/confirmacion')?confirmar():json({},500));
}
async function analizar(user:ReturnType<typeof userEvent.setup>) {
 await user.selectOptions(await screen.findByLabelText('Carrera'),'car-1');
 await user.upload(screen.getByLabelText('Programas analíticos (PDF)'),[pdf(),otro()]);
 await user.click(screen.getByRole('button',{name:'Analizar programas'}));
}

it('analiza varios PDF y muestra un resultado por archivo sin guardar en el catálogo',async()=>{
 const user=userEvent.setup();const request=servidor();render(<ImportacionProgramasPage request={request}/>);
 await analizar(user);
 const resultados=await screen.findByRole('list',{name:'Resultado por archivo'});
 const [plan,escaneado]=within(resultados).getAllByRole('listitem');
 expect(plan).toHaveTextContent('plan.pdf · Borrador');
 expect(escaneado).toHaveTextContent('escaneado.pdf · SinTexto');
 expect(within(escaneado).queryByRole('button')).not.toBeInTheDocument();
 const envio=request.mock.calls.find(c=>c[0]==='/api/importaciones-programas/analisis')!;
 const form=envio[1]!.body as FormData;
 expect(form.get('carreraId')).toBe('car-1');
 expect(form.getAll('archivos')).toHaveLength(2);
 expect(request.mock.calls.some(c=>String(c[0]).endsWith('/confirmacion'))).toBe(false);
});

it('el Administrador corrige el borrador y lo confirma con el PDF original',async()=>{
 const user=userEvent.setup();const request=servidor();render(<ImportacionProgramasPage request={request}/>);
 await analizar(user);
 await user.click(await screen.findByRole('button',{name:'Revisar plan.pdf'}));
 const revision=screen.getByRole('region',{name:'Revisión de plan.pdf'});
 expect(within(revision).getByText(/se propone a partir de sus unidades temáticas/)).toBeInTheDocument();
 const nombre=within(revision).getByLabelText('Nombre de la asignatura');
 await user.clear(nombre);await user.type(nombre,'Asignatura corregida');
 const unidad=within(revision).getByLabelText('Nombre de la unidad 1');
 await user.clear(unidad);await user.type(unidad,'Unidad corregida');
 await user.click(within(revision).getByRole('button',{name:'Confirmar en el catálogo'}));
 expect(await screen.findByText('plan.pdf guardado en el catálogo.')).toBeInTheDocument();
 const envio=request.mock.calls.find(c=>c[0]==='/api/importaciones-programas/confirmacion')!;
 const form=envio[1]!.body as FormData;
 const programa=JSON.parse(String(form.get('programa')));
 expect(programa).toMatchObject({carreraId:'car-1',codigo:'SINT-01-02',nombre:'Asignatura corregida',nivel:1,creditos:3,horas:144});
 expect(programa.unidades[0]).toEqual({numero:1,nombre:'Unidad corregida',horas:144,resultadoAprendizaje:'Resultado sintético.',tematicas:[{nombre:'Tema uno',contenidos:['Contenido A','Contenido B']}]});
 expect((form.get('archivo') as File).name).toBe('plan.pdf');
});

it('un código existente se informa sin anunciar éxito',async()=>{
 const user=userEvent.setup();render(<ImportacionProgramasPage request={servidor(()=>json({detail:'Ya existe una asignatura con ese código en la carrera.'},409))}/>);
 await analizar(user);
 await user.click(await screen.findByRole('button',{name:'Revisar plan.pdf'}));
 await user.click(screen.getByRole('button',{name:'Confirmar en el catálogo'}));
 expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe una asignatura con ese código');
 expect(screen.queryByText('plan.pdf guardado en el catálogo.')).not.toBeInTheDocument();
});

it('los datos no encontrados quedan vacíos y marcados, sin completarse',async()=>{
 const incompleto={carreraId:'car-1',archivos:[{archivo:'plan.pdf',estado:'Borrador',mensaje:'Borrador con datos no encontrados: complételos antes de confirmar.',
  borrador:{...borrador,nivel:null,creditos:null,faltantes:['nivel','créditos'],advertencias:[]}}]};
 const user=userEvent.setup();render(<ImportacionProgramasPage request={servidor(undefined,incompleto)}/>);
 await analizar(user);
 await user.click(await screen.findByRole('button',{name:'Revisar plan.pdf'}));
 const revision=screen.getByRole('region',{name:'Revisión de plan.pdf'});
 expect(within(revision).getByLabelText('Nivel')).toHaveValue(null);
 expect(within(revision).getByLabelText('Créditos')).toHaveValue(null);
 expect(within(revision).getByRole('list',{name:'Datos no encontrados'})).toHaveTextContent('nivel');
});
