import { Navigate, Route, Routes } from 'react-router';
import { AdminPanel } from '../features/admin/AdminPanel';
import { AuthProvider, ProtectedRoute } from '../features/auth';
import { LoginPage } from '../features/auth/LoginPage';
import { ValidacionesPage } from '../features/validaciones/ValidacionesPage';
import { CatalogoRoute } from '../features/catalogo/CatalogoRoute';

export function App() {
  return <AuthProvider><Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<ProtectedRoute role="Administrador" />}>
      <Route path="/panel" element={<AdminPanel />} />
      <Route path="/admin/carreras" element={<CatalogoRoute />} />
    </Route>
    <Route element={<ProtectedRoute />}><Route path="/validaciones" element={<ValidacionesPage />} /></Route>
    <Route path="*" element={<Navigate to="/panel" replace />} />
  </Routes></AuthProvider>;
}
