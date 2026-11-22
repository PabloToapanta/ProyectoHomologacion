# HU-27 — Plan de implementación en pareja

**Spec:** [HU-27](hu-27-e2e-flujo-completo-ci.md) · **Issue:** [#65](https://github.com/PabloToapanta/ProyectoHomologacion/issues/65) · **Estado:** T66 en curso; el arnés de Pablo se verifica por separado y no acredita CA2.

## Orden de trabajo

1. Shirley y Edwin completan T65 a partir de las HU realmente entregadas. Pablo y Leslie señalan el nombre y resultado de cada prueba que aporten, sin marcar criterios de otras historias como aceptados.
2. Pablo prepara el arnés T66: Compose aislado, identidades y datos sintéticos, doble de Ollama, salidas saneadas y limpieza. Prueba primero una falla controlada del arnés y su propagación de estado no cero.
3. Leslie escribe el caso Playwright T66 sobre el entorno del paso 2. Cada actor inicia sesión por la interfaz y confirma el estado visible; Pablo revisa el tramo análisis/informe y Leslie revisa fixture y selectores. El caso se completa al integrar HU-32/33.
4. Pablo conecta T67 a GitHub Actions: .NET/SQL, Node 24, Chromium con dependencias, Docker Compose y artefactos en fallo. Verifica un run verde y una falla controlada; Leslie revisa que el workflow pruebe la PR y no solo un fixture aislado.
5. Edwin hace revisión cruzada, Shirley valida CA1–CA4 y la Draft PR queda lista para revisión formal. El merge requiere autorización separada.

## Propiedad inicial de archivos

| Pablo | Leslie | Compartidos: acordar antes de editar |
| --- | --- | --- |
| `tests/security/hu27-*.py` o arnés equivalente; `.github/workflows/hu27-e2e.yml`; evidencia técnica | `frontend/e2e/hu27-flujo-completo.spec.ts`; selectores y aserciones de interfaz | `frontend/playwright.config.ts`, Compose, scripts de semilla, páginas React y contratos HU-32/33 |

Se reutilizan `frontend/e2e`, los smokes aislados y `tests/security/hu18-ollama-fake.py` cuando sus contratos lo permitan. No se copian credenciales ni se modifica el flujo de negocio para facilitar la prueba. El E2E definitivo debe ejecutarse sobre las implementaciones integradas de HU-32/33, no sobre rutas inventadas en esta rama.

## Comprobación y entrega

### Contrato del arnés T66 para Leslie

`python3 tests/security/hu27-e2e.py --fixture-only` comprueba Docker, Gateway, frontend, usuarios y catálogo sintéticos. El comando sin bandera ejecutará únicamente `frontend/e2e/hu27-flujo-completo.spec.ts` y devolverá su código de fallo; hasta que exista ese archivo, falla antes de levantar Docker. Cada ejecución usa proyecto Compose y claves nuevas y elimina sus volúmenes al terminar.

El caso Playwright recibe `E2E_BASE_URL`, `E2E_ADMIN_USERNAME/PASSWORD`, `E2E_COORD_USERNAME/PASSWORD`, `E2E_DOC1_USERNAME/PASSWORD`, `E2E_DOC2_USERNAME/PASSWORD`, `E2E_CARRERA_ORIGEN_ID`, `E2E_CARRERA_RECEPTORA_ID`, `E2E_ASIGNATURA_ORIGEN_ID`, `E2E_ASIGNATURA_RECEPTORA_ID` y `E2E_RUN_ID`. El arnés crea solo identidades, carreras, asignaturas, perfiles y correspondencia revisada. Leslie crea la solicitud, asigna docentes, analiza, revisa y verifica el informe/remisión/finalización mediante la interfaz. El doble de Ollama reutiliza `tests/security/hu18-ollama-fake.py` dentro de la red Compose. El arnés solicita trace y captura solo al fallar.

La ejecución completa depende de que HU-32 y HU-33 lleguen a la rama con rutas y estados estables. Hasta entonces, `--fixture-only` es evidencia de infraestructura, no de CA2.

- T65: revisión de cobertura HU → CA → prueba por Shirley/Edwin; cada hueco tiene dueño y estado.
- T66: Playwright en Chromium con capturas/trace al fallar, estado final persistido y evidencia de remisión previa; repetición local aislada sin recursos residuales.
- T67: jobs de GitHub Actions visibles en la Draft PR, con integración SQL y E2E; fallo del E2E bloquea el check. La protección obligatoria de `develop` se configura por la administración del repositorio si aún no existe.
- Antes de compartir la rama, correr revisión de Markdown, enlaces locales y estado Git. Commits pequeños en español; no publicar ni hacer merge sin la autorización correspondiente.
