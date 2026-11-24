import { render, screen } from '@testing-library/react';
import { expect, it } from 'vitest';
import { MallasAnalisisResumen } from './MallasAnalisisResumen';

it('identifica un histórico sin atribuirle versiones del catálogo actual', () => {
  render(<MallasAnalisisResumen mallas={null} />);
  expect(screen.getByText('Versión no registrada.')).toBeInTheDocument();
  expect(screen.queryByText(/No vigente al analizar/)).not.toBeInTheDocument();
});

it('muestra nombre, id y vigencia capturada de cada origen y receptora', () => {
  const { rerender } = render(<MallasAnalisisResumen mallas={{
    origenes: [{ asignaturaAprobadaId: 'a1', asignaturaCatalogoId: 'o1', malla: { id: 'mo', carreraId: 'co', nombre: 'Plan de origen', vigenteAlAnalizar: false } }],
    receptoras: [{ asignaturaAprobadaId: null, asignaturaCatalogoId: 'r1', malla: { id: 'mr', carreraId: 'cr', nombre: 'Plan receptor', vigenteAlAnalizar: true } }],
  }} />);
  expect(screen.getByText(/Plan de origen/)).toHaveTextContent('No vigente al analizar');
  expect(screen.getByText(/Plan receptor/)).toHaveTextContent('Vigente al analizar');
  expect(screen.getByText(/Plan de origen/)).toHaveTextContent('mo');
  rerender(<MallasAnalisisResumen mallas={{ origenes: [{ asignaturaAprobadaId: 'e1', asignaturaCatalogoId: null, malla: null }], receptoras: [] }} />);
  expect(screen.getByText(/versión no registrada/)).toBeInTheDocument();
});
