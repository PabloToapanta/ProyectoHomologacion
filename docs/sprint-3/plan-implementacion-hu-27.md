# HU-27 — Plan de implementación en pareja

**Spec:** [HU-27](hu-27-e2e-flujo-completo-ci.md) · **Issue:** [#65](https://github.com/PabloToapanta/ProyectoHomologacion/issues/65) · **Estado:** propuesta para Draft PR; no se inició T65–T67.

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

- T65: revisión de cobertura HU → CA → prueba por Shirley/Edwin; cada hueco tiene dueño y estado.
- T66: Playwright en Chromium con capturas/trace al fallar, estado final persistido y evidencia de remisión previa; repetición local aislada sin recursos residuales.
- T67: jobs de GitHub Actions visibles en la Draft PR, con integración SQL y E2E; fallo del E2E bloquea el check. La protección obligatoria de `develop` se configura por la administración del repositorio si aún no existe.
- Antes de compartir la rama, correr revisión de Markdown, enlaces locales y estado Git. Commits pequeños en español; no publicar ni hacer merge sin la autorización correspondiente.
