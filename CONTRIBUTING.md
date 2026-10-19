# Guía de contribución

## Roles durante el Sprint 1
- **Pablo:** Scrum Master y desarrollo de autenticación / homologación.
- **Edwin:** desarrollo del catálogo académico.
- **Shirley:** Product Owner, priorización y aceptación.
- **Leslie:** documentación, seguimiento y pruebas de aceptación.

## GitFlow
- `main`: releases estables.
- `develop`: integración de historias terminadas.
- `feature/hu-XX-descripcion`: trabajo de una historia de usuario.
- `feature/documentacion-sprint-1` o `docs/...`: documentos si corresponde.
- `release/0.1.0`: estabilización de la entrega del Sprint 1.
- `hotfix/...`: correcciones urgentes posteriores a una release.

## Flujo de una historia
1. Tomar un Issue y verificar los criterios de aceptación.
2. Crear una rama desde `develop`.
3. Implementar un conjunto pequeño de cambios y pruebas.
4. Crear commits Conventional Commits con descripción **en español**.
5. Abrir un Pull Request a `develop` enlazando el Issue sin cerrarlo prematuramente.
6. Solicitar revisión cruzada, comprobar pruebas y fusionar únicamente con aceptación.
7. Shirley verifica el comportamiento y cierra el Issue, registrando la evidencia.

## Conventional Commits en español
Formato: `tipo(alcance): descripción breve en imperativo o infinitivo`.

Ejemplos:
- `feat(auth): implementar autenticación con JWT`
- `feat(catalogo): registrar asignaturas por carrera`
- `fix(homologacion): corregir comparación en el límite del umbral`
- `test(reglas): cubrir la antigüedad máxima de diez años`
- `docs(scrum): actualizar planificación detallada del sprint`
- `ci(github): ejecutar pruebas en solicitudes de integración`
- `chore(repo): configurar exclusiones de archivos locales`

## Revisión
- Nadie hace push directo a `main`; no se fusionan PR con fallos conocidos sin documentarlos.
- Una revisión debe verificar historias, tests, errores, seguridad y contratos API.
- Los trabajos generados con asistencia de IA requieren lectura y validación humana.
- No cerrar Issues por el simple hecho de haber creado un commit.

## Seguridad
El repositorio es público. No subir claves, contraseñas, tokens, datos privados de estudiantes ni documentación académica confidencial. Usar datos sintéticos para pruebas.
