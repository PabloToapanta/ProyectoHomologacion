import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Link, MemoryRouter, useLocation } from 'react-router';
import { expect, it, vi } from 'vitest';
import { loginResponse } from '../test/fixtures';
import { App } from './App';

const carrera = { id: 'carrera-demo', nombre: 'Software de prueba', facultad: 'Facultad sintética', vigencia: true, modalidad: null };
const asignatura = { id: 'asignatura-demo', carreraId: carrera.id, codigo: 'DEMO-ALG', nombre: 'Álgebra Lineal', nivel: 1, creditos: 3, horas: 144 };
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status });

function LocationProbe() {
  const location = useLocation();
  return <><output aria-label="Ubicación de prueba">{location.pathname}</output><Link to="/admin/carreras">Abrir catálogo de prueba</Link></>;
}

function mount(path = '/login') {
  return render(<MemoryRouter initialEntries={[path]}><App /><LocationProbe /></MemoryRouter>);
}

function mockServices(role = 'Administrador', expires = false) {
  const request = vi.fn().mockImplementation(async (path: string, init?: RequestInit) => {
    if (path === '/api/auth/login') return json(loginResponse({ role }));
    if (expires) return json({}, 401);
    if (path === '/api/carreras') return json([carrera]);
    if (path === `/api/carreras/${carrera.id}/asignaturas`) return json([]);
    if (path === '/api/asignaturas' && init?.method === 'POST') return json(asignatura, 201);
    throw new Error(`Solicitud inesperada: ${path}`);
  });
  vi.stubGlobal('fetch', request);
  return request;
}

async function login() {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Usuario'), 'admin-prueba');
  await user.type(screen.getByLabelText('Contraseña', { exact: true }), 'clave-sintetica');
  await user.click(screen.getByRole('button', { name: 'Ingresar' }));
  return user;
}

it('redirige acceso directo al catálogo sin sesión y no consulta datos', () => {
  const request = mockServices();
  mount('/admin/carreras');
  expect(screen.getByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
  expect(screen.getByLabelText('Ubicación de prueba')).toHaveTextContent('/login');
  expect(request).not.toHaveBeenCalled();
});

it('permite panel → carreras → asignaturas → registro → volver al panel con JWT', async () => {
  const request = mockServices();
  mount();
  const user = await login();
  await user.click(await screen.findByRole('link', { name: 'Gestionar carreras y asignaturas' }));
  expect(await screen.findByRole('heading', { name: 'Gestión de carreras' })).toBeInTheDocument();
  expect(screen.getByLabelText('Ubicación de prueba')).toHaveTextContent('/admin/carreras');
  await screen.findByText(carrera.nombre);
  await user.click(screen.getByRole('button', { name: /^Asignaturas$/ }));
  expect(await screen.findByRole('heading', { name: 'Gestión de asignaturas' })).toBeInTheDocument();
  await user.selectOptions(await screen.findByLabelText('Carrera'), carrera.id);
  await screen.findByText('No hay asignaturas registradas en esta carrera.');
  await user.click(screen.getByRole('button', { name: 'Nueva asignatura' }));
  await user.type(screen.getByLabelText('Código'), asignatura.codigo);
  await user.type(screen.getByLabelText('Nombre'), asignatura.nombre);
  await user.type(screen.getByLabelText('Nivel'), '1');
  await user.type(screen.getByLabelText('Créditos'), '3');
  await user.type(screen.getByLabelText('Horas'), '144');
  await user.click(screen.getByRole('button', { name: 'Guardar asignatura' }));
  expect(await screen.findByText(asignatura.nombre)).toBeInTheDocument();
  const post = request.mock.calls.find(([path]) => path === '/api/asignaturas');
  expect(new Headers(post?.[1].headers).get('Authorization')).toMatch(/^Bearer /);
  expect(JSON.parse(post?.[1].body)).toMatchObject({ carreraId: carrera.id, nivel: 1, creditos: 3, horas: 144 });
  await user.click(screen.getByRole('link', { name: 'Volver al panel' }));
  expect(await screen.findByRole('heading', { name: 'Panel de administración' })).toBeInTheDocument();
});

it('permite cerrar sesión desde catálogo y protege acceso posterior', async () => {
  mockServices();
  mount();
  const user = await login();
  await user.click(await screen.findByRole('link', { name: 'Gestionar carreras y asignaturas' }));
  await screen.findByRole('heading', { name: 'Gestión de carreras' });
  await user.click(screen.getByRole('button', { name: 'Cerrar sesión' }));
  await screen.findByRole('heading', { name: 'Iniciar sesión' });
  await user.click(screen.getByRole('link', { name: 'Abrir catálogo de prueba' }));
  expect(screen.getByLabelText('Ubicación de prueba')).toHaveTextContent('/login');
  expect(screen.queryByRole('heading', { name: 'Gestión de carreras' })).not.toBeInTheDocument();
});

it.each(['Docente', 'Coordinador'])('rechaza acceso directo al catálogo para %s', async role => {
  const request = mockServices(role);
  mount();
  const user = await login();
  await screen.findByRole('heading', { name: 'Acceso restringido' });
  await user.click(screen.getByRole('link', { name: 'Abrir catálogo de prueba' }));
  expect(screen.getByLabelText('Ubicación de prueba')).toHaveTextContent('/admin/carreras');
  expect(screen.getByRole('heading', { name: 'Acceso restringido' })).toBeInTheDocument();
  expect(request).toHaveBeenCalledTimes(1);
});

it('un 401 al abrir catálogo devuelve al login', async () => {
  mockServices('Administrador', true);
  mount();
  const user = await login();
  await user.click(await screen.findByRole('link', { name: 'Gestionar carreras y asignaturas' }));
  expect(await screen.findByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
  expect(screen.getByLabelText('Ubicación de prueba')).toHaveTextContent('/login');
});
