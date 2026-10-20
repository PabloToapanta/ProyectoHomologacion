import { describe, expect, it, vi } from 'vitest';
import { carrerasApi } from './carreras-api';

const input = { nombre: 'Carrera sintética', facultad: 'FISEI', vigencia: true, modalidad: null };
const carrera = { id: 'id-de-prueba', ...input };
describe('API de carreras', () => {
  it('consulta listado y propaga cancelación', async () => {
    const request = vi.fn().mockResolvedValue(new Response(JSON.stringify([carrera])));
    const signal = new AbortController().signal;
    expect(await carrerasApi(request).listar(signal)).toEqual([carrera]);
    expect(request).toHaveBeenCalledWith('/api/carreras', expect.objectContaining({ signal }));
  });
  it('envía crear y actualizar con JSON y elimina sin intentar leer 204', async () => {
    const request = vi.fn().mockResolvedValueOnce(new Response(JSON.stringify(carrera), { status: 201 }))
      .mockResolvedValueOnce(new Response(JSON.stringify(carrera)))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    const api = carrerasApi(request);
    expect(await api.crear(input)).toEqual(carrera);
    expect(await api.actualizar(carrera.id, input)).toEqual(carrera);
    await api.eliminar(carrera.id);
    expect(request.mock.calls.map(call => call[1].method)).toEqual(['POST', 'PUT', 'DELETE']);
    expect(JSON.parse(request.mock.calls[0][1].body)).toEqual(input);
  });
  it.each([400, 409])('muestra errores ProblemDetails (%s)', async status => {
    const api = carrerasApi(vi.fn().mockResolvedValue(new Response(JSON.stringify({ detail: 'La carrera tiene asignaturas relacionadas.' }), { status })));
    await expect(api.eliminar('id')).rejects.toThrow('La carrera tiene asignaturas relacionadas.');
  });
  it('maneja fallos de red y errores no JSON', async () => {
    await expect(carrerasApi(vi.fn().mockRejectedValue(new TypeError('fetch'))).listar()).rejects.toThrow('No pudimos conectar');
    await expect(carrerasApi(vi.fn().mockResolvedValue(new Response('error', { status: 500 }))).listar()).rejects.toThrow('El servicio no está disponible');
  });
});
