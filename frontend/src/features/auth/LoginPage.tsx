import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate } from 'react-router';
import { AcademicIcon } from '../../shared/ui/AcademicIcon';
import '../../shared/mockups.css';
import { useAuth } from './AuthProvider';

export function LoginPage() {
  const { session, login } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  if (session && session.expiresAt > Date.now()) return <Navigate to="/panel" replace />;

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (loading) return;
    setError(null);
    setLoading(true);
    try { await login({ nombreUsuario: username.trim(), password }); }
    catch (failure) {
      if (!(failure instanceof DOMException && failure.name === 'AbortError')) {
        setError(failure instanceof Error ? failure.message : 'No se pudo iniciar sesión. Inténtalo de nuevo.');
      }
      setPassword('');
    } finally { setLoading(false); }
  }

  return <main className="login-page mockup-login">
    <section className="login-side" aria-labelledby="login-title">
      <div className="auth-card">
        <div className="login-identity">
          <AcademicIcon />
          <p>Sistema de Homologación<strong>FISEI-UTA</strong></p>
        </div>
        <h1 id="login-title">Iniciar sesión</h1>
        <p className="auth-intro">Accede con tu cuenta institucional asignada.</p>
        {error && <div className="auth-error" id="login-error" role="alert">{error}</div>}
        <form onSubmit={submit} aria-busy={loading}>
          <label className="auth-label" htmlFor="username">Usuario</label>
          <input id="username" name="username" className="auth-input" value={username}
            onChange={(event) => { setUsername(event.target.value); setError(null); }}
            autoComplete="username" autoCapitalize="none" spellCheck={false} required maxLength={256}
            placeholder="Ingresa tu usuario" disabled={loading} aria-invalid={!!error} aria-describedby={error ? 'login-error' : undefined} />
          <label className="auth-label password-label" htmlFor="password">Contraseña</label>
          <div className="password-field">
            <input id="password" name="password" className="auth-input" type={showPassword ? 'text' : 'password'}
              value={password} onChange={(event) => { setPassword(event.target.value); setError(null); }}
              autoComplete="current-password" required maxLength={1024} disabled={loading}
              placeholder="Ingresa tu contraseña" aria-invalid={!!error} aria-describedby={error ? 'login-error' : undefined} />
            <button className="password-toggle" type="button" onClick={() => setShowPassword(!showPassword)}
              aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'} aria-pressed={showPassword} disabled={loading}>
              {showPassword ? 'Ocultar' : 'Mostrar'}
            </button>
          </div>
          <button className="app-button app-button--primary login-submit" type="submit" disabled={loading}>
            {loading ? <><span className="spinner" aria-hidden="true" />Validando…</> : <>Ingresar <span aria-hidden="true">→</span></>}
          </button>
          {loading && <span className="sr-only" role="status">Validando tus credenciales.</span>}
        </form>
        <div className="auth-help"><span aria-hidden="true">ⓘ</span><p>¿Necesitas acceso?<br /><span>Comunícate con el administrador del sistema.</span></p></div>
      </div>
      <p className="login-footer"><span aria-hidden="true">◇</span> Acceso exclusivo para personal autorizado.</p>
    </section>
  </main>;
}
