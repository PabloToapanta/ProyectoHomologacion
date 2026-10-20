import { useAuth } from '../auth';
import { CarrerasPage } from './CarrerasPage';
import { AsignaturasPage } from './AsignaturasPage';
import { useState } from 'react';
import { Link } from 'react-router';
import { Brand } from '../../shared/ui/Brand';

/** Catálogo bajo ProtectedRoute Administrador; reutiliza la sesión pública de HU-01. */
export function CatalogoRoute() {
  const { request, logout } = useAuth();
  const [seccion, setSeccion] = useState<'carreras' | 'asignaturas'>('carreras');
  return <div className="workspace">
    <header className="workspace-header catalogo-header"><Brand /><div className="catalogo-actions">
      <Link to="/panel" className="app-button app-button--secondary" style={{ textDecoration: 'none' }}>Volver al panel</Link>
      <button className="app-button app-button--secondary" onClick={logout}>Cerrar sesión</button>
    </div></header>
    <nav className="catalogo-nav" aria-label="Catálogo académico">
    <button className="app-button app-button--secondary" aria-pressed={seccion === 'carreras'} onClick={() => setSeccion('carreras')}>Carreras</button>
    <button className="app-button app-button--secondary" aria-pressed={seccion === 'asignaturas'} onClick={() => setSeccion('asignaturas')}>Asignaturas</button>
    </nav>{seccion === 'carreras' ? <CarrerasPage request={request} /> : <AsignaturasPage request={request} />}
    <footer className="workspace-footer">FISEI · Universidad Técnica de Ambato</footer>
  </div>;
}
