# Gestión de carreras — HU-04 / T5 / Issue #2

`CarrerasPage` recibe la función `request` de autenticación como propiedad. Incluye listado, formulario de alta/edición, modalidad opcional, vigencia, errores, reintentos y confirmación de eliminación. Reutiliza las clases compartidas `workspace-main`, `auth-input`, `app-button` y los colores existentes; sus estilos adicionales están en `catalogo.css`.

`CatalogoRoute` es el adaptador listo para que Pablo lo monte bajo la ruta protegida de Administrador. No se ha modificado App.tsx, el panel, autenticación, arranque ni configuración común. La pantalla aún no es accesible desde la navegación de la aplicación: integración propuesta en `docs/arquitectura/propuesta-integracion-hu-04.md`, pendiente de acuerdo con Pablo.

La interfaz pública de autenticación está en `features/auth/index.ts`: `useAuth().request` adjunta el JWT al Gateway y cierra sesión ante HTTP 401. El cliente del catálogo consume exclusivamente `/api/carreras` y gestiona ProblemDetails/ValidationProblemDetails. No usa fetch directo ni lee el token.

## Verificación

Desde `frontend`, con Node 24 y las dependencias fijadas por Pablo:

```powershell
npm ci
npm test
npm run build
```

Las pruebas de componentes usan Testing Library y solicitudes simuladas; no sustituyen el recorrido integrado con SQL Server/Gateway. No se han añadido dependencias ni una segunda aplicación React. Las decisiones propuestas sobre UUID, JSON y permisos están detalladas en el documento de integración. HU-05 y HU-06 permanecen fuera de este módulo por ahora.
