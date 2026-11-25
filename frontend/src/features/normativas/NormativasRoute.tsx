import { useAuth } from '../auth';
import { WorkspaceLayout } from '../../shared/ui/WorkspaceLayout';
import { NormativasPage } from './NormativasPage';
import './normativas.css';

export function NormativasRoute() {
  const { request, session } = useAuth();
  return <WorkspaceLayout><NormativasPage request={request} rol={session?.role ?? ''} /></WorkspaceLayout>;
}
