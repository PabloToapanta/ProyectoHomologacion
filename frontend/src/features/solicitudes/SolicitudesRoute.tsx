import {useAuth} from '../auth';
import {WorkspaceLayout} from '../../shared/ui/WorkspaceLayout';
import {SolicitudesPage} from './SolicitudesPage';
import './solicitudes.css';
export function SolicitudesRoute(){const {request}=useAuth();return <WorkspaceLayout><SolicitudesPage request={request}/></WorkspaceLayout>;}
