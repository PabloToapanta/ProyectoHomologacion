import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate } from 'react-router';
import { Brand } from '../../shared/ui/Brand';
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

  return <main className="login-page">
    <section className="login-story" aria-label="Sistema de Homologación FISEI-UTA">
      <Brand light />
      <div className="story-content">
        <p className="eyebrow eyebrow--light">UNIVERSIDAD TÉCNICA DE AMBATO</p>
        <h1>Homologación<br />{' '}de asignaturas<span className="story-period">.</span></h1>
        <p className="story-description">Un espacio para acompañar el análisis académico de nuestra facultad.</p>
        <div className="story-rule" /><p className="story-note">Criterio académico.<br />Información organizada.</p>
      </div>
      <p className="story-footer">FISEI · Universidad Técnica de Ambato</p>
      <div className="story-orbit orbit-one" aria-hidden="true" /><div className="story-orbit orbit-two" aria-hidden="true" />
    </section>
    <section className="login-side" aria-labelledby="login-title">
      <span className="login-context">PORTAL ACADÉMICO</span>
      <div className="auth-card">
        <span className="auth-card-icon" aria-hidden="true">↗</span>
        <p className="eyebrow">TE DAMOS LA BIENVENIDA</p>
        <h2 id="login-title">Iniciar sesión</h2>
        <p className="auth-intro">Accede con la cuenta asignada por el administrador.</p>
        <form onSubmit={submit} aria-busy={loading}>
          <label className="auth-label" htmlFor="username">Usuario</label>
          <input id="username" name="username" className="auth-input" value={username}
            onChange={(event) => { setUsername(event.target.value); setError(null); }}
            autoComplete="username" autoCapitalize="none" spellCheck={false} required maxLength={256}
            placeholder="Ingresa tu usuario" disabled={loading} aria-describedby={error ? 'login-error' : undefined} />
          <label className="auth-label password-label" htmlFor="password">Contraseña</label>
          <div className="password-field">
            <input id="password" name="password" className="auth-input" type={showPassword ? 'text' : 'password'}
              value={password} onChange={(event) => { setPassword(event.target.value); setError(null); }}
              autoComplete="current-password" required maxLength={1024} disabled={loading}
              placeholder="Ingresa tu contraseña" aria-describedby={error ? 'login-error' : undefined} />
            <button className="password-toggle" type="button" onClick={() => setShowPassword(!showPassword)}
              aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'} aria-pressed={showPassword} disabled={loading}>
              {showPassword ? 'Ocultar' : 'Mostrar'}
            </button>
          </div>
          {error && <div className="auth-error" id="login-error" role="alert">{error}</div>}
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
