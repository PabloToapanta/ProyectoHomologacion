"""HU-14 / Issue #5. Solo datos sintéticos; no imprime credenciales ni JWT.

Ejecutar con el stack local levantado y AUTH_ADMIN_USERNAME / AUTH_ADMIN_PASSWORD
inyectados en el entorno. GATEWAY_URL por defecto: http://127.0.0.1:5080.
EQUIVALENCIA_UMBRAL_PCT debe coincidir con la configuración del servicio (80 por defecto).
"""
import copy
import json
import os
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen


def request(path, body, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = Request(os.environ.get("GATEWAY_URL", "http://127.0.0.1:5080").rstrip("/") + path,
                  data=json.dumps(body).encode(), headers=headers, method="POST")
    try:
        with urlopen(req, timeout=20) as response:
            return response.status, json.load(response)
    except HTTPError as error:
        return error.code, None


def main():
    fixture = Path(__file__).resolve().parents[2] / "src/homologacion-service/examples/evaluar.json"
    data = json.loads(fixture.read_text())
    ruta = "/api/homologaciones/evaluar"
    assert request(ruta, data)[0] == 401, "La ruta debe exigir JWT"
    assert request(ruta, data, "token-invalido")[0] == 401, "JWT inválido debe ser rechazado"
    status, login = request("/api/auth/login", {
        "nombreUsuario": os.environ["AUTH_ADMIN_USERNAME"], "password": os.environ["AUTH_ADMIN_PASSWORD"]})
    assert status == 200, f"Login esperado 200, recibido {status}"
    token = login["accessToken"]
    status, result = request(ruta, data, token)
    assert status == 200, f"Evaluación esperada 200, recibida {status}"
    assert result["compatibilidadAcademicaPct"] == 80
    assert result["compatibilidadHorariaPct"] == result["compatibilidadCreditosPct"] == 80
    umbral = float(os.environ.get("EQUIVALENCIA_UMBRAL_PCT", "80"))
    assert result["umbralPct"] == umbral
    assert result["homologable"] is (umbral <= 80)
    assert result["estado"] == ("Homologable" if umbral <= 80 else "NoHomologable")
    assert result["contenidosSinCobertura"] == ["C9", "C10"]
    assert result["resultadosAprendizajeSinCobertura"] == ["R5"]
    assert request(ruta, {"receptora": {"horas": -1}}, token)[0] == 400
    varios = copy.deepcopy(data)
    segundo = copy.deepcopy(varios["origenes"][0]); segundo["id"] = "otro-origen-sintetico"
    varios["origenes"].append(segundo)
    duplicada = copy.deepcopy(varios["correspondenciasContenidos"][0]); duplicada["asignaturaOrigen"] = segundo["id"]
    varios["correspondenciasContenidos"].append(duplicada)
    status, parcial = request(ruta, varios, token)
    assert status == 200 and parcial["compatibilidadAcademicaPct"] == 80
    assert parcial["compatibilidadHorariaPct"] is None and parcial["compatibilidadCreditosPct"] is None
    assert parcial["estado"] == "InformacionIncompleta" and parcial["homologable"] is False
    print("PASS: varios orígenes no duplican cobertura ni suman horas/créditos")
    print("PASS: login Auth + SQL Server → JWT → Gateway YARP → Homologación")
    print("PASS: sin JWT 401; JWT inválido 401; datos inválidos 400")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
