import { Brand } from '../../shared/ui/Brand';
import { useAuth } from '../auth';
import { Link } from 'react-router';

export function AdminPanel() {
  const { session, logout } = useAuth();
  return <div className="workspace">
    <header className="workspace-header"><Brand /><button className="app-button app-button--secondary" onClick={logout}>Cerrar sesión <span aria-hidden="true">↗</span></button></header>
    <main className="workspace-main">
      <p className="eyebrow">INICIO / ADMINISTRACIÓN</p>
      <div className="workspace-heading"><div><h1>Panel de administración</h1><p>Bienvenido al Sistema de Homologación FISEI-UTA.</p></div><span className="session-badge"><span />Sesión activa</span></div>
      <section className="welcome-card" aria-labelledby="welcome-title">
        <div className="welcome-icon" aria-hidden="true">✓</div>
        <div><p className="eyebrow">ACCESO CONFIRMADO</p><h2 id="welcome-title">Sesión iniciada correctamente</h2><p>Has ingresado con tu cuenta de administrador.</p></div>
        <dl className="session-details"><div><dt>Usuario</dt><dd>{session?.name}</dd></div><div><dt>Perfil</dt><dd>{session?.role}</dd></div></dl>
      </section>
      <p><Link to="/admin/carreras" className="app-button app-button--primary" style={{ textDecoration: 'none' }}>Gestionar carreras y asignaturas</Link></p>
    </main>
    <footer className="workspace-footer">FISEI · Universidad Técnica de Ambato</footer>
  </div>;
}
