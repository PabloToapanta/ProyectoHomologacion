# Módulo reservado para Leslie

Esta carpeta se prepara durante HU-01 / T3 y no contiene funcionalidad de HU-04, HU-05 o HU-06.

Leslie puede crear aquí sus componentes, modelos, acceso a datos y estilos del catálogo. Mantener sus reglas y componentes dentro de `features/catalogo`; evitar dependencias de `features/admin` o de detalles internos de autenticación.

La interfaz disponible de autenticación está en `features/auth/index.ts`: `useAuth()` entrega `session`, `logout()` y `request('/api/...', init)`. `request` adjunta el JWT al Gateway y cierra la sesión ante HTTP 401; devuelve la respuesta para que cada funcionalidad gestione su contrato y errores. Solo acepta rutas relativas `/api/`.

## Propuesta de integración pendiente de acuerdo

No se ha contactado a Leslie ni se afirma un acuerdo previo. No existían archivos de frontend en esta rama al comenzar T3. La base nueva pertenece al trabajo de Pablo para HU-01.

Antes de integrar el catálogo, acordar con Leslie cualquier modificación a los puntos compartidos `src/app/App.tsx`, `src/main.tsx`, `src/shared/`, `package.json`, el lockfile y las configuraciones Vite/TypeScript. Pablo mantiene el login y su API pública; Leslie mantiene esta funcionalidad. La integración de rutas se revisa conjuntamente cuando el módulo esté listo.

No están definidos ni modificados por T3 los contratos de datos académicos, identificadores o permisos por operación del catálogo. Consultar `docs/arquitectura/decisiones-y-contratos.md` y acordarlos antes de conectar HU-04/05/06.
