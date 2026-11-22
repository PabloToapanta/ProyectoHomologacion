"""HU-02 / T54-T55: recorrido aislado Auth + Gateway + Frontend + SQL Server por HTTPS.

Usa secretos y certificado sintéticos en un directorio temporal. No imprime claves
ni tokens; elimina solamente su propio proyecto Compose al terminar.
Ejecutar desde la raíz: python3 tests/security/hu02-smoke.py
"""

import base64
import json
import os
from pathlib import Path
import re
import secrets
import ssl
import subprocess
import tempfile
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


ROOT = Path(__file__).resolve().parents[2]
PROJECT = "hu02-check-" + secrets.token_hex(5)
assert re.fullmatch(r"hu02-check-[0-9a-f]{10}", PROJECT)


def command(args, env, *, timeout=600):
    result = subprocess.run(args, cwd=ROOT, env=env, text=True, capture_output=True, timeout=timeout)
    if result.returncode:
        raise RuntimeError(f"Falló {args[0]} {args[1]} (salida omitida para proteger secretos)")
    return result.stdout.strip()


def main():
    checks = []
    with tempfile.TemporaryDirectory(prefix="hu02-integracion-") as temp:
        directory = Path(temp)
        password_admin = "Aa1!" + secrets.token_hex(20)
        password_inicial = "Aa1!" + secrets.token_hex(20)
        password_nueva = "Aa1!" + secrets.token_hex(20)
        settings = {
            "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20),
            "AUTH_JWT_ISSUER": "hu02-isolated",
            "AUTH_JWT_AUDIENCE": "hu02-isolated-api",
            "AUTH_JWT_KEY": secrets.token_hex(32),
            "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
            "APP_SECRET_KEY": secrets.token_urlsafe(32),
            "AUTH_ADMIN_USERNAME": "admin.hu02",
            "AUTH_ADMIN_PASSWORD": password_admin,
            "GATEWAY_HTTP_PORT": "0",
            "FRONTEND_HTTP_PORT": "0",
            "HU02_CERT_DIR": str(directory),
        }
        env = {**os.environ, **settings, "DOCKER_CONTEXT": "default"}
        env_file = directory / "private.env"
        env_file.write_text("\n".join(f"{key}={value}" for key, value in settings.items()), encoding="utf-8")
        env_file.chmod(0o600)
        cert = directory / "localhost.pem"
        key = directory / "localhost-key.pem"
        command(["openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "1",
                 "-subj", "/CN=localhost", "-addext", "subjectAltName=DNS:localhost,IP:127.0.0.1",
                 "-keyout", str(key), "-out", str(cert)], env, timeout=30)
        key.chmod(0o600)
        overlay = directory / "compose.hu02.yaml"
        overlay.write_text("""services:
  gateway:
    user: "0:0"
    environment:
      ASPNETCORE_ENVIRONMENT: Hu34
      Kestrel__Endpoints__Http__Url: http://0.0.0.0:8080
      Kestrel__Endpoints__Https__Url: https://0.0.0.0:8443
      Kestrel__Endpoints__Https__Certificate__Path: /https/localhost.pem
      Kestrel__Endpoints__Https__Certificate__KeyPath: /https/localhost-key.pem
    volumes:
      - ${HU02_CERT_DIR}:/https:ro
    ports: !override
      - "127.0.0.1::8443"
    depends_on: !override []
  frontend:
    ports: !override []
    depends_on: !override []
""", encoding="utf-8")
        compose = ["docker", "compose", "--env-file", str(env_file), "-p", PROJECT,
                   "-f", "compose.sprint1.yaml", "-f", str(overlay)]
        context = ssl.create_default_context(cafile=str(cert))

        def request(base, path, method="GET", body=None, token=None):
            headers = {"Content-Type": "application/json"}
            if token:
                headers["Authorization"] = "Bearer " + token
            data = None if body is None else json.dumps(body).encode()
            req = Request(base + path, method=method, data=data, headers=headers)
            try:
                with urlopen(req, context=context, timeout=15) as response:
                    raw = response.read().decode()
                    return response.status, json.loads(raw) if raw else None
            except HTTPError as error:
                error.read()
                return error.code, None

        def check(name, actual, expected):
            assert actual == expected, f"{name}: esperado {expected}, recibido {actual}"
            checks.append(name)
            print("PASS", name, flush=True)

        try:
            command(compose + ["build", "auth-service", "gateway", "frontend"], env, timeout=900)
            command(compose + ["up", "-d", "--wait", "--wait-timeout", "180", "auth-db"], env, timeout=240)
            command(compose + ["up", "-d", "--no-deps", "auth-service", "frontend", "gateway"], env, timeout=120)
            port = command(compose + ["port", "gateway", "8443"], env).rsplit(":", 1)[-1]
            base = f"https://localhost:{port}"
            deadline = time.monotonic() + 90
            while True:
                try:
                    login_status, login = request(base, "/api/auth/login", "POST",
                                                  {"nombreUsuario": "admin.hu02", "password": password_admin})
                    if login_status == 200:
                        break
                except (URLError, TimeoutError, ConnectionError):
                    pass
                if time.monotonic() > deadline:
                    raise RuntimeError("Auth/Gateway no estuvo disponible en 90 segundos")
                time.sleep(1)
            admin = login["accessToken"]
            check("login administrador por HTTPS", login_status, 200)
            with urlopen(base + "/admin/usuarios", context=context, timeout=15) as frontend_response:
                html = frontend_response.read().decode()
                check("ruta SPA de usuarios por Gateway HTTPS", frontend_response.status, 200)
                check("HTML de frontend en ruta usuarios", 'id="root"' in html, True)
            check("sin token: 401", request(base, "/api/usuarios")[0], 401)
            status, docente = request(base, "/api/usuarios", "POST",
                                      {"nombreUsuario": "docente.hu02", "rol": "Docente", "contrasena": password_inicial}, admin)
            check("alta docente", status, 201)
            docente_id = docente["id"]
            check("alta sin secreto", password_inicial in json.dumps(docente), False)
            docente_original = request(base, "/api/auth/login", "POST",
                                       {"nombreUsuario": "docente.hu02", "password": password_inicial})[1]["accessToken"]
            check("docente: 403", request(base, "/api/usuarios", token=docente_original)[0], 403)
            status, coordinador = request(base, "/api/usuarios", "POST",
                                          {"nombreUsuario": "coord.hu02", "rol": "Coordinador", "contrasena": password_inicial}, admin)
            check("alta coordinador", status, 201)
            check("duplicado normalizado: 409", request(base, "/api/usuarios", "POST",
                  {"nombreUsuario": " DOCENTE.HU02 ", "rol": "Docente", "contrasena": password_inicial}, admin)[0], 409)
            status, listado = request(base, "/api/usuarios", token=admin)
            check("listado administrativo", status, 200)
            check("listado sin secretos", any(password_inicial in json.dumps(u) or "passwordHash" in u for u in listado), False)
            check("detalle por id", request(base, "/api/usuarios/" + docente_id, token=admin)[0], 200)
            check("rol desconocido: 400", request(base, "/api/usuarios/" + docente_id + "/rol", "PUT", {"rol": "Administrador"}, admin)[0], 400)
            check("cambio de rol", request(base, "/api/usuarios/" + docente_id + "/rol", "PUT", {"rol": "Coordinador"}, admin)[0], 200)
            status, docente_login = request(base, "/api/auth/login", "POST", {"nombreUsuario": "docente.hu02", "password": password_inicial})
            check("login refleja rol nuevo", (status, docente_login["role"]), (200, "Coordinador"))
            check("coordinador: 403", request(base, "/api/usuarios", token=docente_login["accessToken"])[0], 403)
            check("desactivar", request(base, "/api/usuarios/" + docente_id + "/estado", "PATCH", {"activo": False}, admin)[0], 200)
            check("inactivo no inicia sesion", request(base, "/api/auth/login", "POST", {"nombreUsuario": "docente.hu02", "password": password_inicial})[0], 401)
            check("reactivar", request(base, "/api/usuarios/" + docente_id + "/estado", "PATCH", {"activo": True}, admin)[0], 200)
            check("nueva contrasena", request(base, "/api/usuarios/" + docente_id + "/contrasena", "PUT", {"contrasena": password_nueva}, admin)[0], 200)
            check("contrasena anterior rechazada", request(base, "/api/auth/login", "POST", {"nombreUsuario": "docente.hu02", "password": password_inicial})[0], 401)
            check("contrasena nueva valida", request(base, "/api/auth/login", "POST", {"nombreUsuario": "docente.hu02", "password": password_nueva})[0], 200)
            admin_id = command(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAuth -h -1 -W -Q "SET NOCOUNT ON; SELECT CONVERT(varchar(36),Id) FROM Usuarios WHERE NombreNormalizado=N\'ADMIN.HU02\'"'], env).strip()
            check("autodesactivacion rechazada", request(base, "/api/usuarios/" + admin_id + "/estado", "PATCH", {"activo": False}, admin)[0], 400)
            token_coord = request(base, "/api/auth/login", "POST", {"nombreUsuario": "coord.hu02", "password": password_inicial})[1]["accessToken"]
            check("coordinador sin permisos: 403", request(base, "/api/usuarios", token=token_coord)[0], 403)
            # El entorno Hu34 no publica Swagger; su contrato se verifica en Gateway.Tests.
        finally:
            subprocess.run(compose + ["down", "--volumes", "--remove-orphans"], cwd=ROOT, env=env,
                           capture_output=True, text=True, timeout=120)

    output = ROOT / "TestResults" / "hu02-integration"
    output.mkdir(parents=True, exist_ok=True)
    (output / "resultado.json").write_text(json.dumps({"checks": checks, "total": len(checks)}, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
