import { useAuth } from '../auth';
import { WorkspaceLayout } from '../../shared/ui/WorkspaceLayout';
import { ConsultaHomologablesPage } from './ConsultaHomologablesPage';

export function ConsultaHomologablesRoute() {
  const { request } = useAuth();
  return <WorkspaceLayout><ConsultaHomologablesPage request={request} /></WorkspaceLayout>;
}
