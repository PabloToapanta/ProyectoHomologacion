# HU-34 — avance técnico del acceso HTTPS

**Issue:** [#35](https://github.com/PabloToapanta/ProyectoHomologacion/issues/35)

**Ficha previa:** [PR borrador #46](https://github.com/PabloToapanta/ProyectoHomologacion/pull/46)

**Fecha de comprobación local:** 02/11/2026. Esta evidencia corresponde a la rama local `feature/hu-34-https`; no acredita aceptación de Shirley.

## Contrato elegido

- Un solo origen público: `https://localhost:443`. `http://localhost:80` se usa únicamente para redirigir las solicitudes GET/HEAD a HTTPS. Un POST por HTTP, incluido el login, recibe 403 sin contactar Auth.
- El Gateway expone 80/443 en loopback y enruta la SPA a un frontend interno. Auth, Catálogo y Homologación conservan HTTP dentro de la red de Docker y no publican puertos al host en el overlay HU-34.
- El certificado y su clave se generan con mkcert fuera de Git. HSTS usa `max-age=60` en esta demostración de localhost para evitar una política de desarrollo duradera.

## TDD y resultados ejecutados

| Comprobación | Resultado real |
| --- | --- |
| Primer ciclo: redirección GET, rechazo del login HTTP y HSTS | 3 fallos antes del cambio; 3 aprobadas después. |
| Segundo ciclo: rutas de SPA y API desconocida | 7 fallos antes de configurar las rutas; 7 aprobadas después. |
| Suite completa de `Gateway.Tests` | 78 aprobadas, 0 fallidas, 0 omitidas (`dotnet test tests/Gateway.Tests/Gateway.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false`). |
| Frontend | `npm run build`: TypeScript y Vite correctos. No se cambiaron archivos de React. |
| Overlay Compose | `docker compose ... config -q` correcto. Configuración efectiva: solo Gateway publica `80→8080` y `443→8443`; los demás servicios no publican puertos. |
| Kestrel real con mkcert | En puertos temporales `18080/18443`: GET HTTP 308 con `Location` HTTPS; POST de login HTTP 403 tras la corrección; GET HTTPS 401 con `Strict-Transport-Security: max-age=60`. `curl --cacert rootCA.pem` validó la cadena; no se usó `-k`. |
| Interfaz por Gateway HTTPS | Con frontend Vite Preview temporal: `/login` 200 HTML, recurso `/assets/...js` 200 JavaScript; `/api/desconocida` 401. Gateway y Vite Preview quedaron detenidos después de la comprobación. |
| Publicación .NET | `dotnet publish` Release correcto; `appsettings.Hu34.json` presente en el artefacto. La comprobación del 403 se ejecutó desde el directorio publicado para cargar esa configuración. |
| Docker Compose integrado | Proyecto aislado `hu34-test` levantado con `compose.sprint1.yaml` y `compose.hu34.yaml`; seis contenedores activos y sanos al concluir `up --wait`. `docker compose ps` mostró solo Gateway publicado en `127.0.0.1:80` y `127.0.0.1:443`. |
| Puertos públicos reales | `http://localhost/login` respondió 308 con `Location: https://localhost/login`; POST HTTP a `/api/auth/login` respondió 403; `https://localhost/login` respondió 200 con HSTS. Todas las peticiones HTTPS verificaron la CA mkcert mediante `curl --cacert` o el contexto TLS de Python. |
| Flujo integrado autenticado | POST HTTPS a `/api/auth/login` con usuario sintético devolvió 200 y un token; GET HTTPS a `/api/carreras` con ese token devolvió 200 JSON. No se imprimieron credenciales ni token. |
| Recursos de la SPA | `/login` y los recursos JavaScript y CSS bajo `/assets/` respondieron 200 por HTTPS. El HTML no incluyó referencias `http://`; esta inspección no sustituye una prueba de contenido mixto en navegador. |

## Certificado y configuración privada

Se descargó mkcert 1.4.4 del paquete Ubuntu y se ejecutó desde `/tmp/hu34-mkcert/usr/bin/mkcert`. La CA y el certificado local se generaron en `/tmp/hu34-certs`: `ca/rootCA.pem`, `localhost.pem` y `localhost-key.pem` para `localhost`, `127.0.0.1` y `::1`. La clave privada tiene permisos 600. **No copiar** `rootCA-key.pem` ni `localhost-key.pem` al repositorio.

Para reproducir en otra máquina, instalar mkcert mediante el gestor de paquetes y generar una CA/certificado locales propios. Agregar al archivo privado de entorno `HU34_CERT_DIR` con la ruta absoluta del directorio que contiene `localhost.pem` y `localhost-key.pem`; opcionalmente fijar `HU34_UID` y `HU34_GID` a los valores de `id -u` y `id -g` para que el Gateway pueda leer la clave montada. El overlay se ejecuta junto con `compose.sprint1.yaml`:

```bash
docker compose --env-file .env.sprint1 -p fisei-uta-hu34 \
  -f compose.sprint1.yaml -f compose.hu34.yaml up -d --build --wait
```

La CA local **no quedó instalada** en el almacén de confianza de esta máquina: `mkcert -install` solicitó una contraseña de `sudo` que esta sesión no posee. Para pruebas HTTP se usó `curl --cacert /tmp/hu34-certs/ca/rootCA.pem` y se verificó el certificado. La prueba de navegador con confianza normal sigue pendiente.

## Pendiente antes de cerrar HU-34

- Instalar la CA mkcert en el entorno de prueba con autorización del propietario del equipo y ejecutar el recorrido de navegador sin omitir errores TLS, incluido login real y ausencia de contenido mixto.
- Ejecutar y registrar las pruebas de aceptación PA1/PA2 con Shirley, revisar el PR de código y comprobar CI. Las pruebas técnicas integradas no sustituyen esa aceptación.
