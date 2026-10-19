#!/usr/bin/env python3
"""HU-01 / T2: smoke con Gateway + auth-service + SQL Server reales.
Uso: python3 tests/gateway/smoke.py /ruta/privada/t2.env
Catálogo/Homologación deben apuntar a destinos de prueba inaccesibles.
No imprime credenciales ni JWT. No crea microservicios simulados.
"""
import json
from pathlib import Path
import sys
import time
import urllib.error
import urllib.request

settings = dict(line.split("=", 1) for line in Path(sys.argv[1]).read_text().splitlines() if line and not line.startswith("#"))
base = "http://127.0.0.1:" + settings.get("GATEWAY_HTTP_PORT", "5080")


def request(path, payload=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(base + path, data=None if payload is None else json.dumps(payload).encode(), headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as error:
        raw = error.read()
        return error.code, json.loads(raw) if raw else {}


credentials = {"nombreUsuario": settings["AUTH_ADMIN_USERNAME"], "password": settings["AUTH_ADMIN_PASSWORD"]}
for _ in range(60):
    try:
        status, result = request("/api/auth/login", credentials)
        if status == 200:
            break
    except (OSError, ValueError):
        pass
    time.sleep(1)
else:
    raise AssertionError("Login a través de Gateway no disponible en 60 segundos")
assert result["accessToken"] and result["tokenType"] == "Bearer"
print("PASS: login público Gateway -> auth-service -> SQL Server devuelve JWT")
for path in ("/api/carreras", "/api/asignaturas/asignatura-prueba"):
    assert request(path)[0] == 401
    assert request(path, token="invalido")[0] == 401
assert request("/api/homologaciones/evaluar", {}, token="invalido")[0] == 401
print("PASS: rutas académicas sin JWT o con JWT inválido devuelven 401")
status, body = request("/api/auth/login", {"nombreUsuario": credentials["nombreUsuario"], "password": "incorrecta"})
assert status == 401 and "accessToken" not in body and body["title"] == "Credenciales incorrectas"
assert request("/api/auth/login", {})[0] == 400
print("PASS: Gateway conserva errores 401/400 de auth-service sin emitir token")
# 502 demuestra validación y despacho; los servicios académicos no están implementados aquí.
assert request("/api/carreras", token=result["accessToken"])[0] == 502
assert request("/api/homologaciones/evaluar", {}, token=result["accessToken"])[0] == 502
print("PASS: JWT real aceptado; destinos académicos inaccesibles devuelven 502, no 401")
assert "/api/auth/login" in request("/swagger/v1/swagger.json")[1]["paths"]
print("PASS: documentación OpenAPI del Gateway disponible")
