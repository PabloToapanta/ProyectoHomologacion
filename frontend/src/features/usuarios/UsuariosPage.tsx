import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { usuariosApi } from './usuarios-api';
import type { RolGestionable, Usuario, UsuariosRequest } from './usuarios-api';
import './usuarios.css';

type Accion = { tipo: 'rol' | 'contrasena'; usuario: Usuario } | null;
const mensaje = (error: unknown) => error instanceof Error ? error.message : 'No se pudo completar la operación.';

export function UsuariosPage({ request }: { request: UsuariosRequest }) {
  const api = useMemo(() => usuariosApi(request), [request]);
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [cargando, setCargando] = useState(true);
  const [errorCarga, setErrorCarga] = useState('');
  const [intento, setIntento] = useState(0);
  const [nuevo, setNuevo] = useState(false);
  const [nombre, setNombre] = useState('');
  const [rol, setRol] = useState<RolGestionable>('Docente');
  const [contrasena, setContrasena] = useState('');
  const [accion, setAccion] = useState<Accion>(null);
  const [rolNuevo, setRolNuevo] = useState<RolGestionable>('Docente');
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState('');
  const [aviso, setAviso] = useState('');

  useEffect(() => {
    const controller = new AbortController();
    setCargando(true); setErrorCarga('');
    api.listar(controller.signal).then(lista => {
      if (!controller.signal.aborted) setUsuarios(lista);
    }).catch(e => {
      if (!controller.signal.aborted) setErrorCarga(mensaje(e));
    }).finally(() => {
      if (!controller.signal.aborted) setCargando(false);
    });
    return () => controller.abort();
  }, [api, intento]);

  function limpiarFormulario() {
    setNombre(''); setRol('Docente'); setContrasena(''); setNuevo(false); setAccion(null);
  }

  async function crear(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (ocupado) return;
    if (!nombre.trim() || nombre.length > 256 || contrasena.length < 12 || contrasena.length > 1024) {
      setError('Revisa el nombre de usuario y la contraseña (12 a 1024 caracteres).');
      setContrasena('');
      return;
    }
    setOcupado(true); setError(''); setAviso('');
    try {
      await api.crear({ nombreUsuario: nombre.trim(), rol, contrasena });
      limpiarFormulario();
      setAviso('Usuario creado. Recuerda entregar la contraseña cara a cara.');
      setIntento(valor => valor + 1);
    } catch (e) { setError(mensaje(e)); setContrasena(''); }
    finally { setOcupado(false); }
  }

  async function cambiarEstado(usuario: Usuario) {
    if (ocupado) return;
    setOcupado(true); setError(''); setAviso('');
    try {
      await api.cambiarEstado(usuario.id, !usuario.activo);
      setAviso(`Usuario ${usuario.activo ? 'desactivado' : 'activado'} correctamente.`);
      setIntento(valor => valor + 1);
    } catch (e) { setError(mensaje(e)); }
    finally { setOcupado(false); }
  }

  function abrirAccion(tipo: 'rol' | 'contrasena', usuario: Usuario) {
    setNuevo(false); setContrasena(''); setError(''); setAviso('');
    setRolNuevo(usuario.rol); setAccion({ tipo, usuario });
  }

  async function guardarAccion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!accion || ocupado) return;
    if (accion.tipo === 'contrasena' && (contrasena.length < 12 || contrasena.length > 1024)) {
      setError('La contraseña debe tener entre 12 y 1024 caracteres.'); setContrasena(''); return;
    }
    setOcupado(true); setError(''); setAviso('');
    try {
      if (accion.tipo === 'rol') await api.cambiarRol(accion.usuario.id, rolNuevo);
      else await api.cambiarContrasena(accion.usuario.id, contrasena);
      setAviso(accion.tipo === 'rol' ? 'Rol actualizado correctamente.' : 'Contraseña actualizada. Recuerda entregarla cara a cara.');
      setAccion(null); setContrasena('');
      setIntento(valor => valor + 1);
    } catch (e) { setError(mensaje(e)); setContrasena(''); }
    finally { setOcupado(false); }
  }

  return <div className="usuarios">
    <p className="eyebrow">ADMINISTRACIÓN / USUARIOS</p>
    <div className="workspace-heading"><div><h1>Gestión de usuarios y roles</h1>
      <p>Administra cuentas Docente y Coordinador.</p></div>
      <button type="button" className="app-button app-button--primary" disabled={ocupado} onClick={() => {
        limpiarFormulario(); setError(''); setAviso(''); setNuevo(true);
      }}>Nuevo usuario</button></div>
    {aviso && <p role="status" className="usuarios-aviso">{aviso}</p>}
    {error && <p role="alert" className="auth-error">{error}</p>}

    {nuevo && <section className="usuarios-card" aria-label="Crear usuario">
      <h2>Nuevo usuario</h2>
      <form onSubmit={crear} autoComplete="off"><fieldset disabled={ocupado} className="usuarios-fields">
        <label className="auth-label">Nombre de usuario<input className="auth-input" value={nombre} maxLength={256} required autoComplete="off"
          onChange={e => setNombre(e.target.value)} /></label>
        <label className="auth-label">Rol<select className="auth-input" value={rol} onChange={e => setRol(e.target.value as RolGestionable)}>
          <option value="Docente">Docente</option><option value="Coordinador">Coordinador</option></select></label>
        <label className="auth-label">Contraseña inicial<input className="auth-input" type="password" value={contrasena}
          minLength={12} maxLength={1024} required autoComplete="new-password" onChange={e => setContrasena(e.target.value)} /></label>
      </fieldset><p>El administrador entrega la contraseña cara a cara al usuario. No se mostrará después de guardar.</p>
      <div className="usuarios-actions"><button className="app-button app-button--primary" disabled={ocupado}>Crear usuario</button>
        <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={limpiarFormulario}>Cancelar</button></div></form>
    </section>}

    {accion && <section className="usuarios-card" aria-label={accion.tipo === 'rol' ? 'Cambiar rol' : 'Cambiar contraseña'}>
      <h2>{accion.tipo === 'rol' ? 'Cambiar rol' : 'Cambiar contraseña'} de {accion.usuario.nombreUsuario}</h2>
      <form onSubmit={guardarAccion} autoComplete="off"><fieldset disabled={ocupado} className="usuarios-fields">
        {accion.tipo === 'rol' ? <label className="auth-label">Nuevo rol<select className="auth-input" value={rolNuevo} onChange={e => setRolNuevo(e.target.value as RolGestionable)}>
          <option value="Docente">Docente</option><option value="Coordinador">Coordinador</option></select></label>
          : <label className="auth-label">Contraseña nueva<input className="auth-input" type="password" value={contrasena}
            minLength={12} maxLength={1024} required autoComplete="new-password" onChange={e => setContrasena(e.target.value)} /></label>}
      </fieldset>{accion.tipo === 'contrasena' && <p>Entrega la contraseña nueva cara a cara al usuario.</p>}
      <div className="usuarios-actions"><button className="app-button app-button--primary" disabled={ocupado}>
        {accion.tipo === 'rol' ? 'Guardar rol' : 'Guardar contraseña'}</button>
        <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={() => { setAccion(null); setContrasena(''); setError(''); }}>Cancelar</button></div></form>
    </section>}

    <section className="usuarios-card" aria-label="Usuarios registrados" aria-busy={cargando}>
      <div className="usuarios-list-heading"><h2>Usuarios registrados ({usuarios.length})</h2>
        <button type="button" className="app-button app-button--secondary" disabled={cargando || ocupado} onClick={() => setIntento(valor => valor + 1)}>Actualizar listado</button></div>
      {cargando ? <p role="status">Cargando usuarios…</p> : errorCarga ? <><p role="alert" className="auth-error">{errorCarga}</p>
        <button type="button" className="app-button app-button--secondary" onClick={() => setIntento(valor => valor + 1)}>Reintentar</button></>
        : usuarios.length === 0 ? <p>No hay usuarios registrados.</p>
        : <div className="usuarios-table-scroll" role="region" aria-label="Tabla de usuarios" tabIndex={0}><table>
          <thead><tr><th scope="col">Usuario</th><th scope="col">Rol</th><th scope="col">Estado</th><th scope="col">Acciones</th></tr></thead>
          <tbody>{usuarios.map(usuario => <tr key={usuario.id}><td>{usuario.nombreUsuario}</td><td>{usuario.rol}</td>
            <td>{usuario.activo ? 'Activo' : 'Inactivo'}</td><td><div className="usuarios-actions">
              <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={() => abrirAccion('rol', usuario)} aria-label={`Cambiar rol de ${usuario.nombreUsuario}`}>Cambiar rol</button>
              <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={() => cambiarEstado(usuario)} aria-label={`${usuario.activo ? 'Desactivar' : 'Activar'} ${usuario.nombreUsuario}`}>{usuario.activo ? 'Desactivar' : 'Activar'}</button>
              <button type="button" className="app-button app-button--secondary" disabled={ocupado} onClick={() => abrirAccion('contrasena', usuario)} aria-label={`Cambiar contraseña de ${usuario.nombreUsuario}`}>Cambiar contraseña</button>
            </div></td></tr>)}</tbody></table></div>}
    </section>
  </div>;
}
