import { Link } from 'react-router';
import { WorkspaceLayout } from '../../shared/ui/WorkspaceLayout';

export function AdminPanel() {
  return <WorkspaceLayout>
    <p className="eyebrow">INICIO / PANEL</p>
    <div className="workspace-heading"><div>
      <h1>Panel de administración</h1>
      <p>Bienvenido al Sistema de Homologación FISEI-UTA.</p>
    </div><span className="session-badge"><span />Sesión activa</span></div>
    <section aria-labelledby="catalog-summary">
      <h2 id="catalog-summary">Catálogo académico</h2>
      <p className="section-description">Organiza la información académica y consulta las herramientas de homologación.</p>
      <div className="module-grid">
        <article className="module-card"><span className="module-icon" aria-hidden="true">▤</span>
          <h3>Carreras y asignaturas</h3><p>Consulta las carreras de la FISEI, su malla y los programas de sus asignaturas.</p>
          <Link to="/admin/carreras" className="app-button app-button--primary">Gestionar carreras y asignaturas</Link>
        </article>
        <article className="module-card"><span className="module-icon" aria-hidden="true">✓</span>
          <h3>Validaciones académicas</h3><p>Evalúa equivalencias, aprobación, antigüedad y requisitos de cambio de carrera.</p>
          <Link to="/validaciones" className="app-button app-button--secondary">Abrir validaciones</Link>
        </article>
      </div>
    </section>
    <aside className="academic-note"><strong>Apoyo al análisis académico</strong><p>Los resultados orientan la revisión. La decisión académica final corresponde a las autoridades competentes.</p></aside>
  </WorkspaceLayout>;
}
