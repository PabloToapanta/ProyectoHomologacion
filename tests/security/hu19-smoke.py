"""HU-19: análisis de cambio externo con Gateway/JWE/servicios/SQL reales.

Credenciales aleatorias por ejecución; no lee .env existentes. HTTP local aislado: no acredita HTTPS.
El análisis externo no invoca al agente (D2), por lo que no se necesita modelo.
Solo elimina el proyecto aleatorio creado por esta ejecución.
"""

import base64
from http.client import RemoteDisconnected
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[2]


def main():
    project = "hu19-check-" + secrets.token_hex(5)
    assert re.fullmatch(r"hu19-check-[0-9a-f]{10}", project)
    output = ROOT / "TestResults" / "hu19-integration"
    output.mkdir(parents=True, exist_ok=True)
    settings = {
        "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "AUTH_JWT_ISSUER": "hu19-isolated",
        "AUTH_JWT_AUDIENCE": "hu19-isolated-api",
        "AUTH_JWT_KEY": secrets.token_hex(32),
        "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
        "APP_SECRET_KEY": secrets.token_urlsafe(32),
        "AUTH_ADMIN_USERNAME": "admin.hu19",
        "AUTH_ADMIN_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "CATALOGO_DATABASE": "Hu19Catalogo",
        "AUTH_SERVICE_URL": "http://auth-service:8080/",
        "CATALOGO_SERVICE_URL": "http://catalogo-api:8080/",
        "HOMOLOGACION_SERVICE_URL": "http://homologacion-service:8080/",
        "GATEWAY_HTTP_PORT": "0", "FRONTEND_HTTP_PORT": "0",
        "ANALISIS_INTERNO_HABILITADO": "false", "OLLAMA_URL": "", "OLLAMA_MODELO": "",
    }
    environment = {**os.environ, **settings}
    private_values = [settings[k] for k in ["AUTH_SQL_PASSWORD", "AUTH_JWT_KEY", "AUTH_JWT_ENCRYPTION_KEY", "APP_SECRET_KEY", "AUTH_ADMIN_PASSWORD"]]
    env_path = ROOT / (".env." + project)
    env_path.write_text("\n".join(f"{k}={v}" for k, v in settings.items()), encoding="utf-8")
    compose = ["docker", "compose", "--env-file", str(env_path), "-p", project,
               "-f", "compose.sprint1.yaml", "-f", "compose.hu10.yaml"]
    checks = []

    def sanear(texto):
        for value in private_values:
            if value:
                texto = texto.replace(value, "[oculto]")
        return texto

    def run(args, timeout=120):
        result = subprocess.run(args, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=timeout)
        if result.returncode:
            (output / "error.log").write_text(sanear(result.stdout + result.stderr), encoding="utf-8")
            raise RuntimeError("Comando Docker falló; diagnóstico saneado en TestResults/hu19-integration/error.log")
        return result.stdout

    def check(name, actual, expected):
        assert actual == expected, f"{name}: esperado {expected}, obtenido {actual}"
        checks.append({"caso": name, "resultado": actual})
        print("PASS", name, flush=True)

    def request(path, body=None, token=None, method=None):
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        req = Request(base + path, data=None if body is None else json.dumps(body).encode(), headers=headers, method=method)
        try:
            with urlopen(req, timeout=15) as response:
                text = response.read().decode()
                assert settings["APP_SECRET_KEY"] not in text
                return response.status, json.loads(text) if text else None
        except HTTPError as error:
            return error.code, None

    def post(path, body, token, expected=201):
        status, value = request(path, body, token)
        check(path, status, expected)
        return value

    try:
        print("Construyendo servicios aislados...", flush=True)
        run(compose + ["build", "auth-service", "catalogo-api", "homologacion-service", "solicitudes-service", "gateway"], 900)
        run(compose + ["up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "gateway"], 240)
        base = "http://" + run(compose + ["port", "gateway", "8080"]).strip()
        deadline = time.monotonic() + 120
        while True:
            try:
                status, login = request("/api/auth/login", {"nombreUsuario": "admin.hu19", "password": settings["AUTH_ADMIN_PASSWORD"]})
                if status == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= deadline:
                raise RuntimeError("Login aislado no disponible")
            time.sleep(1)
        admin = login["accessToken"]
        actors = [("coord.hu19", 3), ("doc1.hu19", 2), ("doc2.hu19", 2), ("doc3.hu19", 2)]
        sql = "".join("INSERT INTO Usuarios (Id,NombreUsuario,NombreNormalizado,PasswordHash,Activo,RolId) "
                      f"SELECT NEWID(),N'{name}',N'{name.upper()}',PasswordHash,1,'00000000-0000-0000-0000-{role:012d}' "
                      "FROM Usuarios WHERE NombreNormalizado=N'ADMIN.HU19';" for name, role in actors)
        run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                       'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAuth -b -Q "' + sql + '"'])
        tokens = {name: post("/api/auth/login", {"nombreUsuario": name, "password": settings["AUTH_ADMIN_PASSWORD"]}, None, 200)["accessToken"] for name, _ in actors}
        coord, docente, ajeno = tokens["coord.hu19"], tokens["doc1.hu19"], tokens["doc3.hu19"]

        career = post("/api/carreras", {"nombre": "HU19 receptora sintetica", "facultad": "FISEI", "vigencia": True}, admin)
        receptora = post("/api/asignaturas", {"carreraId": career["id"], "codigo": "HU19-R", "nombre": "Receptora sintetica", "nivel": 1, "creditos": 3, "horas": 100}, admin)
        post(f'/api/asignaturas/{receptora["id"]}/unidades', {"numero": 1, "nombre": "Unidad sintetica", "horas": 100, "resultadoAprendizaje": "Resultado sintetico", "contenidos": ["Contenido sintetico"]}, admin)
        check("perfil receptora", request(f'/api/asignaturas/{receptora["id"]}/perfil-comparacion', {"planAnalitico": "plan-sintetico-R", "unidadHoras": "hora-reloj", "sistemaCreditos": "sistema-sintetico", "esSintetico": True}, admin, "PUT")[0], 200)

        # D1: universidad ecuatoriana, sin apostilla. D4: cambios previos declarados, períodos aún sin dato.
        application = post("/api/solicitudes", {"numeroQuipux": "HU19-SINTETICO", "identificacionEstudiante": "HU19-SINTETICO", "nombreEstudiante": "Estudiante sintetico externo",
                                                "tipoCambio": "Externo", "institucionOrigen": "Universidad sintetica Ecuador", "carreraOrigenExterna": "Carrera externa sintetica",
                                                "carreraReceptoraId": career["id"], "cambiosPreviosCarrera": 0}, coord)
        sid = application["id"]
        check("periodos sin dato al registrar", application["periodosCursados"], None)
        status, corregida = request(f"/api/solicitudes/{sid}/datos-cambio-externo", {"periodosCursados": 3, "cambiosPreviosCarrera": 0}, coord, "PUT")
        check("coordinador corrige datos RN-05", (status, corregida["periodosCursados"]), (200, 3))
        def externa(nombre, unidad, horas):
            return post(f"/api/solicitudes/{sid}/asignaturas-aprobadas", {"nombreAsignatura": nombre, "creditos": 3, "sistemaCreditos": "sistema-sintetico", "horas": horas,
                        "unidadHoras": unidad, "referenciaPrograma": "programa-externo-sintetico", "nota": 85, "notaMaximaEscala": 100, "notaMinimaAprobacion": 70,
                        "fechaAprobacion": "2025-07-15", "numeroMatricula": "HU19-MATRICULA", "intentoMatricula": 1}, coord)
        igual = externa("Externa misma unidad", "  HORA-RELOJ ", 80)
        distinta = externa("Externa otra unidad", "hora-academica", 80)
        teacher_status, teachers = request(f"/api/solicitudes/{sid}/docentes-disponibles", token=coord)
        check("directorio docente real", teacher_status, 200)
        post(f"/api/solicitudes/{sid}/docentes", {"docenteIds": [t["id"] for t in teachers if t["nombreUsuario"] in ["doc1.hu19", "doc2.hu19"]]}, coord, 200)
        check("datos RN-05 bloqueados tras asignar", request(f"/api/solicitudes/{sid}/datos-cambio-externo", {"periodosCursados": 9, "cambiosPreviosCarrera": 0}, coord, "PUT")[0], 409)
        check("contexto externo no publicado", request(f"/api/solicitudes/{sid}/contexto-analisis-externo", token=docente)[0], 404)

        path = f"/api/homologaciones/solicitudes/{sid}/analisis-externo"
        body = {"asignaturaAprobadaId": igual["id"], "asignaturaReceptoraId": receptora["id"]}
        check("sin JWT", request(path, body)[0], 401)
        check("coordinador no suplanta docente", request(path, body, coord)[0], 403)
        check("docente ajeno", request(path, body, ajeno)[0], 404)
        check("no acepta cantidades del cliente", request(path, {**body, "horas": 100}, docente)[0], 400)
        check("ruta interna rechaza solicitud externa", request(f"/api/homologaciones/solicitudes/{sid}/analisis-interno", body, docente)[0], 400)
        status, options = request(path + "/opciones", token=docente)
        check("opciones solo carrera receptora", (status, [r["id"] for r in options]), (200, [receptora["id"]]))

        status, result = request(path, body, docente)
        check("analisis externo HTTP real", status, 200)
        check("D2 academico incompleto", result["estado"], "Incompleto")
        check("D2 sin sugerencia", result["resultado"], None)
        check("D2 sin porcentaje academico", result["evaluacion"]["compatibilidadAcademicaPct"], None)
        check("D3 horas con unidad normalizada", result["evaluacion"]["compatibilidadHorariaPct"], 80)
        check("D3 creditos mismo sistema", result["evaluacion"]["compatibilidadCreditosPct"], 100)
        check("antiguedad elegible", result["antiguedad"]["estado"], "Elegible")
        check("no expone datos personales", any(x in json.dumps(result) for x in ["HU19-SINTETICO", "HU19-MATRICULA", "Estudiante sintetico"]), False)
        check("sin apostilla", "apostill" in json.dumps(result).lower(), False)
        status, other = request(path, {"asignaturaAprobadaId": distinta["id"], "asignaturaReceptoraId": receptora["id"]}, docente)
        check("D3 unidad distinta sin conversion", (status, other["evaluacion"]["compatibilidadHorariaPct"]), (200, None))
        check("D3 motivo registrado", any("sin conversión aprobada" in f for f in other["evaluacion"]["informacionFaltante"]), True)

        req_path = f"/api/homologaciones/solicitudes/{sid}/requisitos-externos"
        status, req = request(req_path, token=docente)
        check("RN-05 externo HTTP", status, 200)
        check("RN-05 incompleto sin analisis completos", (req["estado"], req["asignaturasHomologables"], req["incumplimientos"]), ("Incompleto", 0, []))
        check("RN-05 usa datos declarados", (req["periodosCursados"], req["cambiosPreviosCarrera"]), (3, 0))
        check("RN-05 ajeno denegado", request(req_path, token=ajeno)[0], 404)

        run(compose + ["restart", "homologacion-service"])
        limit = time.monotonic() + 60
        while True:
            try:
                code, saved = request(path, token=docente)
                if code == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= limit:
                raise RuntimeError("No fue posible recuperar el guardado tras reiniciar")
            time.sleep(1)
        check("dos parejas conservadas tras reinicio", len(saved), 2)
        check("estado conservado tras reinicio", sorted(x["analisis"]["estado"] for x in saved), ["Incompleto", "Incompleto"])
        check("lectura guardada ajena denegada", request(path, token=ajeno)[0], 404)
        post(path, body, docente, 200)
        check("reanalisis no duplica pareja", len(request(path, token=docente)[1]), 2)

        def sql_homologacion(query):
            return run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAnalisis -b -Q "' + query + '"'])
        check("tabla interna sin filas externas", "0" in sql_homologacion("SET NOCOUNT ON; SELECT COUNT(*) FROM AnalisisInternos;").split(), True)
        antes = request(path, token=docente)[1]
        sql_homologacion("CREATE TRIGGER HU19_FalloPrueba ON AnalisisExternos AFTER UPDATE AS THROW 51000, 'Fallo sintetico de persistencia', 1;")
        check("fallo de almacenamiento controlado", request(path, body, docente)[0], 503)
        check("fallo no reemplaza resultado persistido", request(path, token=docente)[1], antes)
        sql_homologacion("DROP TRIGGER HU19_FalloPrueba;")
        (output / "resultado.json").write_text(json.dumps({"proyecto": project, "casos": checks, "transporte": "HTTP local aislado; no acredita HTTPS",
                                                          "modelo": "no se invoca en análisis externo (D2)"}, ensure_ascii=False, indent=2), encoding="utf-8")
    except Exception:
        (output / "servicios-error.log").write_text(sanear(run(compose + ["logs", "--no-color", "--tail", "100"])), encoding="utf-8")
        raise
    finally:
        cleanup = subprocess.run(compose + ["down", "--volumes", "--remove-orphans"], cwd=ROOT, env=environment, capture_output=True, timeout=120)
        if cleanup.returncode:
            print("Limpieza pendiente:", project)
        env_path.unlink(missing_ok=True)


if __name__ == "__main__":
    main()
