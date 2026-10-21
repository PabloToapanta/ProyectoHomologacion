# Pantallas HU-14 / HU-15 / HU-16

Solicitud de Edwin: formularios de las historias del Sprint 1 que solo tienen API.
Rama feature/hu-14-15-16-interfaz desde origin/develop, independiente del PR #28.
Cambio compartido propuesto: añadir /validaciones a App.tsx y acceso desde el panel.
No cambia Auth, Gateway, motor ni DTO. Se usa sesión existente y sus tres roles.
HU-14 compara un par mediante inventarios y correspondencias humanas explícitas,
referencias y ubicaciones de evidencia. Sin similitud automática ni IA. La cobertura
desde múltiples orígenes sigue disponible en API, fuera del formulario de un par.
HU-15/HU-16 son evaluaciones independientes; la cantidad de homologables es declarada
y no se presenta como obtenida automáticamente de HU-14. No se crean solicitudes.
La UI distingue sugerencias académicas de decisiones oficiales. No almacena datos.
Revisión de integración con Pablo antes del merge, aceptación por Shirley.
