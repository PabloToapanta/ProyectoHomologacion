"""Comprobación integrada local de HU-01/04/05/14/15/16, sin escribir datos académicos.

Uso desde la raíz: python3 tests/sprint-1/smoke.py --env-file .env.sprint1
Solo biblioteca estándar. No imprime credenciales ni tokens.
"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


def load_settings(path):
    settings = dict(os.environ)
    for line in path.read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        key, value = line.split("=", 1)
        settings[key.strip()] = value.strip().strip("\"").strip("'")
    return settings


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--env-file", type=Path, required=True)
    args = parser.parse_args()
    settings = load_settings(args.env_file)
    base = settings.get("GATEWAY_URL", "http://127.0.0.1:" + settings.get("GATEWAY_HTTP_PORT", "5080")).rstrip("/")
    frontend = settings.get("FRONTEND_URL", "http://127.0.0.1:" + settings.get("FRONTEND_HTTP_PORT", "5173"))

    def request(path, body=None, token=None):
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        req = Request(base + path, data=None if body is None else json.dumps(body).encode(), headers=headers)
        try:
            with urlopen(req, timeout=10) as response:
                return response.status, json.load(response)
        except HTTPError as error:
            return error.code, None

    credentials = {"nombreUsuario": settings["AUTH_ADMIN_USERNAME"], "password": settings["AUTH_ADMIN_PASSWORD"]}
    deadline = time.monotonic() + 90
    while True:
        try:
            status, login = request("/api/auth/login", credentials)
            if status not in (502, 503, 504):
                break
        except (URLError, TimeoutError):
            pass
        if time.monotonic() >= deadline:
            raise RuntimeError("El stack no estuvo disponible en 90 segundos")
        time.sleep(1)
    assert status == 200, f"Login esperado 200, recibido {status}"
    token = login["accessToken"]
    assert request("/api/auth/login", {**credentials, "password": "incorrecta-sintetica"})[0] == 401
    rutas = ["/api/carreras", "/api/homologaciones/validar-antiguedad",
             "/api/homologaciones/validar-requisitos", "/api/homologaciones/evaluar"]
    for path in rutas:
        body = None if path == "/api/carreras" else {}
        assert request(path, body)[0] == 401, path
        assert request(path, body, "invalido")[0] == 401, path
    print("PASS HU-01: login real, credenciales incorrectas y JWT obligatorio en los servicios")

    status, carreras = request("/api/carreras", token=token)
    assert status == 200 and isinstance(carreras, list)
    for carrera in carreras:
        status, detail = request("/api/carreras/" + carrera["id"], token=token)
        assert status == 200 and detail == carrera
        status, subjects = request("/api/carreras/" + carrera["id"] + "/asignaturas", token=token)
        assert status == 200 and isinstance(subjects, list)
        assert [s["nivel"] for s in subjects] == sorted(s["nivel"] for s in subjects)
        for subject in subjects:
            status, detail = request("/api/asignaturas/" + subject["id"], token=token)
            assert status == 200 and detail == subject
    # La validación rechaza antes de escribir; no crea carreras de prueba.
    assert request("/api/carreras", {"nombre": "Prueba sintética rechazada", "facultad": "Otra facultad", "vigencia": True}, token)[0] == 400
    print(f"PASS HU-04/05: {len(carreras)} carreras consultadas; otra facultad rechazada con 400")

    ruta = "/api/homologaciones/validar-antiguedad"
    for estado, fecha, esperado in [
        ("Aprobada", "2017-10-20", "Elegible"),
        ("Aprobada", "2016-10-20", "Elegible"),
        ("Aprobada", "2016-10-19", "RequiereValidacionConocimientos"),
        ("Reprobada", None, "NoElegible"),
        ("NoCursada", None, "NoElegible"),
    ]:
        status, result = request(ruta, {"estadoAsignatura": estado, "fechaAprobacion": fecha, "fechaEvaluacion": "2026-10-20"}, token)
        assert status == 200, f"HU-15 esperaba 200, recibió {status}"
        assert result["estado"] == esperado and result["motivo"]
    for body in [{}, {"estadoAsignatura": "Aprobada", "fechaEvaluacion": "2026-10-20"},
                 {"estadoAsignatura": "Aprobada", "fechaAprobacion": "2026-10-21", "fechaEvaluacion": "2026-10-20"}]:
        assert request(ruta, body, token)[0] == 400
    print("PASS HU-15: cinco escenarios académicos por Gateway; datos inválidos e incompletos 400")

    ruta = "/api/homologaciones/validar-requisitos"
    for tipo, periodos, homologables, cambios, esperado in [
        ("Interno", 1, 1, 0, True),
        ("Interno", 1, 0, 0, False),
        ("Interno", 0, 1, 0, False),
        ("Externo", 2, 2, 0, True),
        ("Externo", 2, 1, 0, False),
        ("Externo", 1, 2, 0, False),
        ("Externo", 2, 2, 1, False),
    ]:
        body = {"tipoCambio": tipo, "periodosCursados": periodos,
                "asignaturasHomologables": homologables, "cambiosPrevios": cambios}
        status, result = request(ruta, body, token)
        assert status == 200, f"HU-16 esperaba 200, recibió {status}"
        assert result["cumple"] is esperado
        assert isinstance(result["incumplimientos"], list)
        assert (len(result["incumplimientos"]) == 0) is esperado
    valido = {"tipoCambio": "Interno", "periodosCursados": 1,
              "asignaturasHomologables": 1, "cambiosPrevios": 0}
    for body in [{}, {"tipoCambio": "Interno", "periodosCursados": 1, "asignaturasHomologables": 1},
                 {**valido, "periodosCursados": -1}, {**valido, "tipoCambio": "Otro"},
                 {**valido, "tipoCambio": 0}, {**valido, "cumple": True}]:
        assert request(ruta, body, token)[0] == 400
    print("PASS HU-16: siete escenarios sintéticos por Gateway; datos inválidos/incompletos/campos adicionales 400")

    root = Path(__file__).resolve().parents[2]
    sys.stdout.flush()
    subprocess.run([sys.executable, str(root / "tests/homologacion-service/smoke.py")],
                   env={**settings, "GATEWAY_URL": base}, check=True)
    with urlopen(frontend, timeout=10) as response:
        assert response.status == 200 and b'<div id="root">' in response.read()
    print("PASS frontend: React publicado; las pruebas de navegador se ejecutan por separado")
    print("HU-06 pendiente de integración; no se certifica aceptación de la PO")


if __name__ == "__main__":
    main()
