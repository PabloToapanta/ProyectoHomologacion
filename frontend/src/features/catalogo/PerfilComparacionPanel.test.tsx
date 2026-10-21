import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test, vi } from 'vitest';
import { PerfilComparacionPanel } from './PerfilComparacionPanel';
test('los metadatos se editan y guardan una vez en el catálogo', async () => {
  const request = vi.fn().mockResolvedValueOnce(new Response(null, { status: 204 })).mockResolvedValueOnce(Response.json({}));
  const user = userEvent.setup(); render(<PerfilComparacionPanel id="a" nombre="Materia" request={request} cerrar={() => {}} />);
  await screen.findByText('Perfil de comparación · Materia');
  await user.type(screen.getByLabelText('Referencia al plan analítico'), 'Plan DEMO v1');
  await user.type(screen.getByLabelText('Unidad de horas'), 'hora-reloj');
  await user.type(screen.getByLabelText('Sistema de créditos'), 'DEMO');
  await user.click(screen.getByLabelText('Datos sintéticos de demostración'));
  await user.click(screen.getByRole('button', { name: 'Guardar perfil' }));
  await screen.findByText('Perfil guardado.');
  expect(JSON.parse(request.mock.calls[1]![1].body)).toEqual({ planAnalitico: 'Plan DEMO v1', unidadHoras: 'hora-reloj', sistemaCreditos: 'DEMO', esSintetico: true });
});
