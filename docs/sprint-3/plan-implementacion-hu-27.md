# HU-27 — Plan de implementación en pareja

**Spec:** [HU-27](hu-27-e2e-flujo-completo-ci.md) · **Issue:** [#65](https://github.com/PabloToapanta/ProyectoHomologacion/issues/65) · **Estado al 22/11:** HU-32/33 integradas en `develop`; T65 entregada en PR #94; T66/T67 ejecutadas en verde en PR #95. Revisión cruzada y aceptación PO pendientes.

## Orden de trabajo

1. Shirley y Edwin completan T65 a partir de las HU realmente entregadas. Pablo y Leslie señalan el nombre y resultado de cada prueba que aporten, sin marcar criterios de otras historias como aceptados.
2. Pablo prepara el arnés T66: Compose aislado, identidades y datos sintéticos, doble de Ollama, salidas saneadas y limpieza. Prueba primero una falla controlada del arnés y su propagación de estado no cero.
3. El caso Playwright T66 recorrió la interfaz de Coordinación y dos docentes sobre Docker Compose; verificó análisis, las dos revisiones, informe, remisión, finalización y persistencia tras autenticarse de nuevo.
4. El workflow T67 corrió en verde con jobs de integración .NET/SQL y E2E Chromium/Docker. Leslie revisa el recorrido/CI y Edwin hace la revisión cruzada.
5. Shirley confirma la matriz T65 y acepta CA1–CA4 después de la revisión cruzada. El merge requiere autorización separada; la protección de `develop` se configura en GitHub por la administración del repositorio.

## Propiedad inicial de archivos

| Pablo | Leslie | Compartidos: acordar antes de editar |
| --- | --- | --- |
| `tests/security/hu27-*.py` o arnés equivalente; `.github/workflows/hu27-e2e.yml`; evidencia técnica | `frontend/e2e/hu27-flujo-completo.spec.ts`; selectores y aserciones de interfaz | `frontend/playwright.config.ts`, Compose, scripts de semilla, páginas React y contratos HU-32/33 |

Se reutilizan `frontend/e2e`, los smokes aislados y `tests/security/hu18-ollama-fake.py` cuando sus contratos lo permitan. No se copian credenciales ni se modifica el flujo de negocio para facilitar la prueba. El E2E definitivo debe ejecutarse sobre las implementaciones integradas de HU-32/33, no sobre rutas inventadas en esta rama.

## Comprobación y entrega

### Contrato del arnés T66 para Leslie

`python3 tests/security/hu27-e2e.py --fixture-only` comprueba Docker, Gateway, frontend, usuarios y catálogo sintéticos. El comando sin bandera ejecutará únicamente `frontend/e2e/hu27-flujo-completo.spec.ts` y devolverá su código de fallo; hasta que exista ese archivo, falla antes de levantar Docker. Cada ejecución usa proyecto Compose y claves nuevas y elimina sus volúmenes al terminar.

El caso Playwright recibe `E2E_BASE_URL`, usuarios/contraseñas sintéticas de Coordinación y dos docentes, carreras, asignaturas y `E2E_RUN_ID`. El arnés crea solo identidades, carreras, asignaturas, perfiles y correspondencia revisada. El caso crea la solicitud, asigna docentes, analiza, registra ambos acuerdos, genera y remite el informe y finaliza con Coordinación. El doble de Ollama reutiliza `tests/security/hu18-ollama-fake.py` dentro de la red Compose. El test runner genera captura solo al fallar; no se conserva trace porque puede incluir credenciales de autorización.

`--fixture-only` solo verifica infraestructura y semilla; no acredita CA2. La última ejecución completa de CI acreditó el recorrido en la rama del PR #95.

- T65: revisión de cobertura HU → CA → prueba por Shirley/Edwin; cada hueco tiene dueño y estado.
- T66: Playwright en Chromium con capturas/trace al fallar, estado final persistido y evidencia de remisión previa; repetición local aislada sin recursos residuales.
- T67: jobs de GitHub Actions visibles en la Draft PR, con integración SQL y E2E; fallo del E2E bloquea el check. La protección obligatoria de `develop` se configura por la administración del repositorio si aún no existe.
- Antes de compartir la rama, correr revisión de Markdown, enlaces locales y estado Git. Commits pequeños en español; no publicar ni hacer merge sin la autorización correspondiente.

