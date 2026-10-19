#!/usr/bin/env python3
"""HU-01 / Issue #1: verifica T1 contra el Compose local ya iniciado.
Uso: python3 tests/auth-service/smoke.py /ruta/privada/auth.env [proyecto-compose]
No imprime contraseñas, hashes ni JWT. Requiere el daemon Docker.
"""
import base64
import hashlib
import hmac
import json
from pathlib import Path
import subprocess
import sys
import time
import urllib.error
import urllib.request

root = Path(__file__).resolve().parents[2]
env_file = Path(sys.argv[1]).resolve()
project = sys.argv[2] if len(sys.argv) > 2 else "hu01-verification"
settings = dict(line.split("=", 1) for line in env_file.read_text().splitlines() if line and not line.startswith("#"))
base = "http://127.0.0.1:" + settings.get("AUTH_HTTP_PORT", "5081")
compose = ["docker", "compose", "--env-file", str(env_file), "-f", str(root / "src/auth-service/compose.yaml"), "-p", project]


def request(path, payload=None):
    body = None if payload is None else json.dumps(payload).encode()
    req = urllib.request.Request(base + path, data=body, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            return response.status, json.load(response), response.headers
    except urllib.error.HTTPError as error:
        return error.code, json.load(error), error.headers


def wait_ready():
    for _ in range(60):
        try:
            if request("/swagger/v1/swagger.json")[0] == 200:
                return
        except (OSError, ValueError):
            pass
        time.sleep(1)
    raise AssertionError("auth-service no estuvo listo en 60 segundos")


def password_hash():
    # Capturar para comparar; nunca mostrar el resultado de sqlcmd.
    query = 'SET NOCOUNT ON; SELECT PasswordHash FROM Usuarios;'
    command = 'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAuth -h -1 -W -b -Q ' + "'" + query + "'"
    return subprocess.check_output(compose + ["exec", "-T", "auth-db", "sh", "-c", command], text=True).strip()


wait_ready()
credentials = {"nombreUsuario": settings["AUTH_ADMIN_USERNAME"], "password": settings["AUTH_ADMIN_PASSWORD"]}
status, result, headers = request("/api/auth/login", credentials)
assert status == 200 and headers.get("Cache-Control") == "no-store"
assert result["tokenType"] == "Bearer"
header, payload, signature = result["accessToken"].split(".")
decode = lambda part: base64.urlsafe_b64decode(part + "=" * (-len(part) % 4))
assert json.loads(decode(header))["alg"] == "HS256"
claims = json.loads(decode(payload))
expected = hmac.new(settings["AUTH_JWT_KEY"].encode(), (header + "." + payload).encode(), hashlib.sha256).digest()
assert hmac.compare_digest(expected, decode(signature))
assert claims["iss"] == settings["AUTH_JWT_ISSUER"] and claims["aud"] == settings["AUTH_JWT_AUDIENCE"]
assert claims["role"] == "Administrador" and claims["sub"] and claims["jti"]
assert claims["exp"] > time.time() and claims["exp"] - claims["iat"] == 900
print("PASS: login SQL Server emite JWT HS256 válido con rol y expiración de 15 minutos")
for name in (credentials["nombreUsuario"], "usuario-inexistente"):
    status, body, _ = request("/api/auth/login", {"nombreUsuario": name, "password": "incorrecta"})
    assert status == 401 and "accessToken" not in body and body["title"] == "Credenciales incorrectas"
assert request("/api/auth/login", {})[0] == 400
assert "/api/auth/login" in request("/swagger/v1/swagger.json")[1]["paths"]
print("PASS: credenciales incorrectas -> 401 sin token; cuerpo inválido -> 400; OpenAPI disponible")
original = password_hash()
assert original and original != credentials["password"] and len(original.splitlines()) == 1
subprocess.run(compose + ["restart", "auth-service"], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
wait_ready()
assert password_hash() == original
assert request("/api/auth/login", credentials)[0] == 200
print("PASS: reinicio reaplica migraciones y semilla sin duplicar ni sobrescribir el hash")
