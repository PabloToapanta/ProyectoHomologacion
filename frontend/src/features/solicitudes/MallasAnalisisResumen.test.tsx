import { render, screen } from '@testing-library/react';
import { expect, it } from 'vitest';
import { MallasAnalisisResumen } from './MallasAnalisisResumen';

it('identifica un histórico sin atribuirle versiones del catálogo actual', () => {
  render(<MallasAnalisisResumen mallas={null} />);
  expect(screen.getByText('Malla: versión no registrada.')).toBeInTheDocument();
  expect(screen.queryByText(/no vigente al analizar/)).not.toBeInTheDocument();
});

it('muestra nombre y vigencia capturada de origen y receptora sin identificadores internos', () => {
  const { rerender } = render(<MallasAnalisisResumen mallas={{
    origenes: [{ asignaturaAprobadaId: 'a1', asignaturaCatalogoId: 'o1', malla: { id: 'mo', carreraId: 'co', nombre: 'Plan de origen', vigenteAlAnalizar: false } }],
    receptoras: [{ asignaturaAprobadaId: null, asignaturaCatalogoId: 'r1', malla: { id: 'mr', carreraId: 'cr', nombre: 'Plan receptor', vigenteAlAnalizar: true } }],
  }} />);
  const resumen = screen.getByLabelText('Mallas guardadas del análisis');
  expect(resumen).toHaveTextContent('Malla de origen: Plan de origen (no vigente al analizar).');
  expect(resumen).toHaveTextContent('Malla receptora: Plan receptor (vigente al analizar).');
  expect(resumen.textContent).not.toMatch(/\bmo\b|\bmr\b|\ba1\b|\bo1\b|\br1\b/);
  rerender(<MallasAnalisisResumen mallas={{ origenes: [{ asignaturaAprobadaId: 'e1', asignaturaCatalogoId: null, malla: null }], receptoras: [] }} />);
  expect(screen.getByLabelText('Mallas guardadas del análisis')).toHaveTextContent('Malla de origen: versión no registrada.');
});
