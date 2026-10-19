import { useAuth } from '../auth';
import { CarrerasPage } from './CarrerasPage';

/** Adaptador para la ruta protegida que Pablo incorporará al arranque compartido. */
export function CatalogoRoute() {
  const { request } = useAuth();
  return <CarrerasPage request={request} />;
}
