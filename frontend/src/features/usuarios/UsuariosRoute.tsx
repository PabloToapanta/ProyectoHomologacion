import { useAuth } from '../auth';
import { WorkspaceLayout } from '../../shared/ui/WorkspaceLayout';
import { UsuariosPage } from './UsuariosPage';

export function UsuariosRoute() {
  const { request } = useAuth();
  return <WorkspaceLayout><UsuariosPage request={request} /></WorkspaceLayout>;
}
