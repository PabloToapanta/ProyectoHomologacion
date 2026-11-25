/** Nombres de la pareja analizada, para no mostrar identificadores internos al docente. */
export type NombresPareja = { origen?: string; receptora?: string; receptoraId?: string };

const uuid = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;
const CATALOGO = 'Catálogo → Asignaturas → «Perfil y equivalencias»';

function quien(valor: string, n: NombresPareja) {
  const v = valor.trim();
  if (n.receptoraId && v.toLowerCase() === n.receptoraId.toLowerCase()) return `«${n.receptora ?? 'asignatura receptora'}» (receptora)`;
  if (v === 'receptora') return `«${n.receptora ?? 'asignatura receptora'}» (receptora)`;
  if (/^[0-9a-f-]{36}$/i.test(v) || v === 'origen') return `«${n.origen ?? 'asignatura de origen'}» (origen)`;
  return v;
}

const sinIds = (texto: string, n: NombresPareja) => texto.replace(uuid, id => quien(id, n));

/**
 * Traduce «Información incompleta: a; b; c.» del motor (HU-14/HU-18) a pasos comprensibles para el docente:
 * qué falta, de qué asignatura y dónde se completa. No cambia el resultado ni inventa datos.
 */
export function explicarFaltantes(mensaje: string, n: NombresPareja): string[] {
  const cuerpo = mensaje.replace(/^\s*Información incompleta:\s*/i, '').replace(/\.\s*$/, '');
  const pasos: string[] = [];
  const agregar = (p: string) => { if (!pasos.includes(p)) pasos.push(p); };
  for (const parte of cuerpo.split(';').map(p => p.trim()).filter(Boolean)) {
    let m: RegExpExecArray | null;
    if ((m = /^referencia al plan analítico de (.+)$/i.exec(parte)))
      agregar(`Registrar la referencia al plan analítico de ${quien(m[1], n)} (${CATALOGO}).`);
    else if (/^correspondencias explícitas de (contenidos|resultados de aprendizaje)$/i.test(parte) || /^faltan correspondencias explícitas revisadas/i.test(parte))
      agregar(`Revisar qué contenidos y resultados de aprendizaje de «${n.origen ?? 'la asignatura de origen'}» corresponden a «${n.receptora ?? 'la receptora'}», o actualizar esa revisión si alguno de los programas cambió (${CATALOGO}).`);
    else if ((m = /^unidades comparables de (horas|créditos)/i.exec(parte)))
      agregar(`Indicar ${m[1] === 'horas' ? 'la unidad de horas' : 'el sistema de créditos'} en el perfil de ambas asignaturas, con el mismo valor para poder compararlas (${CATALOGO}).`);
    else if ((m = /^inventario de (contenidos|resultados de aprendizaje) de (.+)$/i.exec(parte)))
      agregar(`Registrar las unidades con sus ${m[1]} en ${quien(m[2], n)} (Catálogo → Asignaturas → «Ver unidades»).`);
    else if ((m = /^cantidades positivas de (.+) de origen y receptora$/i.exec(parte)))
      agregar(`Registrar ${m[1]} mayores que cero en ambas asignaturas.`);
    else agregar(sinIds(parte.charAt(0).toUpperCase() + parte.slice(1), n) + '.');
  }
  return pasos;
}

/** Evidencia nueva «C1 · Contenido: «a» → «b» | Ubicación: x → y» o antigua «C1: id/id [x] → id [y]». */
export function evidenciaPartes(e: string): { principal: string; ubicacion?: string } {
  const nueva = /^(.*?) \| Ubicación: (.*)$/.exec(e);
  if (nueva) return { principal: nueva[1], ubicacion: nueva[2] };
  const antigua = /^(\w+):.*?\[([^\]]*)\]\s*→.*?\[([^\]]*)\]\s*$/.exec(e);
  return antigua ? { principal: `${antigua[1]} · ${antigua[2]} → ${antigua[3]}` } : { principal: e };
}
