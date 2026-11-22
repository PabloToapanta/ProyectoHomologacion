import { useAuth } from '../auth';
import { WorkspaceLayout } from '../../shared/ui/WorkspaceLayout';
import { ConfiguracionPage } from './ConfiguracionPage';
import './configuracion.css';

export function ConfiguracionRoute() {
  const { request } = useAuth();
  return <WorkspaceLayout><ConfiguracionPage request={request} /></WorkspaceLayout>;
}
