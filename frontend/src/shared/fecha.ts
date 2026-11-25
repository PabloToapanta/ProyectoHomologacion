const formato = new Intl.DateTimeFormat('es-EC', {
  day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
});

/** Fecha y hora local en formato corto de 24 h («25/11/2026 14:58»), sin «a. m.» que choque con el punto final de una frase. */
export function fechaHora(valor: string | number | Date): string {
  const fecha = valor instanceof Date ? valor : new Date(valor);
  return Number.isNaN(fecha.getTime()) ? 'fecha no válida' : formato.format(fecha).replace(',', '');
}
