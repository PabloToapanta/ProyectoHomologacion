import type { ReactNode } from 'react';
import { Link, NavLink, useLocation } from 'react-router';
import { useAuth } from '../../features/auth';
import { AcademicIcon } from './AcademicIcon';
import '../mockups.css';

/** Estructura reutilizable; solo enlaza módulos existentes. */
export function WorkspaceLayout({ children }: { children: ReactNode }) {
  const { session, logout } = useAuth();
  const location = useLocation();
  const asignaturas = new URLSearchParams(location.search).get('seccion') === 'asignaturas';
  const catalogo = location.pathname === '/admin/carreras';
  return <div className="academic-workspace">
    <a className="skip-link" href="#workspace-content">Saltar al contenido</a>
    <header className="academic-header">
      <div className="academic-brand"><AcademicIcon /><span>Sistema de Homologación<strong>FISEI-UTA</strong></span></div>
      <div className="academic-account"><span>{session?.role}: <strong>{session?.name}</strong></span>
        <button type="button" className="app-button app-button--secondary" onClick={logout}>Cerrar sesión</button>
      </div>
    </header>
    <aside className="academic-sidebar">
      <p className="eyebrow">ESPACIO ACADÉMICO</p>
      <nav aria-label="Navegación principal">
        {session?.role === 'Administrador' && <><NavLink to="/panel" end><span aria-hidden="true">⌂</span>Panel</NavLink>
        <Link to="/admin/carreras" className={catalogo && !asignaturas ? 'active' : undefined} aria-current={catalogo && !asignaturas ? 'page' : undefined}><span aria-hidden="true">▤</span>Carreras</Link>
        <Link to="/admin/carreras?seccion=asignaturas" className={catalogo && asignaturas ? 'active' : undefined} aria-current={catalogo && asignaturas ? 'page' : undefined}><span aria-hidden="true">▥</span>Asignaturas</Link></>}
        <NavLink to="/validaciones"><span aria-hidden="true">✓</span>Validaciones académicas</NavLink>
      </nav>
      <p className="sidebar-caption">Facultad de Ingeniería en Sistemas, Electrónica e Industrial</p>
    </aside>
    <main id="workspace-content" className="academic-content" tabIndex={-1}>{children}</main>
    <footer className="academic-footer">Universidad Técnica de Ambato · FISEI</footer>
  </div>;
}
