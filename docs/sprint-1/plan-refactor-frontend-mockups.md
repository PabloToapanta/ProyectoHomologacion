# Refactorización del frontend según mockups

Fecha: 21/10/2026. Rama: `feature/hu-01-refactor-mockups` desde `develop`.
Petición: revisar el proyecto y empezar a adaptar el frontend al ZIP local
`Docs/Homologación FISEI.zip`.

## Diagnóstico y fuentes revisadas

Se inspeccionaron visualmente las dos imágenes: `Login (HU-01).png` y
`New Wireframe 1.png`, además del borrador de informe en Docs, los criterios,
contexto, planificación y contratos del repositorio. No se verificaron en esta
intervención todos los DOCX/PDF del directorio. El borrador del informe conserva
pendientes históricos: no se usa como prueba del estado actual del código.

El código contiene autenticación, catálogo, unidades, equivalencia y validaciones.
La primera diferencia es de composición: login dividido frente a formulario central;
panel con enlaces frente a cabecera, menú lateral y área de trabajo. El catálogo ya
tiene operaciones, estados y pruebas que deben preservarse al adaptar su presentación.
Los marcos de navegador dibujados en el wireframe no forman parte de la aplicación.
El ZIP es un wireframe sin especificación de colores: se conserva la paleta existente.

## Plan por entregas

| Etapa | Issue / criterios preservados | Cambios | Estado |
|---|---|---|---|
| 1. Login y panel | [#1 HU-01](https://github.com/PabloToapanta/ProyectoHomologacion/issues/1): login válido, error, rutas protegidas | Login centrado con símbolo académico, alerta antes del formulario; estructura reutilizable de cabecera, navegación lateral, sesión y contenido; adaptación móvil | Implementado localmente; ver verificación al final |
| 2. Carreras | [#2 HU-04](https://github.com/PabloToapanta/ProyectoHomologacion/issues/2): crear/listar, FISEI fija, duplicados y eliminación restringida | Integrar estructura común, buscador, tabla compacta y formulario modal accesible; errores de duplicado y eliminación como en mockups | Propuesta para coordinar con Leslie |
| 3. Malla y unidades | [#3 HU-05](https://github.com/PabloToapanta/ProyectoHomologacion/issues/3), [#4 HU-06](https://github.com/PabloToapanta/ProyectoHomologacion/issues/4): datos positivos, orden por nivel, contenidos y resultados | Navegación carrera → malla, filtro de nivel y alta contextual; mantener panel de unidades | Planificado; coordinar con Leslie y Edwin |
| 4. Consistencia y cierre | [#5](https://github.com/PabloToapanta/ProyectoHomologacion/issues/5), [#6](https://github.com/PabloToapanta/ProyectoHomologacion/issues/6), [#7](https://github.com/PabloToapanta/ProyectoHomologacion/issues/7) y #1–4 | Aplicar estructura a validaciones, verificar regresión completa y recorrido integrado con Docker; revisión cruzada y aceptación de Shirley | Pendiente |

## Contrato de presentación propuesto para Leslie y Edwin

`shared/ui/WorkspaceLayout.tsx` recibe `children`, muestra nombre/rol reales de la
sesión, navega a rutas existentes y cierra sesión con la API pública de HU-01.
En esta etapa solo lo consume el panel de Pablo. Los componentes y estilos del catálogo,
las validaciones, App y los servicios permanecen sin modificaciones.
Antes de intervenir en esos módulos, coordinar esta propuesta de cambio conforme
al punto 10 de AGENTS.md. Esta propuesta local no supone revisión ni acuerdo del equipo.

Para carreras/asignaturas, conservar DTO, request autenticado, manejo de 401,
validaciones y datos. Resolver navegación a asignaturas con una URL explícita o estado
controlado antes de añadir su entrada lateral independiente; hoy ambas viven en una ruta.
Los modales deberán permitir teclado, Escape, foco inicial y retorno de foco al cerrar.

## Diferencias deliberadas frente al dibujo

- Se conserva «Usuario»: Auth recibe `nombreUsuario`; correo exige un cambio de contrato.
- No se añade «Recordarme»: la sesión vigente es solo en memoria, sin refresh token.
- Las cifras del dibujo son ilustrativas: no se presentan como datos del sistema.
  Las tarjetas iniciales son accesos funcionales; un resumen numérico requiere consultas
  reales y estados de carga/error, sin confundir error con cero registros.
- Usuarios/roles, docentes, coordinadores, carga masiva, parámetros y nueva solicitud
  no se habilitan por aparecer en el mockup; siguen fuera del alcance vigente.
- Se conserva FISEI fija pese al selector de facultad histórico del wireframe.
- No se inventan edición/eliminación de asignaturas ni endpoints que no existan.

## Verificación

Antes de cambios: `npm test -- --reporter=dot`, 11 archivos y 89 pruebas aprobadas.
Después de cambios: `npm run build` (TypeScript + Vite) correcto y
`npm test -- --reporter=dot`: 11 archivos, 89 pruebas aprobadas. `git diff --check`
sin errores. Tras el último ajuste CSS se repitieron build y revisión de navegador.

Chromium/Playwright con API de login simulada y datos sintéticos: login, error 401,
panel y cierre de sesión en 1365×900 y 390×844; sin errores JavaScript. Se comprobó
centrado del formulario y ausencia de desbordamiento horizontal en login y panel.
Se inspeccionaron capturas de escritorio y móvil; se corrigió una interferencia de
la hoja anterior sobre el centrado. Capturas temporales: `/tmp/fisei-*-login.png`,
`/tmp/fisei-*-error.png`, `/tmp/fisei-*-panel.png` (no versionadas).
No se ejecutó el recorrido integrado contra Gateway/Auth reales ni Docker Compose
en esta intervención. No se modificaron reglas de negocio ni validaciones de dominio.
No implica aceptación funcional de la PO, revisión cruzada ni cumplimiento de toda la DoD.
