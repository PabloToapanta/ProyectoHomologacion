import {useAuth} from '../auth';
import {WorkspaceLayout} from '../../shared/ui/WorkspaceLayout';
import {SolicitudesPage} from './SolicitudesPage';
import {SolicitudesDocentePage} from './SolicitudesDocentePage';
import './solicitudes.css';
export function SolicitudesRoute(){const {request}=useAuth();return <WorkspaceLayout><SolicitudesPage request={request}/></WorkspaceLayout>;}
export function SolicitudesDocenteRoute(){const {request}=useAuth();return <WorkspaceLayout><SolicitudesDocentePage request={request}/></WorkspaceLayout>;}
