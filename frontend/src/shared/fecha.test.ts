import { expect, it } from 'vitest';
import { fechaHora } from './fecha';

it('formatea fecha y hora en 24 h sin «a. m.» que duplique el punto final', () => {
  const texto = fechaHora('2026-11-25T14:58:00');
  expect(texto).toBe('25/11/2026 14:58');
  expect(`Remitido el ${texto}.`).not.toMatch(/\.\.$/);
  expect(fechaHora('no-es-fecha')).toBe('fecha no válida');
});
