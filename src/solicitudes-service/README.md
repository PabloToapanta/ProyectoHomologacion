# Solicitudes — HU-10 / Issue #38

Registro manual por el Coordinador de los trámites recibidos mediante Quipux. Cada número identifica una única solicitud, según confirmación de Edwin del 28/10/2026. No conecta con la API de Quipux ni habilita acceso al estudiante.

## Contrato

Todas las rutas exigen JWT válido con rol `Coordinador` tanto para crear como para consultar.

| Método | Ruta | Resultado |
| --- | --- | --- |
| POST | `/api/solicitudes` | 201 y Location al detalle |
| GET | `/api/solicitudes?pagina=1` | Hasta 50 solicitudes, más recientes primero |
| GET | `/api/solicitudes/{id}` | Detalle o 404 |
| GET | `/api/solicitudes/carreras` | Referencias id/nombre de carreras FISEI vigentes |

Cuerpo de ejemplo **sintético**; sustituir los UUID por dos carreras distintas del catálogo:

```json
{
  "numeroQuipux": "TRAMITE-SINTETICO-001",
  "identificacionEstudiante": "EST-SINTETICO",
  "nombreEstudiante": "Estudiante de prueba",
  "tipoCambio": "Interno",
  "carreraOrigenId": "11111111-1111-1111-1111-111111111111",
  "institucionOrigen": null,
  "carreraOrigenExterna": null,
  "carreraReceptoraId": "22222222-2222-2222-2222-222222222222"
}
```

Para `Externo`, enviar `carreraOrigenId: null`, `institucionOrigen` y `carreraOrigenExterna` obligatorios. Representa otra universidad del Ecuador según SC-01, sin crear carreras externas en el catálogo FISEI. La identificación del estudiante no impone una validación de cédula ecuatoriana: el requisito no establece un documento único universal.

El número Quipux admite hasta 100 caracteres y se normaliza con Unicode FormKC, Trim y mayúsculas para la unicidad. Se conserva su forma de presentación sin espacios externos. Campos obligatorios, caracteres de control y referencias inválidas devuelven 400; duplicado 409; sin sesión 401; rol incorrecto 403; catálogo no disponible 503. El índice único protege también contra envíos concurrentes. Un mismo estudiante puede tener otros trámites con números diferentes; no se inventa una restricción adicional.

Estado inicial `Registrada`; fecha UTC del servidor; ID generado por servidor. Sin edición, eliminación, asignaciones docentes ni resultados en esta historia. La consulta docente queda para HU-30.

## Arquitectura e integración

Cuatro capas; Domain y Application no dependen de HTTP ni EF. Infrastructure implementa almacenamiento y el cliente del catálogo. La base lógica exclusiva es `HomologacionSolicitudes`, aunque comparta instancia física SQL. No consulta directamente la base de Catálogo ni Auth.

`GET /api/carreras-solicitudes` es una consulta nueva y mínima del catálogo para Coordinador: solo id/nombre de FISEI vigentes. No cambia permisos de edición del catálogo. El servicio reenvía el JWT del coordinador al catálogo con timeout de 10 s y sin redirecciones de HTTP; no guarda ni registra ese token.

Esta rama conserva el contrato JWT vigente de develop. Pablo/Leslie deben incluir Solicitudes y su llamada al catálogo al integrar HU-35 (cifrado) y HU-36 (clave interna); esas historias no se declaran terminadas aquí. Propuesta de integración: `docs/sprint-2/propuesta-contrato-hu-10.md`.

## Ejecutar

Requiere una cuenta de prueba con rol Coordinador preparada por el responsable de Auth, además de dos carreras FISEI vigentes para cambio interno. La cuenta admin de demostración no recibe estos permisos automáticamente. No incluye administración de usuarios HU-02 ni credenciales nuevas en Git.

Desde la raíz, con las variables existentes en `.env`:

```powershell
docker compose --env-file .env -f compose.sprint1.yaml -f compose.hu10.yaml up -d --build
```

El complemento incorpora el servicio y su base mediante migración al arrancar, y añade su destino al Gateway. No publica un puerto interno de Solicitudes ni modifica los cambios locales de puertos existentes en los Compose del usuario. La interfaz está en `/solicitudes`; el login del Coordinador lleva allí. El Swagger del servicio se expone en `/swagger` solo en Development; puede consultarse desde la red interna de Compose, o ejecutar localmente el servicio para usar la UI Swagger, sin abrir puertos internos permanentes.

Para ejecución local configurar `ConnectionStrings__Solicitudes`, `Catalogo__BaseUrl`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` y opcionalmente `Database__ApplyMigrations=true`, después:

```powershell
dotnet run --project src/solicitudes-service/Solicitudes.Api
```

## Pruebas

```powershell
dotnet test src/solicitudes-service/Solicitudes.sln -c Release
```

La API se prueba con SQL relacional SQLite aislado y catálogo simulado; valida creación/lectura entre clientes, duplicado normalizado, campos/referencias inválidos, externo, caída del catálogo y permisos. La prueba SQL Server aplica la migración, realiza inserciones concurrentes y comprueba el índice único y lectura desde otro contexto. Requiere `HU10_TEST_SQL` con conexión a un servidor **de pruebas** con permiso para crear/eliminar una base temporal `Hu10Tests_<uuid>`; nunca usa la base de negocio. Sin variable queda explícitamente omitida. El workflow `solicitudes-service.yml` configura SQL Server efímero para ejecutarla en CI. Ese workflow no se ha ejecutado en GitHub mientras la rama no esté publicada.

La suite frontend incluye formulario/modal, duplicados sin pérdida de datos, detalle, origen externo y cancelación; las regresiones prueban el nuevo destino del Coordinador y conservan la denegación de acceso al catálogo de administración.

Estado de verificaciones y pendientes: `docs/sprint-2/evidencia-hu-10.md`.
