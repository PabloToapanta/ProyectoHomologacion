# HU-36 — Clave interna entre Gateway y servicios

Issue #37. Responsable de este incremento: Leslie. Base actual: `origin/develop` en
`e01767b`, con HU-10 integrada. Propuesta local de integración, abierta antes de editar los puntos
compartidos; pendiente de revisión con Pablo y Edwin. No implica comunicaciones
externas ni aceptación de Shirley.

## Contrato propuesto

- Cabecera reservada: `X-Internal-Key`. Configuración de servidor:
  `InternalAccess:Key`, alimentada por `InternalAccess__Key` y `APP_SECRET_KEY`
  en Compose. Clave independiente de firma/cifrado JWT. Generar al menos 32 bytes
  aleatorios y codificarlos en Base64; no versionar el valor real.
- Validación técnica: 32 a 512 caracteres ASCII imprimibles sin espacios.
  Configuración ausente o inválida impide arrancar, sin incluir el valor en errores.
- Auth, Catálogo, Homologación y Solicitudes rechazan cabeceras ausentes, repetidas o incorrectas
  con 403 ProblemDetails genérico antes de Swagger, autenticación y controladores.
  Comparación de resúmenes SHA-256 con `CryptographicOperations.FixedTimeEquals`.
  La cabecera se retira de la petición antes de continuar o responder.
- Gateway reemplaza cualquier valor aportado por el cliente con su clave privada
  en todas las llamadas YARP. No exige JWT en el login público. En rutas protegidas
  conserva los 401 por JWT ausente/inválido y los 403 por rol insuficiente.
- La clave acredita conocimiento del secreto; no identifica criptográficamente
  al Gateway. Complementar con red privada y ausencia de puertos internos publicados.
  No sustituye HTTPS, JWT ni autorización. No enviarla a React ni variables VITE_.
- `.env.example` incluye solo la variable vacía. Las pruebas usan secretos
  sintéticos; no desactivar seguridad para facilitar pruebas o despliegues.

## Puntos compartidos y dependencias

Se incorporan llamadas explícitas en `Program.cs`: registrar la protección antes
de construir la aplicación y validar la configuración al resolverla inmediatamente
después de `Build`, antes de inicializar bases y atender peticiones. El middleware
se coloca antes de Swagger/autenticación. Las implementaciones se mantienen en archivos nuevos de
la capa HTTP; Domain y Application no dependen de cabeceras ni criptografía HTTP.
No modificar App.tsx, Homologacion.sln ni el algoritmo JWT.

- PR #47 / HU-34: combinar Compose, red privada, healthchecks y orden del pipeline
  HTTPS. Los healthchecks internos necesitan la clave desde servidor, sin imprimirla.
- PR #49 / HU-35: combinar las llamadas de registro en Program.cs y configuración
  privada; conservar íntegramente la validación JWE. Las pruebas sobre esta base
  todavía usan el JWT firmado actual y no acreditan integración con JWE.
- PR #45 / HU-10: integrado en develop. La API de Solicitudes y su cliente HTTP
  hacia Catálogo incorporan el contrato manteniendo JWT/rol. Comprobado con
  Coordinador y Catálogo reales, además de pruebas de rechazo directo.

## Tareas y evidencia

T22: guardas HTTP y validación de arranque. T23: transformación YARP. T24: Compose
de comprobación aislada sin puertos internos. T25: pruebas de rechazo, arranque,
inyección/sobrescritura y permisos, regresión y comprobación integrada.

TDD: primero ejecutar las pruebas HTTP de clave ausente/incorrecta contra los
servicios actuales y registrar el fallo observado; implementar después y repetir.
Los resultados reales y pendientes se registran en `evidencia-hu-36.md`.

## Ejecución local y cambios operativos

Completar `APP_SECRET_KEY` solo en la configuración privada y reiniciar juntos el
Gateway y los servicios que la utilizan. Nunca reutilizar la clave JWT para ello.
Los stacks Compose existentes ahora la requieren explícitamente; Auth, Catálogo
y Homologación ya no publican sus puertos al host; Solicitudes tampoco los publica.
Swagger directo requiere una
llamada desde la red interna con la clave, o el documento público propio del Gateway.
Los scripts de demostración deben apuntar al Gateway, no a los antiguos puertos
5081/5082/5083. No cambiar ni eliminar volúmenes de despliegues existentes.

Para reproducir sin datos del equipo:

```text
python tests/security/hu36-smoke.py
```

El script no lee archivos privados existentes: genera un proyecto Docker aleatorio,
credenciales temporales y una base nueva. Fija destinos internos explícitos para no
heredar direcciones de otro entorno. Comprueba el fallo de arranque, login, guardas,
puertos y ausencia de clave en logs. Al finalizar elimina únicamente su proyecto
y volumen de prueba. Conserva resultados sin secretos en TestResults.

Los adaptadores HTTP de las cuatro API son locales e idénticos salvo su namespace,
para mantener las imágenes y contextos Docker independientes sin añadir un paquete
compartido ni dependencias entre microservicios. Si el equipo evoluciona este
contrato debe actualizar los cuatro; las pruebas comprueban cada servicio.

Aceptación humana pendiente de Shirley. Este documento no declara la historia
terminada ni sustituye la coordinación final de los PR abiertos.

## Ampliación tras integrar HU-10

El 03/11 se comprobó que PR #45 ya está en develop `e01767b`. Esta rama se
actualizó conservando sus cambios y la referencia anterior, sin merge ni commits.
Se amplía la misma guarda HTTP a Solicitudes. Su CatalogoAuthorizationHandler
incorporará la clave del servidor y conservará el Bearer para la consulta de
carreras autorizada al Coordinador. No reenviará el valor de X-Internal-Key del
cliente. Es la integración prevista en el contrato HU-10; no cambia sus DTO,
reglas, esquema, migración ni permisos. Primero se reproducen fallos HTTP y del
handler antes de implementar esta ampliación.
