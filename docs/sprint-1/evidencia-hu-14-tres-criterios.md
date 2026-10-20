# HU-14 / Issue #5 — corrección a tres criterios por aclaración académica

Fecha: 2026-10-19. Rama `feature/hu-14-equivalencia`, base publicada `11dbb68`.
Se conservaron los cambios sin commit de la revisión anterior y sus evidencias históricas.
Fuente: nuevas reglas del docente confirmadas por el usuario en esta sesión. Sustituyen las cuatro
dimensiones de la planificación previa; no se presentan como una disposición normativa institucional.

**Estado:** implementación y verificación técnica local realizadas. Revisión cruzada, aceptación de
Shirley y actualización del Issue remoto siguen pendientes. No se realizaron commits, pushes ni merges.

## Inspección y coordinación

Se revisaron AGENTS.md, contexto, criterios, planificación, contrato, código y pruebas. Se consultó
el Issue #5 mediante la integración de GitHub: sigue abierto y describe cuatro dimensiones. No se
editó el Issue ni se enviaron mensajes a colaboradores. La aclaración del usuario es la fuente del
cambio local; queda documentada la discrepancia con el texto remoto.

Antes de editar contratos compartidos se registró la [propuesta concreta de cambio e integración](../arquitectura/cambio-hu-14-tres-criterios.md).
Se actualizan solo las secciones de HU-14 de contexto, planificación, criterios y contrato. No se
modifican reglas, archivos ni criterios de HU-15/HU-16, Auth, Gateway, catálogo o frontend.
No se modifica la solución ni los registros de DI. La única edición de Program.cs es descripción
Swagger del servicio de homologación. No se afirma un acuerdo personal ya recibido de Edwin/Leslie.

## Implementación

- Domain: `CoberturaAcademica` calcula la fórmula confirmada y deduplica elementos receptores por
  categoría. `MotorEquivalencia` exige los tres criterios individualmente. Profundidad independiente
  desaparece. Se conserva la razón de horas/créditos con tope 100 %, sin redondeo previo.
- Application: recibe varios orígenes, constancia explícita de aprobación, inventarios de contenidos
  y resultados, referencias de planes y correspondencias con evidencia por ambos extremos.
  Rechaza identificadores inexistentes en inventarios conocidos, duplicados e inputs inválidos.
  La ausencia de inventarios/evidencia/planes/aprobación no permite recomendar homologación.
- Horas y créditos se calculan para un origen con cantidades positivas y unidad/sistema explícitamente
  iguales. Para varios orígenes o unidades/sistemas no comparables quedan indeterminados; no se
  suman cantidades ni se inventan conversiones.
- API: misma ruta y JWT, contrato incompatible nuevo. Responde tres porcentajes, estado explícito,
  justificación, información faltante y contenidos/resultados receptores sin cobertura acreditada.
  Se rechazan campos desconocidos/antiguos con 400, incluyendo porcentajes o umbral del cliente.
- Infrastructure: conserva el adaptador del umbral configurable y su validación al arrancar.
  No se añade persistencia, EF, bibliotecas, IA ni referencias al catálogo.

Clean Architecture se conserva: Domain sin dependencias técnicas; Application solo Domain;
Infrastructure implementa el puerto de configuración; Api contiene HTTP y composición. Las pruebas
existentes de referencias de ensamblados siguen aprobadas. Edwin puede agregar módulos hermanos
independientes; si consume equivalencias deberá usar los DTO nuevos y coordinar archivos compartidos.

## TDD y resultados reales

Se escribieron primero pruebas y firmas pendientes de implementación. Resultados iniciales:

| Suite | Fase fallida | Causa |
|---|---|---|
| Domain | 27 fallidas, 13 aprobadas | fórmula nueva y decisión de tres criterios sin implementar; razones existentes conservadas |
| Application | 26 fallidas, 0 aprobadas | caso de uso nuevo sin implementar |
| API | 17 fallidas, 14 aprobadas | mapeo del contrato nuevo sin implementar; seguridad/esquemas ya comprobables |

Después de implementar se ejecutó restore, build Release y toda la suite .NET:

| Suite | Aprobadas | Fallidas / omitidas |
|---|---:|---:|
| AuthService.Tests | 43 | 0 / 0 |
| Auth.Application.Tests | 13 | 0 / 0 |
| Gateway.Tests | 59 | 0 / 0 |
| Homologacion.Domain.Tests | 40 | 0 / 0 |
| Homologacion.Application.Tests | 26 | 0 / 0 |
| Homologacion.Api.Tests | 31 | 0 / 0 |
| **Total .NET** | **212** | **0 / 0** |

Compilación: **0 errores, 0 advertencias**. TestServer emite sus advertencias sobre el límite HTTP
de cuerpo, característica que no implementa; no se presenta ese límite como verificado por TestServer.

Frontend sin cambios: `npm run build` (incluye chequeo de tipos), `npm test`: **43 pruebas aprobadas**.
Además se ejecutaron las **2 pruebas Playwright/Chromium existentes** contra el frontend Docker real:
login, errores de credenciales, protección, recarga, logout y vista móvil. Ambas aprobadas.
Advertencia de color de Node (`NO_COLOR`/`FORCE_COLOR`), sin fallo.

La salida se conserva en [verificacion.txt](evidencias/hu-14-tres-criterios/verificacion.txt).
TRX en `/tmp/hu14-tres-results` y resultados/screenhots Chromium en `/tmp/hu14-tres-playwright-results`,
fuera de Git. No se declara ejecutado CI remoto ni verificados planes reales.

## Matriz de criterios de aceptación técnicos

| Criterio | Resultado real |
|---|---|
| 8 contenidos + 4 resultados de 10 + 5 | 12/15 × 100 = 80 %, pruebas Domain/Application/API aprobadas |
| 7 contenidos + 4 resultados de 10 + 5 | 11/15 × 100 < 80 %, NoHomologable con datos completos |
| Igual peso por elemento, no promedio 50/50 | 1 contenido cubierto de 1 y 0 resultados de 9 produce 10 %, no 50 % |
| Varios orígenes sin doble conteo | Unión deduplicada por categoría, incluso cuando ambos orígenes cubren un mismo receptor |
| IDs inexistentes | Rechazo explícito; se prueban origen, contenido origen/receptor y resultado origen/receptor |
| Información ausente/total receptor cero | InformacionIncompleta, sin recomendación ni división por cero |
| Tres criterios al 80 % | Homologable en Domain, Application, API y Gateway YARP |
| Un criterio < umbral | NoHomologable en cada posición, sin compensación; también 79,9 % |
| Umbral configurable 85 % | 82 % en cada posición es insuficiente; cambio externo a 85 verificado por API |
| Horas/créditos origen >= receptora | 100 %, nunca superior; cantidades extremas no desbordan |
| Evidencia/plan/aprobación obligatoria ausente | Incompleto; no basta con aportar porcentajes del cliente |
| Varias fuentes / unidades incompatibles | Académica calculable, horas/créditos null, no sumas ni conversiones inferidas |
| JWT y roles | 401 para token ausente/inválido, 403 para rol ajeno, tres roles permitidos, parámetros criptográficos conservados |

## Integración Docker efectivamente ejecutada

Proyecto temporal `hu14-tres-20261019` con SQL Server, Auth, Gateway, Homologación y frontend existentes.
Puertos de prueba loopback: Gateway 15840, Auth 15841, frontend 15843; motor sin puerto público.
Credenciales y clave sintéticas se generaron en archivo privado fuera de Git. No se consultó el catálogo.

El smoke actualizado obtuvo JWT mediante login real por Gateway y evaluó la fixture sintética:
compatibilidad académica **80 %**, horaria **80 %**, créditos **80 %**, umbral **80 %**,
`estado: Homologable`, `homologable: true`, contenidos pendientes C9/C10 y resultado pendiente R5.
Añadió un segundo origen con cobertura repetida: académica permaneció en 80 %, horas y créditos
fueron null, estado InformacionIncompleta y sin recomendación. También verificó 401 y 400.

- [Resultado del smoke](evidencias/hu-14-tres-criterios/compose-smoke.txt).
- [Estado de contenedores](evidencias/hu-14-tres-criterios/compose-estado.txt).

Después de ejecutar las pruebas se retiró únicamente ese proyecto y su volumen de prueba, y se eliminó
el archivo temporal de secretos. No se tocaron datos o contenedores de otros proyectos.

## Cambio incompatible y trabajo pendiente

La guía de [migración del contrato](../../src/homologacion-service/README.md) detalla los reemplazos:
`origen` pasa a `origenes[]`, temas se separan en contenidos/resultados, se exige evidencia, desaparecen
contenido/profundidad independientes y aparecen tres compatibilidades, estado y listas pendientes.
La ruta se conserva, sin compatibilidad silenciosa con solicitudes anteriores. El contrato local y
criterios de HU-14 se actualizaron; las casillas de aceptación de la PO no se marcan por cuenta del agente.

Decisiones académicas pendientes:

1. Cómo agregar **horas y créditos de varios orígenes** y evitar atribuciones dobles en esas cantidades.
   Hasta disponer de esa regla, múltiples fuentes solo completan el criterio académico.
2. Comparabilidad/conversión de **unidades horarias y sistemas de créditos diferentes**: factores,
   fuentes y condiciones. No se introduce una conversión universal ni un sistema oficial inventado.
3. Validar con los planes analíticos reales autorizados las correspondencias y sus ubicaciones de
   evidencia. La validación actual es estructural: no autentica el documento ni juzga semántica con IA.

Ya no está pendiente una fórmula de profundidad: fue sustituida expresamente por la fórmula combinada.
La integración posterior con el catálogo necesita acordar identificadores, versiones y procedencia de
las constancias/evidencias, sin consultas directas a bases ajenas. Pendientes de entrega: revisión
cruzada de Edwin, aceptación de Shirley, actualización coordinada del Issue remoto y CI remoto tras
una futura autorización de publicación. Esta tarea no realiza commits, pushes ni merges.
