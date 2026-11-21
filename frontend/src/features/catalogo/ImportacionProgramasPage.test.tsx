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

it('un código existente se informa dentro de la revisión, sin anunciar éxito ni salir de ella',async()=>{
 const user=userEvent.setup();render(<ImportacionProgramasPage request={servidor(()=>json({detail:'Ya existe una asignatura con ese código en la carrera.'},409))}/>);
 await analizar(user);
 await user.click(await screen.findByRole('button',{name:'Revisar plan.pdf'}));
 await user.click(screen.getByRole('button',{name:'Confirmar en el catálogo'}));
 const revision=screen.getByRole('region',{name:'Revisión de plan.pdf'});
 expect(await within(revision).findByRole('alert')).toHaveTextContent('Ya existe una asignatura con ese código');
 expect(within(revision).getByRole('alert')).toHaveTextContent(/cambia el código|ya está en el catálogo/);
 expect(screen.queryByText('plan.pdf guardado en el catálogo.')).not.toBeInTheDocument();
});

it('la revisión se abre como pantalla propia y se vuelve al lote sin perder los resultados',async()=>{
 const user=userEvent.setup();render(<ImportacionProgramasPage request={servidor()}/>);
 await analizar(user);
 await user.click(await screen.findByRole('button',{name:'Revisar plan.pdf'}));
 expect(screen.getByRole('heading',{name:'Revisión de plan.pdf'})).toHaveFocus();
 expect(screen.queryByRole('list',{name:'Resultado por archivo'})).not.toBeInTheDocument();
 expect(screen.queryByRole('button',{name:'Analizar programas'})).not.toBeInTheDocument();
 expect(screen.getByText('Archivo 1 de 2')).toBeInTheDocument();
 await user.click(screen.getByRole('button',{name:'Volver al lote'}));
 expect(screen.queryByRole('region',{name:'Revisión de plan.pdf'})).not.toBeInTheDocument();
 expect(screen.getByRole('list',{name:'Resultado por archivo'})).toHaveTextContent('plan.pdf');
});

it('al confirmar vuelve al lote con el archivo marcado como confirmado',async()=>{
 const user=userEvent.setup();render(<ImportacionProgramasPage request={servidor()}/>);
 await analizar(user);
 await user.click(await screen.findByRole('button',{name:'Revisar plan.pdf'}));
 await user.click(screen.getByRole('button',{name:'Confirmar en el catálogo'}));
 expect(await screen.findByText('plan.pdf guardado en el catálogo.')).toBeInTheDocument();
 const resultados=screen.getByRole('list',{name:'Resultado por archivo'});
 expect(within(resultados).getAllByRole('listitem')[0]).toHaveTextContent('Confirmado');
 expect(screen.queryByRole('region',{name:'Revisión de plan.pdf'})).not.toBeInTheDocument();
});

it('el lote muestra código y nombre extraídos y avisa de un código que ya existe antes de revisar',async()=>{
 const existente={carreraId:'car-1',archivos:[{...lote.archivos[0],borrador:{...borrador,advertencias:['El código ya existe en la carrera: la confirmación será rechazada sin sobrescribir.']}}]};
 const user=userEvent.setup();render(<ImportacionProgramasPage request={servidor(undefined,existente)}/>);
 await analizar(user);
 const item=within(await screen.findByRole('list',{name:'Resultado por archivo'})).getAllByRole('listitem')[0];
 expect(item).toHaveTextContent('SINT-01-02');
 expect(item).toHaveTextContent('ASIGNATURA SINTETICA');
 expect(item).toHaveTextContent('El código ya existe en la carrera');
});

it('indica el límite de archivos por lote',async()=>{
 render(<ImportacionProgramasPage request={servidor()}/>);
 expect(await screen.findByText(/Hasta 50 archivos/)).toBeInTheDocument();
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

it('confirmar un archivo no oculta otro del mismo nombre',async()=>{
 const duplicados={carreraId:'car-1',archivos:[lote.archivos[0],{...lote.archivos[0],borrador:{...borrador,codigo:'OTRO-02'}}]};
 const user=userEvent.setup();render(<ImportacionProgramasPage request={servidor(undefined,duplicados)}/>);
 await user.selectOptions(await screen.findByLabelText('Carrera'),'car-1');
 await user.upload(screen.getByLabelText('Programas analíticos (PDF)'),[pdf(),new File(['%PDF-1.4 otro'],'plan.pdf',{type:'application/pdf'})]);
 await user.click(screen.getByRole('button',{name:'Analizar programas'}));
 await user.click((await screen.findAllByRole('button',{name:'Revisar plan.pdf'}))[0]);
 await user.click(screen.getByRole('button',{name:'Confirmar en el catálogo'}));
 expect(await screen.findByText('plan.pdf guardado en el catálogo.')).toBeInTheDocument();
 expect(screen.getAllByRole('button',{name:'Revisar plan.pdf'})).toHaveLength(1);
});

it('permite reconstruir una unidad y su temática no detectadas antes de confirmar',async()=>{
 const incompleto={carreraId:'car-1',archivos:[{...lote.archivos[0],borrador:{...borrador,unidades:[],faltantes:['unidades']}}]};
 const user=userEvent.setup();const request=servidor(undefined,incompleto);render(<ImportacionProgramasPage request={request}/>);
 await analizar(user);await user.click(await screen.findByRole('button',{name:'Revisar plan.pdf'}));
 await user.click(screen.getByRole('button',{name:'Añadir unidad'}));
 await user.type(screen.getByLabelText('Nombre de la unidad 1'),'Unidad revisada');
 await user.type(screen.getByLabelText('Horas de la unidad 1'),'144');
 await user.type(screen.getByLabelText('Resultado de aprendizaje de la unidad 1'),'Resultado revisado');
 await user.type(screen.getByLabelText('Temática 1.1'),'Tema revisado');
 await user.type(screen.getByLabelText('Contenidos de 1.1 (uno por línea)'),'Contenido revisado');
 await user.click(screen.getByRole('button',{name:'Confirmar en el catálogo'}));
 expect(await screen.findByText('plan.pdf guardado en el catálogo.')).toBeInTheDocument();
 const envio=request.mock.calls.find(c=>c[0].endsWith('/confirmacion'))!;
 const programa=JSON.parse(String((envio[1]!.body as FormData).get('programa')));
 expect(programa.unidades).toEqual([{numero:1,nombre:'Unidad revisada',horas:144,resultadoAprendizaje:'Resultado revisado',tematicas:[{nombre:'Tema revisado',contenidos:['Contenido revisado']}]}]);
});
