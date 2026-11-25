import { expect, it } from 'vitest';

/** Código fuente de las pantallas (sin pruebas), para revisar que cada control tenga estilo de la aplicación. */
const fuentes = Object.entries(import.meta.glob('../../**/*.tsx', { query: '?raw', import: 'default', eager: true }) as Record<string, string>)
  .filter(([ruta]) => !ruta.includes('.test.'));

/** Etiquetas de apertura completas, respetando llaves de JSX (las flechas «=>» no cierran la etiqueta). */
function etiquetas(codigo: string, nombre: string) {
  const resultado: string[] = [];
  const patron = new RegExp(`<${nombre}\\b`, 'g');
  for (let m = patron.exec(codigo); m; m = patron.exec(codigo)) {
    let i = m.index + m[0].length, nivel = 0;
    for (; i < codigo.length; i++) {
      if (codigo[i] === '{') nivel++;
      else if (codigo[i] === '}') nivel--;
      else if (codigo[i] === '>' && nivel === 0) break;
    }
    resultado.push(codigo.slice(m.index, i + 1));
  }
  return resultado;
}

it('todo botón y enlace tiene una clase de estilo', () => {
  const sinEstilo = fuentes.flatMap(([ruta, codigo]) => ['button', 'a', 'Link'].flatMap(n => etiquetas(codigo, n))
    .filter(t => !t.includes('className')).map(t => `${ruta}: ${t.slice(0, 80)}`));
  expect(sinEstilo).toEqual([]);
});

it('los botones de la aplicación indican su variante (principal, secundaria o de peligro)', () => {
  const sinVariante = fuentes.flatMap(([ruta, codigo]) => etiquetas(codigo, 'button')
    .filter(t => /className="app-button"/.test(t)).map(t => `${ruta}: ${t.slice(0, 80)}`));
  expect(sinVariante).toEqual([]);
});
