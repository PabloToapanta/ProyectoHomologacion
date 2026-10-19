# Guion de demostración del Sprint 1

Duración sugerida: 10–15 minutos. Preparar datos de prueba reproducibles y mostrar solamente funcionalidades reales.

1. **Infraestructura:** ejecutar `docker compose up --build`, verificar servicios levantados y Swagger del Gateway o las APIs.
2. **HU-01:** login del administrador, rechazo de credenciales inválidas y acceso 401 sin token.
3. **HU-04:** registrar carrera; rechazar duplicado; impedir borrado cuando tiene asignaturas.
4. **HU-05:** registrar asignatura; impedir horas y créditos inválidos; mostrar asignaturas por nivel.
5. **HU-06:** cargar unidades, temas y resultados de aprendizaje de Álgebra Lineal; consultarlos desde la interfaz.
6. **HU-14:** comparar asignaturas con umbral del 80 %; demostrar límites 79,9/80 y umbral configurable.
7. **HU-15:** demostrar una asignatura elegible de diez años y otra que necesita validación.
8. **HU-16:** demostrar mínimos cumplidos/no cumplidos para cambios interno y externo.
9. **Calidad:** mostrar xUnit en verde, GitHub Actions y PR revisados si están disponibles.
10. **Scrum + XP:** mostrar Issues y evidencia de aceptación, más impedimentos reales.

## Responsabilidades
- **Pablo:** infraestructura, autenticación, motor de homologación y TDD.
- **Leslie:** APIs y pantallas del catálogo.
- **Shirley:** Product Owner, objetivos y aceptación.
- **Edwin:** análisis de requisitos, datos académicos, evidencias y retroalimentación.

## Preparativos
- [ ] Entorno local funciona con las variables configuradas.
- [ ] No hay datos personales reales ni claves en la demo.
- [ ] Datos de prueba disponibles.
- [ ] Cada historia mostrada tiene evidencias guardadas.
- [ ] Se documentan explícitamente los criterios que no se cumplieron.

La demostración no equivale a una resolución oficial de homologación.
