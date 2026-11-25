import { Link, Navigate, Outlet } from 'react-router';
import { Brand } from '../../shared/ui/Brand';
import { useAuth } from './AuthProvider';
import type { Role } from './types';

/** `role` admite un rol o varios (HU-09: Docente y Coordinador). */
export function ProtectedRoute({ role }: { role?: Role | Role[] }) {
  const { session, logout } = useAuth();
  if (!session || session.expiresAt <= Date.now()) return <Navigate to="/login" replace />;
  if (role && !(Array.isArray(role) ? role : [role]).includes(session.role)) {
    return <main className="access-page"><Brand /><section className="access-card">
      <p className="eyebrow">PERMISOS DE ACCESO</p><h1>Acceso restringido</h1>
      <p>Tu cuenta no tiene acceso a esta sección.</p>
      <p><Link to="/validaciones" className="app-button app-button--primary">Ir a validaciones académicas</Link></p>
      <button className="app-button app-button--primary" onClick={logout}>Cerrar sesión</button>
    </section></main>;
  }
  return <Outlet />;
}
