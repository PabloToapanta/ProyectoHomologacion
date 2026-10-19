# Evidencia HU-14 — T9 y T10 — Issue #5

Fecha: 2026-10-19. Desarrollo: Pablo. Rama: `feature/hu-14-equivalencia`, basada en `develop`.
Issue consultado: https://github.com/PabloToapanta/ProyectoHomologacion/issues/5.
Estado: implementación técnica y pruebas locales verificadas; **HU-14 no aceptada ni cerrada**.
T10 verificada. T9 dispone de reglas/decisión y endpoint, con profundidad pendiente de definición académica.

## Contexto revisado y alcance

Se leyeron AGENTS.md, contexto del proyecto, criterios, planificación detallada y decisiones/contratos,
así como el Issue #5 y los proyectos existentes de Auth y Gateway. Git estaba limpio al iniciar.
La base ejecutó 115 pruebas aprobadas antes de cambios.

Se agregaron cuatro proyectos independientes de homologación y tres de pruebas a `Homologacion.sln`.
Se agregaron Dockerfile, overlay Compose, fixture, smoke, workflow y documentación propios de HU-14.
No se modificaron Auth, Gateway, frontend, catálogo ni sus bases de datos o contratos compartidos.
No se implementaron HU-15/HU-16, persistencia, LLM, PDF o historias posteriores.
La implementación inicial se entregó sin commits ni push. El usuario autorizó posteriormente
los commits atómicos y el push de la rama. No se realizaron merges, PR ni mensajes a colaboradores.

## Decisiones verificables

- Dominio puro, sin paquetes ni dependencias técnicas; Application solo Domain.
- Infrastructure implementa el puerto de configuración, con umbral externo validado al arrancar.
- API compone dependencias, valida HTTP, documenta Swagger y comprueba JWT compatible con Auth/YARP.
- Contenido cuenta identificadores receptores distintos cubiertos por correspondencias declaradas;
  es implementación de la propuesta del equipo, no validación semántica de sílabos.
- Horas y créditos usan la receptora como denominador, se limitan a 100 % y no redondean antes de comparar.
- Las cuatro dimensiones se comparan individualmente. Ausencias/valores inválidos nunca autorizan homologación.
- Profundidad no tiene fórmula acordada: se mantiene desconocida; no se acepta un porcentaje del cliente
  como sustituto de un criterio académico.
- La respuesta conserva las claves JSON propuestas. Porcentajes desconocidos son `null`;
  `justificacion` indica «Información incompleta» y `homologable` es `false`.

## TDD y ejecución .NET

Primero se escribieron pruebas y firmas sin implementación. Resultados reales antes de implementar:

| Suite nueva | Fase fallida |
|---|---|
| Dominio | 35 fallidas; reglas todavía `NotImplementedException` |
| Aplicación | 14 fallidas; caso de uso todavía `NotImplementedException` |
| API e integración inicial | 24 fallidas y 2 aprobadas; servidor sin endpoint devuelve 404 |

Después de implementar se ejecutaron `dotnet restore`, `dotnet build` Release y `dotnet test` sobre
la solución completa. Se corrigió la representación inicial de datos nullable de xUnit antes de
registrar la fase fallida de dominio. Resultado final:

| Suite | Aprobadas | Fallidas / omitidas |
|---|---:|---:|
| AuthService.Tests | 43 | 0 / 0 |
| Auth.Application.Tests | 13 | 0 / 0 |
| Gateway.Tests | 59 | 0 / 0 |
| Homologacion.Domain.Tests | 35 | 0 / 0 |
| Homologacion.Application.Tests | 14 | 0 / 0 |
| Homologacion.Api.Tests | 28 | 0 / 0 |
| **Total** | **192** | **0 / 0** |

Compilación: **0 advertencias, 0 errores**. Las 77 pruebas nuevas incluyen los límites pedidos,
correspondencias, validaciones, configuración, contrato JSON, Swagger, dependencias y YARP real
con destinos TestServer. TestServer advierte que no puede aplicar el límite de cuerpo HTTP;
no se presenta esa característica de Kestrel como probada por el servidor en memoria.
No se ejecutó frontend por no haber cambios allí. No se declara ejecutado el nuevo workflow remoto.

Salida conservada: [verificacion.txt](evidencias/hu-14-t9-t10/verificacion.txt).

## Matriz de criterios y límites de la evidencia

| Criterio | Entrada sintética / esperado | Resultado real y alcance |
|---|---|---|
| Origen ≥ receptora en horas y créditos → 100 % | 144/144, 200/144; 3/3, 4/3 | Pruebas de relación aprobadas; integración también devuelve ambas dimensiones a 100 % |
| Una dimensión 79,9 %, umbral 80 % → no homologable | 79,9 en cada una de las cuatro posiciones; las demás 100 | Cuatro pruebas del motor aprobadas; no hay promedio compensatorio |
| Cuatro dimensiones 80 %, umbral 80 % → homologable | (80,80,80,80) | Prueba del motor aprobada; profundidad es un valor sintético proporcionado al dominio, no una fórmula académica verificada |
| Umbral 85 %, una dimensión 82 % → no homologable | 82 en cada una de las cuatro posiciones | Cuatro pruebas del motor aprobadas; 85 % externo también verificado por API y Compose |
| Información incompleta / inválida → sin homologación automática | null; cantidades ≤0; referencias inexistentes; temas nulos/duplicados | Dominio devuelve estado incompleto; API distingue 200 incompleto de 400 inválido; nunca homologa |
| Porcentajes ≤100 % | Origen mayor, valores extremos decimal, cobertura distinta | Pruebas aprobadas, sin desbordamiento ni doble conteo de temas receptores |

Las condiciones numéricas de aceptación se verifican en el núcleo puro. **El caso positivo de HTTP
con cuatro dimensiones calculadas sigue pendiente**: no se define una fórmula de profundidad
por cuenta del desarrollo. No se marca aceptada la historia en los criterios compartidos.

## Integración Docker Compose efectivamente ejecutada

Se construyeron y levantaron Auth + SQL Server, Gateway y Homologación con el overlay HU-14,
proyecto aislado `hu14-t9-t10-20261019`. Puertos locales 15840/15841; motor sin puerto público.
Credenciales y clave JWT sintéticas generadas en archivo privado temporal, fuera de Git.
El catálogo no se levantó ni se consultó; su URL solo fue una configuración de destino no ejercitada.

El smoke `tests/homologacion-service/smoke.py` realizó login real mediante Gateway y utilizó el JWT
emitido por Auth contra el nuevo endpoint. Fixture `examples/evaluar.json`, umbral externo **85 %**.

Resultado real: HTTP 200, contenido 80 %, profundidad null, horas/créditos 100 %, umbral 85 %,
`homologable: false`, `temasSinCobertura: ["R5"]`, justificación de información incompleta.
También comprobó 401 sin JWT, 401 con JWT inválido y 400 con horas negativas.
La suite en memoria comprueba además los tres roles, 403 para rol ajeno, firma, audiencia,
emisor, algoritmo y expiración de JWT.

- [Salida del smoke](evidencias/hu-14-t9-t10/compose-smoke.txt).
- [Estado de contenedores durante la prueba](evidencias/hu-14-t9-t10/compose-estado.txt).

Después de verificar se retiraron únicamente los contenedores, red y volumen creados para esta prueba
con `down --volumes`, y se eliminó el archivo temporal de secretos.

## Pendiente para aceptación e integración del equipo

1. Coordinación de Carrera debe definir el método de profundidad y su evidencia verificable
   sobre resultados/niveles. Confirmar asimismo la fórmula de contenido, procedencia de correspondencias
   y umbral como propuesta del equipo; no se presume normativa oficial.
2. Acordar con Leslie las instantáneas/identificadores y su adaptación al contrato del catálogo real;
   la integración presente usa fixtures sintéticas, no datos oficiales ni sílabos inventados.
3. Acordar con Edwin los archivos compartidos y el agregado independiente de HU-15/HU-16 según
   [propuesta de coordinación](../arquitectura/coordinacion-homologacion-service.md).
4. Tras definir profundidad, agregar sus pruebas antes de implementarla y verificar el resultado
   positivo por HTTP con datos académicos autorizados.
5. Revisión cruzada de Edwin, PR hacia develop, CI remota y aceptación de Shirley pendientes.
   Los commits y el push se autorizaron posteriormente a la entrega inicial; no implican aceptación de HU-14.

Guía completa de contrato, capas, configuración y comandos:
[README del servicio](../../src/homologacion-service/README.md).
