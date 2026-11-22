import {render,screen} from '@testing-library/react';
import {it,expect} from 'vitest';
import {GruposInforme} from './GruposInforme';
it('informe muestra una sola unidad grupal con desglose, aportes y las dos decisiones fotografiadas',()=>{
 render(<GruposInforme nombre={id=>id} grupos={[{grupoId:'g',asignaturasAprobadas:['o1','o2'],estadoAnalisis:'InformacionIncompleta',sugerenciaFinal:'SinAcuerdo',leyenda:'Homologación por agrupación — regla de análisis pendiente de validación institucional',configuracionVersionId:'11111111-1111-1111-1111-111111111111',
  docentes:[{docenteId:'d1',docente:'Ana',postura:'Ajuste',decision:'Homologable',observacion:'Revisión uno'},{docenteId:'d2',docente:'Luis',postura:'Ajuste',decision:'NoHomologable',observacion:'Revisión dos'}],
  receptoras:[{asignaturaReceptora:'r',compatibilidadAcademicaPct:null,compatibilidadHorariaPct:null,compatibilidadCreditosPct:null,estado:0,justificacion:'Falta programa',informacionFaltante:['Programa'],contenidosSinCobertura:['C2'],resultadosAprendizajeSinCobertura:[]}],
  aportes:[{asignaturaOrigenId:'o1',asignaturaReceptoraId:'r',horasAportadas:40.5,creditosAportados:1.25}],evidencias:[]}]}/>);
 expect(screen.getAllByRole('article')).toHaveLength(1);expect(screen.getByText(/Grupo g · SinAcuerdo/)).toBeInTheDocument();
 expect(screen.getByText(/Ana · Ajuste/)).toBeInTheDocument();expect(screen.getByText(/Luis · Ajuste/)).toBeInTheDocument();
 expect(screen.getByText(/40.5 horas/)).toBeInTheDocument();expect(screen.getByRole('heading',{name:'Sugerencia única: InformacionIncompleta'})).toBeInTheDocument();
 expect(screen.getByText('Versión de parámetros: 11111111-1111-1111-1111-111111111111')).toBeInTheDocument();
 expect(screen.getAllByText('Homologación por agrupación — regla de análisis pendiente de validación institucional')).toHaveLength(1);
 expect(screen.getAllByText('Sin dato')).toHaveLength(3);
});
