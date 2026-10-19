# Propuesta de coordinación — homologacion-service / Issue #5

Fecha: 2026-10-19. Responsable inicial de composición y equivalencias: Pablo.
Este documento registra la estructura disponible y una **propuesta para acordar con Edwin y Leslie**;
no afirma que se haya celebrado un acuerdo ni modifica sus contratos o archivos.

## Edwin: HU-15 y HU-16

El microservicio ya tiene cuatro proyectos y no necesita un segundo arranque ni otra base.
Agregar posteriormente carpetas por funcionalidad, sin mezclar reglas con `Equivalencias`:

```text
Homologacion.Domain/
  Equivalencias/                  HU-14, Pablo
  Antiguedad/                     futura HU-15, Edwin
  RequisitosCambio/               futura HU-16, Edwin
Homologacion.Application/
  Equivalencias/                  HU-14, Pablo
  Antiguedad/                     futura HU-15, Edwin
  RequisitosCambio/               futura HU-16, Edwin
Homologacion.Api/
  Controllers/EquivalenciasController.cs
  Controllers/AntiguedadController.cs          futuro
  Controllers/RequisitosCambioController.cs   futuro
  Contracts/                     archivos nuevos por funcionalidad
```

Son puntos de extensión documentados; no se crean módulos vacíos, reglas ni endpoints de HU-15/HU-16.
Las rutas futuras `/api/homologaciones/validar-antiguedad` y `/validar-requisitos` ya propuestas
permanecen reservadas y sin implementar. No modificar la respuesta de equivalencia para incorporar
antigüedad o cambio; los casos de uso futuros podrán consumir resultados explícitos, sin tratar
`InformacionIncompleta` como aprobación. `Homologable` es una sugerencia del comparador, no una
validación de aprobación, antigüedad, cambio ni una resolución del Consejo Directivo.

El estado puro se expone en `ResultadoEquivalencia.Estado` y
`EvaluacionEquivalencia.Estado` (`InformacionIncompleta`, `NoHomologable`, `Homologable`).
Los porcentajes ausentes no equivalen a cero. No usar como homologables resultados incompletos.
No es necesario modificar el motor para implementar esas reglas independientes.

## Leslie: integración posterior del catálogo

No se referencia ningún proyecto, entidad, DbContext o base del catálogo. El endpoint recibe
instantáneas con identificador opaco, horas, créditos e identificadores de temas; también recibe
correspondencias explícitas de origen a receptora. El detalle y ejemplo están en
`src/homologacion-service/README.md`. No se agrega un cliente HTTP sin un contrato de catálogo acordado.

Propuesta pendiente de acuerdo:

1. Acordar cómo traducir identificadores y versiones de asignaturas/temas del catálogo a las instantáneas.
2. Acordar la procedencia verificable de las correspondencias. HU-14 cuenta las declaradas;
   no certifica académicamente su corrección ni las genera por similitud de texto.
3. Definir con Coordinación el criterio de profundidad, escala, datos y evidencia necesaria sobre
   resultados de aprendizaje; los niveles por sí solos no tienen una fórmula aprobada.
4. Acordar un contrato de integración nuevo si se necesitan resultados de aprendizaje u otra evidencia.
   Hasta entonces, profundidad es `null` y no hay homologación automática por HTTP.

## Archivos compartidos: coordinar antes de modificarlos

| Archivo o grupo | Responsable inicial / coordinación |
|---|---|
| `Homologacion.sln` | Pablo; registrar proyectos sin reescribir referencias de otros desarrolladores |
| `Homologacion.Api/Program.cs` | Pablo; acordar la incorporación de registros de nuevos casos de uso |
| `Homologacion.Api/appsettings.json` y opciones JWT | Pablo; no variar parámetros compartidos de seguridad unilateralmente |
| Proyectos `.csproj` de homologación | coordinar dependencias; Domain puro y Application solo Domain |
| `Infrastructure/Configuration/UmbralConfigurado.cs` | Pablo; configuración de HU-14, no reutilizar como umbral de otras historias |
| Dockerfile y `compose.hu14.yaml` | Pablo; adaptar solo cuando exista una necesidad técnica real |
| Configuración y rutas del Gateway | Pablo; HU-14 reutiliza su ruta sin editar T2 |
| Contratos compartidos en `decisiones-y-contratos.md` | acuerdo de integración; conservados en este trabajo |
| Modelos/contratos/DB del catálogo | Leslie (HU-04/05), Edwin coordina HU-06; no editados por HU-14 |
| `frontend/src/App.tsx` | Pablo; no modificado en HU-14 |

Trabajar los módulos independientes con sus propias pruebas. Antes de tocar un archivo compartido,
acordar el cambio concreto entre responsables y revisar el diff para conservar trabajo ajeno.
La incorporación del microservicio, pruebas y referencias de HU-14 a la solución se realizó en
`feature/hu-14-equivalencia`, basada en `develop`, sin cambios iniciales ajenos.
No se envió ningún mensaje a colaboradores ni se creó un PR. Los commits y el push de HU-14
se realizan tras la autorización explícita del usuario, posterior a la entrega inicial.
