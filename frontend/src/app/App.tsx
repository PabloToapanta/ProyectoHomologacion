import { SolicitudesDocenteRoute, SolicitudesRoute } from '../features/solicitudes/SolicitudesRoute';
import { Navigate, Route, Routes } from 'react-router';
import { AdminPanel } from '../features/admin/AdminPanel';
import { AuthProvider, ProtectedRoute } from '../features/auth';
import { LoginPage } from '../features/auth/LoginPage';
import { ValidacionesPage } from '../features/validaciones/ValidacionesPage';
import { CatalogoRoute } from '../features/catalogo/CatalogoRoute';
import { UsuariosRoute } from '../features/usuarios/UsuariosRoute';
import { ConfiguracionRoute } from '../features/configuracion/ConfiguracionRoute';
import { ConsultaHomologablesRoute } from '../features/consulta/ConsultaHomologablesRoute';
import { NormativasRoute } from '../features/normativas/NormativasRoute';

export function App() {
  return <AuthProvider><Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<ProtectedRoute role="Administrador" />}>
      <Route path="/panel" element={<AdminPanel />} />
      <Route path="/admin/carreras" element={<CatalogoRoute />} />
      <Route path="/admin/usuarios" element={<UsuariosRoute />} />
    </Route>
    <Route element={<ProtectedRoute />}><Route path="/validaciones" element={<ValidacionesPage />} /></Route>
    <Route element={<ProtectedRoute role="Coordinador" />}><Route path="/solicitudes" element={<SolicitudesRoute />} /></Route>
    <Route element={<ProtectedRoute role={['Coordinador', 'Administrador']} />}><Route path="/configuracion" element={<ConfiguracionRoute />} /></Route>
    <Route element={<ProtectedRoute role={['Administrador', 'Coordinador', 'Docente']} />}><Route path="/normativas" element={<NormativasRoute />} /></Route>
    <Route element={<ProtectedRoute role="Docente" />}><Route path="/docente/solicitudes" element={<SolicitudesDocenteRoute />} /></Route>
    <Route element={<ProtectedRoute role={['Docente', 'Coordinador']} />}><Route path="/consulta-homologables" element={<ConsultaHomologablesRoute />} /></Route>
    <Route path="*" element={<Navigate to="/panel" replace />} />
  </Routes></AuthProvider>;
}
