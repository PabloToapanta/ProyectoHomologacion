import { expect, it, vi } from 'vitest';
import { usuariosApi } from './usuarios-api';

const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), { status });

it('usa las seis rutas de Auth mediante el request autenticado', async () => {
  const usuario = { id: 'u1', nombreUsuario: 'docente.ana', rol: 'Docente', activo: true };
  const request = vi.fn().mockImplementation(async () => json(usuario));
  const api = usuariosApi(request);
  await api.listar();
  await api.crear({ nombreUsuario: 'docente.ana', rol: 'Docente', contrasena: 'Sintetica-123!' });
  await api.obtener('u1');
  await api.cambiarEstado('u1', false);
  await api.cambiarRol('u1', 'Coordinador');
  await api.cambiarContrasena('u1', 'Nueva-Sintetica-123!');
  expect(request.mock.calls.map(c => [c[0], c[1]?.method])).toEqual([
    ['/api/usuarios', undefined], ['/api/usuarios', 'POST'], ['/api/usuarios/u1', undefined],
    ['/api/usuarios/u1/estado', 'PATCH'], ['/api/usuarios/u1/rol', 'PUT'], ['/api/usuarios/u1/contrasena', 'PUT'],
  ]);
  expect(JSON.parse(request.mock.calls[1][1].body)).toEqual({ nombreUsuario: 'docente.ana', rol: 'Docente', contrasena: 'Sintetica-123!' });
  expect(JSON.parse(request.mock.calls[3][1].body)).toEqual({ activo: false });
});

it.each([
  [400, 'Revisa'], [401, 'sesión'], [403, 'permiso'], [404, 'no existe'], [409, 'ya existe'], [502, 'disponible'],
])('explica el error HTTP %i sin mostrar secretos', async (status, texto) => {
  const request = vi.fn().mockResolvedValue(json({ title: 'Error técnico', detail: 'Dato interno' }, status));
  await expect(usuariosApi(request).crear({ nombreUsuario: 'docente.ana', rol: 'Docente', contrasena: 'Sintetica-123!' }))
    .rejects.toThrow(texto);
});

it('explica fallo de red', async () => {
  const request = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'));
  await expect(usuariosApi(request).listar()).rejects.toThrow('conectar');
});
