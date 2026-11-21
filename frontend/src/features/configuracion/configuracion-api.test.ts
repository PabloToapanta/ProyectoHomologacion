import { expect, it, vi } from 'vitest';
import { configuracionApi } from './configuracion-api';

const parametros = { versionId: 'v1', umbralPct: 80, antiguedadMaximaAnios: 10,
  periodosMinimosInterno: 1, asignaturasHomologablesMinimasInterno: 1,
  periodosMinimosExterno: 2, asignaturasHomologablesMinimasExterno: 2 };

it('envía If-Match, motivo y respaldo y conserva el ETag de respuesta', async () => {
  const request = vi.fn().mockResolvedValueOnce(new Response(JSON.stringify(parametros), { headers: { ETag: '"v1"' } }))
    .mockResolvedValueOnce(new Response(JSON.stringify({ ...parametros, versionId: 'v2' }), { headers: { ETag: '"v2"' } }));
  const api = configuracionApi(request);
  expect((await api.obtener()).etag).toBe('"v1"');
  const cambio = { ...parametros, motivo: 'Acta', respaldo: 'Expediente' };
  const { versionId: _versionId, ...body } = cambio;
  expect((await api.actualizar(body, '"v1"')).etag).toBe('"v2"');
  expect(request.mock.calls[1][1].headers['If-Match']).toBe('"v1"');
  expect(JSON.parse(request.mock.calls[1][1].body)).toEqual(body);
});

it('informa conflicto y falta de ETag', async () => {
  const conflicto = configuracionApi(vi.fn().mockResolvedValue(new Response(JSON.stringify({ detail: 'Versión obsoleta' }), { status: 409 })));
  await expect(conflicto.actualizar({ umbralPct: 80, antiguedadMaximaAnios: 10,
    periodosMinimosInterno: 1, asignaturasHomologablesMinimasInterno: 1,
    periodosMinimosExterno: 2, asignaturasHomologablesMinimasExterno: 2,
    motivo: 'Acta', respaldo: 'Expediente' }, '"v1"')).rejects.toThrow('Versión obsoleta');
  await expect(configuracionApi(vi.fn().mockResolvedValue(new Response(JSON.stringify(parametros)))).obtener())
    .rejects.toThrow('no incluyó la versión');
});
