# Catálogo — HU-04/HU-05, Issues #2/#3

`CarrerasPage` recibe la función `request` de autenticación como propiedad. Incluye listado, formulario de alta/edición, modalidad opcional, vigencia, errores, reintentos y confirmación de eliminación. Reutiliza las clases compartidas `workspace-main`, `auth-input`, `app-button` y los colores existentes; sus estilos adicionales están en `catalogo.css`.

Con autorización expresa de Leslie, `CatalogoRoute` está montado en `/admin/carreras` bajo la protección existente de Administrador en App.tsx. El panel ofrece «Gestionar carreras y asignaturas» y el catálogo permite volver al panel o cerrar sesión. No se modificaron la lógica de autenticación, el arranque ni la configuración común. La navegación de React está integrada localmente; la conexión real de Gateway/SQL Server sigue pendiente de verificación.

`AsignaturasPage` recibe el mismo `request`, carga carreras, permite seleccionar una, consultar su listado por nivel y registrar asignaturas. Incluye errores, reintentos, cancelación de solicitudes anteriores y bloqueo durante el guardado. `CatalogoRoute` ofrece los botones Carreras/Asignaturas dentro del módulo; no necesita una segunda aplicación React.

La interfaz pública de autenticación está en `features/auth/index.ts`: `useAuth().request` adjunta el JWT al Gateway y cierra sesión ante HTTP 401. Los clientes consumen `/api/carreras`, `/api/carreras/{id}/asignaturas` y `/api/asignaturas`, gestionando ProblemDetails/ValidationProblemDetails. No usan fetch directo ni leen el token.

## Verificación

Desde `frontend`, con Node 24 y las dependencias fijadas por Pablo:

```powershell
npm ci
npm test
npm run build
```

Las pruebas de componentes usan Testing Library y solicitudes simuladas; no sustituyen el recorrido integrado con SQL Server/Gateway. No se han añadido dependencias ni una segunda aplicación React. Las decisiones sobre UUID, JSON y permisos están detalladas en los documentos de integración de HU-04/HU-05. HU-06 permanece fuera de este módulo.
