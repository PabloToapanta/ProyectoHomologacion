# HU-30 — consulta docente de solicitudes y análisis / Issue #41

Rama `feature/hu-30-vista-docente` desde `origin/develop` (`afacebc`, con HU-10, HU-11, HU-29, HU-34 y HU-35 integradas). Commits locales `1e573c3` (integración JWT cifrado) y `2d271cd` (consulta docente). Sin revisión cruzada ni aceptación de la PO.

## Prerrequisito: Solicitudes no aceptaba el token cifrado de HU-35

HU-35 (#49) adaptó Auth, Catálogo, Homologación y el Gateway al JWT firmado y cifrado, pero Solicitudes se había integrado antes (#45) y seguía validando solo la firma: con un login real todas sus rutas respondían 401. Las pruebas no lo detectaban porque fabricaban tokens sin cifrar.

- `Solicitudes.Api/Program.cs`: `Jwt:EncryptionKey` obligatoria (32 bytes en Base64), `TokenDecryptionKey` y rechazo de tokens que no lleguen cifrados con A256KW + A256CBC-HS512, igual que Catálogo.
- `compose.hu10.yaml`: `Jwt__EncryptionKey` desde `AUTH_JWT_ENCRYPTION_KEY`. README del servicio actualizado.
- Pruebas: la fábrica emite tokens cifrados como Auth; nuevos casos para token sin cifrar, con otra clave o alterado (401) y clave inválida (no arranca). Antes de corregir fallaron 36 pruebas de la API.

La corrección se conserva en el commit independiente `1e573c3`, para revisarla como prerrequisito técnico de HU-30 o publicarla por separado antes de la vista docente.

## Contrato HU-30

- `GET /api/solicitudes/asignadas`, rol `Docente`: solicitudes donde el `sub` del token figura entre los dos docentes asignados, recientes primero. El docente no se pasa como parámetro.
- `GET /api/solicitudes/asignadas/{id}`, rol `Docente`: `{ solicitud, analisis }`. **404** si la solicitud no existe o no está asignada a quien consulta, sin distinguir ambos casos para no revelar solicitudes ajenas (PA1 dejaba 403/404 por acordar).
- `analisis`: `{ estado, mensaje, resultado }`. Estados previstos: `Pendiente`, `Completo`, `Incompleto`, `Fallido`. Hoy siempre es `Pendiente` con `resultado: null`: no existe resultado persistido (HU-18/HU-20).
- `resultado` propuesto: `porcentajeAcademico`, `porcentajeHoras`, `porcentajeCreditos`, `sugerencia`, `justificacion`, `evidencias`. **Es una propuesta**: HU-18 y HU-20 deben fijar el contrato definitivo.
- Coordinador y Administrador reciben 403 en estas rutas; el docente sigue sin acceso a las rutas de coordinación (403). Sin rutas de escritura (CA5).
- Gateway: dos rutas GET nuevas en `src/gateway/appsettings.json`.
- Interfaz: ruta `/docente/solicitudes`, enlace «Mis solicitudes» y redirección del Docente tras el login (antes iba a `/panel` y veía «Acceso restringido»). El análisis solo muestra sugerencia y porcentajes con estado `Completo` y resultado; cualquier otro estado se presenta como pendiente, fallido o información incompleta, sin recomendación.

## Criterios

| Criterio | Estado |
| --- | --- |
| CA1 — lista solo las asignadas | Verificado con API, SQL Server y Gateway con login real |
| CA2 — rechazo desde la API | Verificado (404 en no asignadas, 403 en rutas de coordinación) |
| CA3 — porcentajes, sugerencia, justificación y evidencia | **Solo interfaz con datos simulados.** Falta resultado real de HU-18/HU-20 |
| CA4 — pendiente, fallido o incompleto sin éxito ficticio | Pendiente verificado de punta a punta; fallido e incompleto solo en interfaz con datos simulados |
| CA5 — sin revisión ni remisión | Verificado (sin rutas de escritura ni botones de acción) |

## Verificación del 03/11/2026

Entorno: SDK 8.0 en contenedor (el equipo no tiene runtime .NET 8), stack desechable `compose.sprint1.yaml` + `compose.hu10.yaml` con secretos generados y base vacía. Cuentas Coordinador y Docente insertadas por SQL solo en esa base. Datos sintéticos.

1. TDD: 7 pruebas de API HU-30, 2 del Gateway y 1 de interfaz de enrutamiento fallaron antes de implementar; la página nueva no existía.
2. Solicitudes con SQL Server real: 105 aprobadas, ninguna omitida (incluida la consulta por docente traducida a SQL). Gateway: 103 aprobadas.
3. Frontend: `npx vitest run` 120 aprobadas; `npm run build` (incluye typecheck) correcto. `UnidadesPanel.test.tsx` falló una vez por el límite de 5 s en la ejecución completa y pasó en las siguientes y 3 de 3 por separado; no se modificó.
4. Flujos por Gateway con login real y token cifrado: HU-10 21, HU-11 35 y HU-29 21 comprobaciones aprobadas (2 fallos de HU-29 se deben al script, que espera una base sin datos de otros flujos). HU-30: 30 de 30 (CA1, CA2, CA4 pendiente, CA5, 401 sin sesión y con token alterado, 403 para Coordinador/Administrador, Swagger del Gateway).
5. Navegador como `docente.luis`: tras el login abre «Mis solicitudes asignadas», lista solo sus dos solicitudes, el detalle muestra docentes y «Análisis pendiente», sin botones de acción. A 375 px sin desbordamiento. Sin errores de consola ni `fail:` en registros de Solicitudes.

**No verificado:** CI, revisión cruzada, aceptación de la PO y CA3/CA4 con resultados reales.

## Pendientes y observaciones

- El docente no ve nombres de carreras: `/api/solicitudes/carreras` es solo para Coordinador. Decidir si el detalle debe incluirlos.
- Integración propuesta para revisión con Pablo: publicar en `src/gateway/appsettings.Hu34.json` las rutas exactas `/solicitudes` y `/docente/solicitudes` hacia el cluster frontend, con GET/HEAD y política anonymous, igual que `/panel`. La SPA y las API conservan sus controles de sesión y rol. Se añaden pruebas de transporte HTTPS antes del ajuste.
- Se editaron archivos compartidos (`App.tsx`, `WorkspaceLayout.tsx`, `LoginPage.tsx`, Gateway): coordinar con Pablo en la revisión.

## Preparación para PR — 03/11/2026

- Revisión frente a #41 y `origin/develop` (`afacebc`): CA1, CA2 y CA5 implementados; CA3 y los estados reales fallido/incompleto de CA4 permanecen pendientes de HU-18/HU-20. Preparar como PR de avance, sin cerrar la historia.
- Suite de Solicitudes: 105 aprobadas y ninguna omitida con SQL Server temporal real, SDK .NET 8 y copia del código. Base y red eliminadas al finalizar sin tocar las existentes. TRX: `tmp/hu30-review-20261101/hu30-review.trx` (nombre histórico del script, no fecha de ejecución).
- HTTPS: dos pruebas nuevas fallaron con 401 en `/solicitudes` y `/docente/solicitudes`; tras declarar las rutas GET/HEAD del frontend, la suite del Gateway pasó 105 pruebas. La autorización de las API se conserva.
- Frontend: la primera ejecución detectó una aserción que comprobaba la consulta antes del efecto asíncrono. Se corrigió con `waitFor` sobre esa petición; después pasaron las 120 pruebas, TypeScript y compilación Vite.
- Compose: se corrigió la sangría del puerto local en `compose.hu05.yaml`; ambas configuraciones (`compose.hu05.yaml` y Sprint 1 + HU-10) validan con `config --no-interpolate --quiet`. Los cambios de puertos siguen siendo locales y no se incluyen en HU-30.
- Herramientas HU-18: el evaluador sin seguimiento pasó sus 8 casos en modo simulado. Se excluyen los resultados generados mediante su `.gitignore`; no se evaluaron modelos reales ni se incluye este banco en HU-30.
- Total de suites actuales de producto: 330 pruebas aprobadas. No se repitió un recorrido de navegador con todo el stack ni se ejecutó CI en GitHub. La descripción local está preparada en `docs/sprint-2/pr-hu-30.md`; su existencia no implica que se haya publicado un PR.
