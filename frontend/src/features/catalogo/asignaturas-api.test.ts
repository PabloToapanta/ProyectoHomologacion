import { expect, it, vi } from 'vitest';
import { asignaturasApi } from './asignaturas-api';

const input = { carreraId: 'c1', codigo: 'DEMO-ALG', nombre: 'Álgebra Lineal', nivel: 1, creditos: 3, horas: 144 };
const item = { id: 'a1', ...input };
it('consulta por carrera y detalle usando rutas acordadas', async () => {
  const request = vi.fn().mockResolvedValueOnce(new Response(JSON.stringify([item]))).mockResolvedValueOnce(new Response(JSON.stringify(item)));
  const api = asignaturasApi(request);
  const signal = new AbortController().signal;
  expect(await api.listar('c1', signal)).toEqual([item]);
  expect(request).toHaveBeenNthCalledWith(1, '/api/carreras/c1/asignaturas', expect.objectContaining({ signal }));
  expect(await api.obtener('a1')).toEqual(item);
  expect(request).toHaveBeenNthCalledWith(2, '/api/asignaturas/a1', undefined);
});
it('registra JSON con valores numéricos', async () => {
  const request = vi.fn().mockResolvedValue(new Response(JSON.stringify(item), { status: 201 }));
  expect(await asignaturasApi(request).crear(input)).toEqual(item);
  expect(request).toHaveBeenCalledWith('/api/asignaturas', expect.objectContaining({ method: 'POST', body: JSON.stringify(input) }));
});
it.each([400, 409])('explica ProblemDetails (%s)', async status => {
  const api = asignaturasApi(vi.fn().mockResolvedValue(new Response(JSON.stringify({ detail: 'Datos inválidos de asignatura.' }), { status })));
  await expect(api.crear(input)).rejects.toThrow('Datos inválidos de asignatura.');
});
it.each([[401, 'sesión ha finalizado'], [403, 'No tienes permiso'], [404, 'No se encontró'], [500, 'no está disponible']] as const)('maneja errores HTTP (%s)', async (status, message) => {
  await expect(asignaturasApi(vi.fn().mockResolvedValue(new Response(null, { status }))).listar('c1')).rejects.toThrow(message);
});
it('maneja red y propaga cancelación', async () => {
  await expect(asignaturasApi(vi.fn().mockRejectedValue(new TypeError('fetch'))).listar('c1')).rejects.toThrow('No pudimos conectar');
  const controller = new AbortController(); controller.abort();
  const error = new DOMException('Cancelada', 'AbortError');
  await expect(asignaturasApi(vi.fn().mockRejectedValue(error)).listar('c1', controller.signal)).rejects.toBe(error);
});
