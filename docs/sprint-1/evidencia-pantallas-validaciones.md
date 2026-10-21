# Pantallas de validaciones — HU-14 / HU-15 / HU-16

Issues #5, #6 y #7. Extensión de frontend solicitada por Edwin.
Rama feature/hu-14-15-16-interfaz desde develop aeb111f; independiente de HU-06 / PR #28.
No incorpora ni sustituye los cambios de esa otra rama. Para la demostración completa
se deben revisar e integrar ambos PR en develop, o preparar una integración autorizada.

Ruta /validaciones protegida con sesión; disponible para Administrador, Docente y
Coordinador, con los permisos definitivos validados en la API. Acceso desde el panel
administrador o desde la página de restricción para los otros dos roles.

- HU-15: estado, fecha de aprobación y fecha de evaluación explícita; resultado y motivo.
- HU-16: tipo interno/externo, períodos, homologables y cambios previos; resultado y motivos.
- HU-14: formulario de un par, inventarios, referencias a planes, unidades/sistemas,
  correspondencias humanas con ubicación de evidencia en ambos extremos. No infiere
  equivalencias, no inventa evidencia ni envía porcentajes calculados por el navegador.
  Múltiples orígenes siguen disponibles en API; no se presentan como cubiertos por esta vista.
- Resultados obsoletos se limpian al cambiar datos; se cancelan peticiones al salir
  del formulario. Sesión/JWT existentes, sin almacenamiento nuevo.
- No se implementa flujo de solicitudes, IA ni informe del Sprint 2.

Verificación local: pruebas de interacción con HTTP simulado para antigüedad,
requisitos e información incompleta de equivalencia. TypeScript y Vite.
La comprobación contra los servicios Docker y la aceptación de Shirley siguen pendientes.
No se declara realizada revisión humana ni ejecutado CI remoto por estos cambios.

Corrección solicitada durante prueba manual: fecha de evaluación no posterior a
hoy en Ecuador continental (UTC-5). Pantalla limita calendario y valida al enviar;
API responde 400 aunque se envíe por Swagger. Dominio conserva evaluación pura
por fechas explícitas para pruebas y cálculos históricos. Hoy sigue permitido.
Suite API: 85 pruebas aprobadas, incluidos hoy permitido y mañana rechazado.
