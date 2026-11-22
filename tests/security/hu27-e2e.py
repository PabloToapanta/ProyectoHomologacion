"""Arnés aislado de T66. --fixture-only verifica la semilla, no acredita CA2."""

import argparse
import base64
from http.client import RemoteDisconnected
import json
import os
from pathlib import Path
import secrets
import subprocess
import tempfile
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[2]
SPEC = ROOT / "frontend/e2e/hu27-flujo-completo.spec.ts"


def checked_run(args, *, environment, secrets_to_hide, timeout=120):
    result = subprocess.run(args, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=timeout)
    if result.returncode:
        diagnostic = (result.stdout + result.stderr)[-12000:]
        for value in secrets_to_hide:
            diagnostic = diagnostic.replace(value, "[oculto]")
        raise RuntimeError(f"Comando falló ({result.returncode}): {diagnostic}")
    return result.stdout


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fixture-only", action="store_true", help="Verifica solo entorno y semilla; no ejecuta CA2")
    args = parser.parse_args()
    if not args.fixture_only and not SPEC.exists():
        parser.error("Falta frontend/e2e/hu27-flujo-completo.spec.ts; T66 completo sigue pendiente")

    project = "hu27-e2e-" + secrets.token_hex(5)
    admin_password = "Aa1!" + secrets.token_hex(20)
    actor_password = "Aa1!" + secrets.token_hex(20)
    settings = {
        "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "AUTH_JWT_ISSUER": "hu27-isolated", "AUTH_JWT_AUDIENCE": "hu27-isolated-api",
        "AUTH_JWT_KEY": secrets.token_hex(32),
        "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
        "APP_SECRET_KEY": secrets.token_urlsafe(32),
        "AUTH_ADMIN_USERNAME": "admin.hu27", "AUTH_ADMIN_PASSWORD": admin_password,
        "CATALOGO_DATABASE": "Hu27Catalogo",
        "AUTH_SERVICE_URL": "http://auth-service:8080/",
        "CATALOGO_SERVICE_URL": "http://catalogo-api:8080/",
        "HOMOLOGACION_SERVICE_URL": "http://homologacion-service:8080/",
        "GATEWAY_HTTP_PORT": "0", "FRONTEND_HTTP_PORT": "0",
        "ANALISIS_INTERNO_HABILITADO": "true",
        "OLLAMA_URL": "http://ollama-simulado:11434/", "OLLAMA_MODELO": "qwen3:4b",
    }
    environment = {**os.environ, **settings}
    private = [settings[key] for key in ("AUTH_SQL_PASSWORD", "AUTH_JWT_KEY", "AUTH_JWT_ENCRYPTION_KEY", "APP_SECRET_KEY", "AUTH_ADMIN_PASSWORD")]
    private.append(actor_password)
    with tempfile.TemporaryDirectory(prefix="hu27-") as temporary:
        env_file = Path(temporary) / "compose.env"
        env_file.write_text("\n".join(f"{key}={value}" for key, value in settings.items()) + "\n", encoding="utf-8")
        env_file.chmod(0o600)
        overlay = Path(temporary) / "compose.json"
        overlay.write_text(json.dumps({"services": {
            "ollama-simulado": {"image": "python:3.12-alpine", "command": ["python", "/fixture.py"],
                "volumes": [{"type": "bind", "source": str(ROOT / "tests/security/hu18-ollama-fake.py"),
                             "target": "/fixture.py", "read_only": True}]},
            "homologacion-service": {"depends_on": {"ollama-simulado": {"condition": "service_started"}}},
        }}), encoding="utf-8")
        compose = ["docker", "compose", "--env-file", str(env_file), "-p", project,
                   "-f", "compose.sprint1.yaml", "-f", "compose.hu10.yaml", "-f", str(overlay)]

        def run(*parts, timeout=120):
            return checked_run(compose + list(parts), environment=environment, secrets_to_hide=private, timeout=timeout)

        try:
            config = json.loads(run("config", "--format", "json"))
            assert config["services"]["homologacion-service"]["environment"]["AnalisisInterno__OllamaUrl"] == settings["OLLAMA_URL"]
            print("Construyendo y levantando proyecto Docker aislado...", flush=True)
            run("build", "auth-service", "catalogo-api", "homologacion-service", "solicitudes-service", "gateway", "frontend", timeout=900)
            run("up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "gateway", "frontend", timeout=300)
            base = "http://" + run("port", "gateway", "8080").strip()
            frontend = "http://" + run("port", "frontend", "8080").strip()

            def request(path, body=None, token=None, method=None):
                headers = {"Content-Type": "application/json"}
                if token:
                    headers["Authorization"] = "Bearer " + token
                payload = None if body is None else json.dumps(body).encode()
                try:
                    with urlopen(Request(base + path, data=payload, headers=headers, method=method), timeout=15) as response:
                        raw = response.read().decode()
                        return response.status, json.loads(raw) if raw else None
                except HTTPError as error:
                    return error.code, None

            deadline = time.monotonic() + 120
            while True:
                try:
                    status, login = request("/api/auth/login", {"nombreUsuario": "admin.hu27", "password": admin_password})
                    if status == 200:
                        break
                except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                    pass
                if time.monotonic() > deadline:
                    raise RuntimeError("Gateway/Auth no estuvo disponible")
                time.sleep(1)
            admin = login["accessToken"]
            assert len(admin.split(".")) == 5, "Se esperaba JWE"

            def post(path, body, token=admin):
                status, value = request(path, body, token)
                if status != 201:
                    raise RuntimeError(f"Semilla {path}: HTTP {status}; se esperaba 201")
                return value

            actors = [("coord.hu27", "Coordinador"), ("doc1.hu27", "Docente"), ("doc2.hu27", "Docente")]
            for name, role in actors:
                post("/api/usuarios", {"nombreUsuario": name, "rol": role, "contrasena": actor_password})
                assert request("/api/auth/login", {"nombreUsuario": name, "password": actor_password})[0] == 200

            subjects, units = [], []
            for suffix in ("O", "R"):
                career = post("/api/carreras", {"nombre": "HU27 sintética " + suffix, "facultad": "FISEI", "vigencia": True})
                subject = post("/api/asignaturas", {"carreraId": career["id"], "codigo": "HU27-" + suffix,
                    "nombre": "Asignatura sintética " + suffix, "nivel": 1, "creditos": 3, "horas": 100})
                unit = post(f'/api/asignaturas/{subject["id"]}/unidades', {"numero": 1, "nombre": "Unidad sintética",
                    "horas": 100, "resultadoAprendizaje": "Resultado sintético", "contenidos": ["Contenido sintético"]})
                status, _ = request(f'/api/asignaturas/{subject["id"]}/perfil-comparacion',
                    {"planAnalitico": "plan-sintetico-" + suffix, "unidadHoras": "hora-reloj",
                     "sistemaCreditos": "sistema-sintetico", "esSintetico": True}, admin, "PUT")
                assert status == 200, f"Perfil sintético: HTTP {status}"
                subjects.append((career["id"], subject["id"]))
                units.append(unit)
            def link(a, b):
                return {"elementoOrigen": a, "elementoReceptor": b,
                        "ubicacionOrigen": "plan-O/U1", "ubicacionReceptora": "plan-R/U1"}
            revision = {"contenidos": [link(units[0]["contenidos"][0]["id"], units[1]["contenidos"][0]["id"])],
                        "resultados": [link(units[0]["id"], units[1]["id"])]}
            status, _ = request(f"/api/asignaturas/{subjects[0][1]}/correspondencias/{subjects[1][1]}", revision, admin, "PUT")
            assert status == 200, f"Correspondencia revisada: HTTP {status}"
            with urlopen(frontend, timeout=15) as response:
                assert response.status == 200 and 'id="root"' in response.read().decode()
            print("PASS: usuarios, catálogo, correspondencia revisada y frontend sintéticos", flush=True)

            if args.fixture_only:
                print("Fixture verificado; CA2 y T66 completo siguen pendientes.", flush=True)
                return
            playwright_env = {**environment,
                "E2E_BASE_URL": frontend,
                "E2E_ADMIN_USERNAME": settings["AUTH_ADMIN_USERNAME"], "E2E_ADMIN_PASSWORD": admin_password,
                "E2E_COORD_USERNAME": "coord.hu27", "E2E_COORD_PASSWORD": actor_password,
                "E2E_DOC1_USERNAME": "doc1.hu27", "E2E_DOC1_PASSWORD": actor_password,
                "E2E_DOC2_USERNAME": "doc2.hu27", "E2E_DOC2_PASSWORD": actor_password,
                "E2E_CARRERA_ORIGEN_ID": subjects[0][0], "E2E_CARRERA_RECEPTORA_ID": subjects[1][0],
                "E2E_ASIGNATURA_ORIGEN_ID": subjects[0][1], "E2E_ASIGNATURA_RECEPTORA_ID": subjects[1][1],
                "E2E_RUN_ID": project,
            }
            checked_run(["npm", "--prefix", "frontend", "run", "test:e2e", "--", "hu27-flujo-completo.spec.ts",
                         "--trace", "off", "--screenshot", "only-on-failure"],
                environment=playwright_env, secrets_to_hide=private, timeout=600)
            print("PASS: T66 Playwright completo", flush=True)
        finally:
            cleanup = subprocess.run(compose + ["down", "--volumes", "--remove-orphans"],
                cwd=ROOT, env=environment, capture_output=True, timeout=180)
            if cleanup.returncode:
                print(f"Limpieza Docker pendiente: {project}", flush=True)


if __name__ == "__main__":
    main()

