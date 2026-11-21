import {ResultadoAgrupacion,type ReceptoraEvaluada,type EvaluacionGrupo} from './ResultadoAgrupacion';
export type GrupoInforme={grupoId:string;asignaturasAprobadas:string[];receptoras:ReceptoraEvaluada[];estadoAnalisis:string;leyenda:string;sugerenciaFinal:string;
 docentes:{docenteId:string;docente:string;postura:string;decision:string;observacion:string|null}[];aportes:{asignaturaOrigenId:string;asignaturaReceptoraId:string;horasAportadas:number|null;creditosAportados:number|null}[];evidencias:EvaluacionGrupo['evidencias']};
export function GruposInforme({grupos,nombre}:{grupos:GrupoInforme[];nombre:(id:string)=>string}){
 return <section aria-label="Agrupaciones del informe">{grupos.map(g=><article key={g.grupoId}><h4>Grupo {g.grupoId} · {g.sugerenciaFinal}</h4>
  <p>Homologación por agrupación — regla de análisis pendiente de validación institucional</p><p>{g.leyenda}</p><p>Orígenes: {g.asignaturasAprobadas.map(nombre).join(', ')}</p>
  <ul>{g.aportes.map(a=><li key={`${a.asignaturaOrigenId}/${a.asignaturaReceptoraId}`}>{nombre(a.asignaturaOrigenId)} → {nombre(a.asignaturaReceptoraId)}: {a.horasAportadas??'Pendiente'} horas · {a.creditosAportados??'Pendiente'} créditos</li>)}</ul>
  <ResultadoAgrupacion nombre={nombre} evaluacion={{estado:g.estadoAnalisis,justificacion:'Desglose fotografiado al generar el informe; no se recalcula con el Catálogo actual.',configuracionVersionId:'Conservada en el análisis grupal',receptoras:g.receptoras,evidencias:g.evidencias}}/>
  <ul aria-label="Revisiones fotografiadas del grupo">{g.docentes.map(d=><li key={d.docenteId}>{d.docente} · {d.postura} · {d.decision} · {d.observacion}</li>)}</ul>
 </article>)}</section>;
}
