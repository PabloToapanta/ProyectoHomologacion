import { render, screen, within } from '@testing-library/react';
import { expect, it } from 'vitest';
import { createElement } from 'react';
import { evidenciaPartes, explicarFaltantes } from './explicarAnalisis';
import { AnalisisSolicitud } from './SolicitudesDocentePage';

const origenId = 'a160aba4-6d7c-4087-b981-5296cc0b7c38';
const receptoraId = '582c3599-5dd8-4770-bf21-5167eb5be124';
const nombres = { origen: 'ALGEBRA LINEAL', receptora: 'Álgebra Lineal (TI)', receptoraId };
const mensaje = `Información incompleta: referencia al plan analítico de ${origenId}; referencia al plan analítico de ${receptoraId}; `
  + 'correspondencias explícitas de contenidos; correspondencias explícitas de resultados de aprendizaje; '
  + 'unidades comparables de horas, sin factores de conversión inferidos; unidades comparables de créditos, sin factores de conversión inferidos; '
  + 'faltan correspondencias explícitas revisadas y vigentes de ambos programas.';

it('traduce los faltantes del motor a pasos con nombres y sin identificadores', () => {
  const pasos = explicarFaltantes(mensaje, nombres);
  expect(pasos).toHaveLength(5);
  expect(pasos[0]).toContain('plan analítico de «ALGEBRA LINEAL» (origen)');
  expect(pasos[1]).toContain('plan analítico de «Álgebra Lineal (TI)» (receptora)');
  expect(pasos[2]).toContain('qué contenidos y resultados de aprendizaje de «ALGEBRA LINEAL» corresponden a «Álgebra Lineal (TI)»');
  expect(pasos[3]).toContain('la unidad de horas');
  expect(pasos[4]).toContain('el sistema de créditos');
  expect(pasos.join(' ')).not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}/);
});

it('otros faltantes conservan su texto y reemplazan identificadores', () => {
  expect(explicarFaltantes(`Información incompleta: fecha de aprobación ausente o futura: requiere revisión; algo de ${origenId}.`, nombres))
    .toEqual(['Fecha de aprobación ausente o futura: requiere revisión.', 'Algo de «ALGEBRA LINEAL» (origen).']);
});

it('la pantalla de información incompleta muestra los pasos', () => {
  render(createElement(AnalisisSolicitud, { analisis: { estado: 'Incompleto', mensaje, resultado: null }, nombres }));
  const seccion = screen.getByRole('status');
  expect(seccion).toHaveTextContent('Para completarla falta');
  expect(within(seccion).getAllByRole('listitem')).toHaveLength(5);
  expect(seccion).not.toHaveTextContent(origenId);
});

it('la evidencia nueva separa el tema de su ubicación; la antigua sigue legible', () => {
  expect(evidenciaPartes('C1 · Contenido: «Matrices» → «Matrices» | Ubicación: Plan SHA-256 X, unidad 1 → Plan Y, unidad 1'))
    .toEqual({ principal: 'C1 · Contenido: «Matrices» → «Matrices»', ubicacion: 'Plan SHA-256 X, unidad 1 → Plan Y, unidad 1' });
  expect(evidenciaPartes('C1: o/c [U1] → r [U2]')).toEqual({ principal: 'C1 · U1 → U2' });
});
