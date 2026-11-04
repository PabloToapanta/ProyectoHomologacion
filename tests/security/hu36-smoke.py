"""HU-36: prueba Docker/SQL aislada. No lee .env ni usa volúmenes existentes.

Ejecutar desde la raíz: python tests/security/hu36-smoke.py
Genera secretos efímeros, elimina solo su proyecto hu36-check-<id> al terminar
y escribe un resultado sin secretos en TestResults/hu36-integration/resultado.json.
"""

import json
import base64
from datetime import datetime, timedelta, timezone
import os
from pathlib import Path
import re
import secrets
import subprocess
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


ROOT = Path(__file__).resolve().parents[2]
SERVICES = ["auth-service", "catalogo-api", "homologacion-service", "solicitudes-service"]


def main():
    project = "hu36-check-" + secrets.token_hex(5)
    assert re.fullmatch(r"hu36-check-[0-9a-f]{10}", project)
    output = ROOT / "TestResults" / "hu36-integration"
    output.mkdir(parents=True, exist_ok=True)
    settings = {
        "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "AUTH_JWT_ISSUER": "hu36-isolated",
        "AUTH_JWT_AUDIENCE": "hu36-isolated-api",
        "AUTH_JWT_KEY": secrets.token_hex(32),
        "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
        "APP_SECRET_KEY": secrets.token_urlsafe(32),
        "AUTH_SERVICE_URL": "http://auth-service:8080/",
        "CATALOGO_SERVICE_URL": "http://catalogo-api:8080/",
        "HOMOLOGACION_SERVICE_URL": "http://homologacion-service:8080/",
        "CATALOGO_DATABASE": "Hu36Catalogo",
        "AUTH_ADMIN_USERNAME": "admin.hu36",
        "AUTH_ADMIN_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "GATEWAY_HTTP_PORT": "0",
        "FRONTEND_HTTP_PORT": "0",
    }
    private_env = {**os.environ, **settings}
    env_path = ROOT / (".env." + project)
    env_path.write_text("\n".join(f"{key}={value}" for key, value in settings.items()), encoding="utf-8")
    compose = ["docker", "compose", "--env-file", str(env_path), "-p", project, "-f", "compose.sprint1.yaml", "-f", "compose.hu10.yaml"]
    checks = []

    def run(args, *, timeout=120):
        result = subprocess.run(args, cwd=ROOT, env=private_env, text=True, capture_output=True, timeout=timeout)
        if result.returncode:
            # El diagnóstico se conserva localmente, nunca se imprime configuración privada.
            message = result.stdout + result.stderr
            for value in settings.values():
                message = message.replace(value, "[oculto]")
            (output / "error.log").write_text(message, encoding="utf-8")
            raise RuntimeError("Falló un comando Docker; ver diagnóstico local saneado en TestResults/hu36-integration/error.log")
        return result.stdout

    def check(name, actual, expected):
        assert actual == expected, f"{name}: esperado {expected}, obtenido {actual}"
        checks.append({"caso": name, "resultado": actual})
        print("PASS", name)

    def request(base, path, body=None, token=None, internal=None):
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        if internal:
            headers["X-Internal-Key"] = internal
        req = Request(base + path, data=None if body is None else json.dumps(body).encode(), headers=headers)
        try:
            with urlopen(req, timeout=10) as response:
                text = response.read().decode()
                assert settings["APP_SECRET_KEY"] not in text
                assert "X-Internal-Key" not in response.headers
                return response.status, json.loads(text)
        except HTTPError as error:
            text = error.read().decode()
            assert settings["APP_SECRET_KEY"] not in text
            return error.code, None

    try:
        config = json.loads(run(compose + ["config", "--format", "json"]))
        check("clave interna ausente de frontend", settings["APP_SECRET_KEY"] in json.dumps(config["services"]["frontend"]), False)
        for service in SERVICES:
            check(service + " sin puertos publicados", bool(config["services"][service].get("ports")), False)
        print("Construyendo el stack aislado...")
        run(compose + ["build", *SERVICES, "gateway"], timeout=900)
        for service in [*SERVICES, "gateway"]:
            failed = subprocess.run(compose + ["run", "--rm", "--no-deps", "-e", "InternalAccess__Key=", service],
                                    cwd=ROOT, env=private_env, text=True, capture_output=True, timeout=60)
            check(service + " no arranca sin clave", failed.returncode != 0, True)
            diagnostic = failed.stdout + failed.stderr
            check(service + " identifica configuracion invalida", "Configure InternalAccess:Key" in diagnostic, True)
            check(service + " no revela clave al fallar", settings["APP_SECRET_KEY"] in diagnostic, False)
        run(compose + ["up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "gateway"], timeout=240)
        base = "http://" + run(compose + ["port", "gateway", "8080"]).strip()
        credentials = {"nombreUsuario": settings["AUTH_ADMIN_USERNAME"], "password": settings["AUTH_ADMIN_PASSWORD"]}
        deadline = time.monotonic() + 120
        while True:
            try:
                status, login = request(base, "/api/auth/login", credentials)
                if status == 200:
                    break
            except (URLError, TimeoutError):
                pass
            if time.monotonic() >= deadline:
                raise RuntimeError("El login aislado no estuvo disponible en 120 segundos")
            time.sleep(1)
        check("login por Gateway", status, 200)
        token = login["accessToken"]
        check("Gateway sobrescribe cabecera del navegador", request(base, "/api/auth/login", credentials, internal="clave-del-cliente-no-confiable")[0], 200)
        check("Catalogo autenticado por Gateway", request(base, "/api/carreras", token=token)[0], 200)
        check("JWT sigue siendo obligatorio", request(base, "/api/carreras")[0], 401)
        check("JWT invalido se rechaza", request(base, "/api/carreras", token="invalido")[0], 401)
        today = datetime.now(timezone(timedelta(hours=-5))).date().isoformat()
        body = {"estadoAsignatura": "Aprobada", "fechaAprobacion": "2025-01-01", "fechaEvaluacion": today}
        check("Homologacion por Gateway", request(base, "/api/homologaciones/validar-antiguedad", body, token)[0], 200)

        # Cuenta sintética solo en la base Auth recién creada. Conserva hash,
        # nunca almacena una contraseña en texto plano ni habilita un endpoint seed.
        seed = "INSERT INTO Usuarios (Id,NombreUsuario,NombreNormalizado,PasswordHash,Activo,RolId) SELECT NEWID(),N'coord.hu36',N'COORD.HU36',PasswordHash,1,'00000000-0000-0000-0000-000000000003' FROM Usuarios WHERE NombreNormalizado=N'ADMIN.HU36';"
        run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                       'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAuth -b -Q "' + seed + '"'])
        coord_login = request(base, "/api/auth/login", {"nombreUsuario": "coord.hu36", "password": settings["AUTH_ADMIN_PASSWORD"]})
        check("login Coordinador", coord_login[0], 200)
        coord_token = coord_login[1]["accessToken"]
        career_ids = []
        for name in ["Origen sintetico HU36", "Receptora sintetica HU36"]:
            status, career = request(base, "/api/carreras", {"nombre": name, "facultad": "FISEI", "vigencia": True}, token)
            check("carrera sintetica creada: " + name, status, 201)
            career_ids.append(career["id"])
        status, references = request(base, "/api/solicitudes/carreras", token=coord_token)
        check("Solicitud consulta Catalogo con JWT y clave interna", status, 200)
        check("referencias correctas del Catalogo real", {item["id"] for item in references} == set(career_ids), True)
        application = {"numeroQuipux": "HU36-SINTETICO", "identificacionEstudiante": "HU36-SINTETICO", "nombreEstudiante": "Estudiante sintetico HU36",
                       "tipoCambio": "Interno", "carreraOrigenId": career_ids[0], "carreraReceptoraId": career_ids[1]}
        status, saved = request(base, "/api/solicitudes", application, coord_token)
        check("Solicitud sintetica persistida", status, 201)
        check("Solicitud consultable tras guardar", request(base, "/api/solicitudes/" + saved["id"], token=coord_token)[0], 200)
        check("Solicitud sin JWT se rechaza", request(base, "/api/solicitudes")[0], 401)
        check("Administrador no suplanta al Coordinador", request(base, "/api/solicitudes", token=token)[0], 403)

        # curl solo existe en el contenedor de pruebas SDK. La clave viaja por
        # entorno/stdin interno, nunca como valor en los argumentos del host.
        for service in SERVICES:
            for mode, expected in [("missing", "403"), ("wrong", "403"), ("valid", "200")]:
                script = 'curl --silent --show-error --max-time 10 --output /dev/null --write-out "%{http_code}"'
                if mode == "valid":
                    script = 'printf \'header = "X-Internal-Key: %s"\\n\' "$APP_SECRET_KEY" | ' + script + ' --config -'
                elif mode == "wrong":
                    script += ' --header "X-Internal-Key: incorrecta"'
                script += f" http://{service}:8080/swagger/v1/swagger.json"
                result = run(["docker", "run", "--rm", "--network", project + "_default", "--env", "APP_SECRET_KEY",
                              "--entrypoint", "/bin/bash", "mcr.microsoft.com/dotnet/sdk:8.0", "-c", script]).strip()
                check(f"{service} llamada interna {mode}", result, expected)
        logs = run(compose + ["logs", "--no-color", *SERVICES, "gateway"])
        check("secreto interno ausente de logs", settings["APP_SECRET_KEY"] in logs, False)
        # El inventario efectivo también debe carecer de publicaciones de puertos.
        for service in SERVICES:
            container_id = run(compose + ["ps", "-q", service]).strip()
            inspect = json.loads(run(["docker", "inspect", container_id]))[0]
            check(service + " sin bindings efectivos", bool(inspect["HostConfig"].get("PortBindings")), False)
        (output / "resultado.json").write_text(json.dumps({"proyecto": project, "casos": checks, "solicitudes": "incluido desde develop e01767b; guarda y cliente HTTP comprobados",
            "https_jwe": "pendiente integracion PR #47/#49"}, ensure_ascii=False, indent=2), encoding="utf-8")
    finally:
        # Solo el proyecto aleatorio creado por esta ejecución; jamás los stacks del equipo.
        cleanup = subprocess.run(compose + ["down", "--volumes", "--remove-orphans"], cwd=ROOT, env=private_env, capture_output=True, timeout=120)
        if cleanup.returncode:
            print("Limpieza pendiente del proyecto aislado:", project)
        env_path.unlink(missing_ok=True)


if __name__ == "__main__":
    main()
