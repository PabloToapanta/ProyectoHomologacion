namespace Solicitudes.Domain;
public sealed class SolicitudNoEncontradaException() : Exception("Solicitud no encontrada.");
public sealed class AsignaturaAprobada
{
 public const decimal NotaMaximaFisei=10m;
 public const decimal NotaMinimaAprobacionFisei=7m;
 public const int IntentoMaximo=3;
 // Límite técnico de decimal(5,2) para escalas de otras universidades; no es una regla institucional.
 const decimal LimiteTecnicoEscala=999.99m;
 const decimal LimiteTecnicoCreditos=9999.99m;
 public Guid Id {get; private set;}
 public Guid SolicitudId {get; private set;}
 public Guid? AsignaturaOrigenId {get; private set;}
 public string? CodigoAsignatura {get; private set;}
 public string NombreAsignatura {get; private set;} = "";
 public decimal Creditos {get; private set;}
 public string? SistemaCreditos {get; private set;}
 public int Horas {get; private set;}
 public string? UnidadHoras {get; private set;}
 public string? ReferenciaPrograma {get; private set;}
 public decimal Nota {get; private set;}
 public decimal NotaMaximaEscala {get; private set;}
 public decimal NotaMinimaAprobacion {get; private set;}
 public DateOnly FechaAprobacion {get; private set;}
 public string NumeroMatricula {get; private set;} = "";
 public int IntentoMatricula {get; private set;}
 public string Estado=>Nota>=NotaMinimaAprobacion?"Aprobada":"Reprobada";
 private AsignaturaAprobada() { }
 public static AsignaturaAprobada Interna(Guid solicitudId,Guid asignaturaOrigenId,string codigo,string nombre,decimal creditos,int horas,decimal nota,DateOnly fechaAprobacion,string numeroMatricula,int intentoMatricula,DateOnly hoy)
 {
  ValidarNota(nota,NotaMaximaFisei,NotaMinimaAprobacionFisei);
  return new AsignaturaAprobada {Id=Guid.NewGuid(),SolicitudId=solicitudId,AsignaturaOrigenId=asignaturaOrigenId,CodigoAsignatura=codigo,NombreAsignatura=nombre,Creditos=creditos,Horas=horas,Nota=nota,NotaMaximaEscala=NotaMaximaFisei,NotaMinimaAprobacion=NotaMinimaAprobacionFisei}.ConMatricula(fechaAprobacion,numeroMatricula,intentoMatricula,hoy);
 }
 public static AsignaturaAprobada Externa(Guid solicitudId,string nombre,decimal creditos,string sistemaCreditos,int horas,string unidadHoras,string referenciaPrograma,decimal nota,decimal notaMaximaEscala,decimal notaMinimaAprobacion,DateOnly fechaAprobacion,string numeroMatricula,int intentoMatricula,DateOnly hoy)
 {
  nombre=Solicitud.Limpiar(nombre,"Nombre de la asignatura",200);
  sistemaCreditos=Solicitud.Limpiar(sistemaCreditos,"Sistema de créditos",100);
  unidadHoras=Solicitud.Limpiar(unidadHoras,"Unidad de horas",100);
  referenciaPrograma=Solicitud.Limpiar(referenciaPrograma,"Referencia al programa analítico",200);
  if(creditos<=0 || creditos>LimiteTecnicoCreditos || decimal.Round(creditos,2)!=creditos) throw new SolicitudException("Los créditos deben ser mayores que cero, con hasta dos decimales.");
  if(horas<=0) throw new SolicitudException("Las horas deben ser mayores que cero.");
  ValidarNota(nota,notaMaximaEscala,notaMinimaAprobacion);
  return new AsignaturaAprobada {Id=Guid.NewGuid(),SolicitudId=solicitudId,NombreAsignatura=nombre,Creditos=creditos,SistemaCreditos=sistemaCreditos,Horas=horas,UnidadHoras=unidadHoras,ReferenciaPrograma=referenciaPrograma,Nota=nota,NotaMaximaEscala=notaMaximaEscala,NotaMinimaAprobacion=notaMinimaAprobacion}.ConMatricula(fechaAprobacion,numeroMatricula,intentoMatricula,hoy);
 }
 AsignaturaAprobada ConMatricula(DateOnly fechaAprobacion,string numeroMatricula,int intentoMatricula,DateOnly hoy)
 {
  if(fechaAprobacion==default) throw new SolicitudException("La fecha de aprobación es obligatoria.");
  if(fechaAprobacion>hoy) throw new SolicitudException("La fecha de aprobación no puede ser futura.");
  if(intentoMatricula<1 || intentoMatricula>IntentoMaximo) throw new SolicitudException("El intento de matrícula debe ser primera, segunda o tercera.");
  FechaAprobacion=fechaAprobacion;NumeroMatricula=Solicitud.Limpiar(numeroMatricula,"Número de matrícula",50);IntentoMatricula=intentoMatricula;
  return this;
 }
 static void ValidarNota(decimal nota,decimal maxima,decimal minimaAprobacion)
 {
  if(maxima<=0 || maxima>LimiteTecnicoEscala || decimal.Round(maxima,2)!=maxima) throw new SolicitudException("La nota máxima de la escala debe ser mayor que cero, con hasta dos decimales.");
  if(minimaAprobacion<=0 || minimaAprobacion>maxima || decimal.Round(minimaAprobacion,2)!=minimaAprobacion) throw new SolicitudException("La nota mínima de aprobación debe estar dentro de la escala.");
  if(nota<0 || nota>maxima || decimal.Round(nota,2)!=nota) throw new SolicitudException($"La nota debe estar entre 0 y {maxima:0.##}, con hasta dos decimales.");
 }
}
