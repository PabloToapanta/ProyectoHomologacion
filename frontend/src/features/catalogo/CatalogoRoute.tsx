import { useAuth } from '../auth';
import { CarrerasPage } from './CarrerasPage';
import { AsignaturasPage } from './AsignaturasPage';
import { Link, useSearchParams } from 'react-router';
import { WorkspaceLayout } from '../../shared/ui/WorkspaceLayout';

/** La URL conserva la sección del catálogo al avanzar o retroceder. */
export function CatalogoRoute() {
  const { request } = useAuth();
  const [params, setParams] = useSearchParams();
  const seccion = params.get('seccion') === 'asignaturas' ? 'asignaturas' : 'carreras';
  return <WorkspaceLayout>
    <Link to="/panel" className="catalogo-back">Volver al panel</Link>
    <nav className="catalogo-tabs" aria-label="Catálogo académico">
      <button className="app-button app-button--secondary" aria-pressed={seccion === 'carreras'} onClick={() => setParams({})}>Carreras</button>
      <button className="app-button app-button--secondary" aria-pressed={seccion === 'asignaturas'} onClick={() => setParams({ seccion: 'asignaturas' })}>Asignaturas</button>
    </nav>
    {seccion === 'carreras' ? <CarrerasPage request={request} onVerAsignaturas={id => setParams({ seccion: 'asignaturas', carrera: id })} /> : <AsignaturasPage request={request} carreraSeleccionada={params.get('carrera') ?? ''} onSeleccion={id => setParams(id ? { seccion: 'asignaturas', carrera: id } : { seccion: 'asignaturas' })} />}
  </WorkspaceLayout>;
}
