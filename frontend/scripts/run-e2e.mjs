// Carga credenciales temporales sin colocarlas en argumentos ni archivos del repositorio.
import { readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';

if (!process.argv[2]) throw new Error('Uso: node scripts/run-e2e.mjs /ruta/privada/stack.env');
const settings = Object.fromEntries(readFileSync(process.argv[2], 'utf8').split(/\r?\n/)
  .filter((line) => line && !line.startsWith('#'))
  .map((line) => { const at = line.indexOf('='); return [line.slice(0, at), line.slice(at + 1)]; }));
if (!settings.AUTH_ADMIN_USERNAME || !settings.AUTH_ADMIN_PASSWORD) throw new Error('El archivo debe definir AUTH_ADMIN_USERNAME y AUTH_ADMIN_PASSWORD.');
const result = spawnSync('npm', ['run', 'test:e2e'], {
  stdio: 'inherit',
  env: {
    ...process.env,
    E2E_ADMIN_USERNAME: settings.AUTH_ADMIN_USERNAME,
    E2E_ADMIN_PASSWORD: settings.AUTH_ADMIN_PASSWORD,
    E2E_BASE_URL: process.env.E2E_BASE_URL || `http://127.0.0.1:${settings.FRONTEND_HTTP_PORT || '5173'}`,
  },
});
if (result.error) throw result.error;
process.exit(result.status ?? 1);
