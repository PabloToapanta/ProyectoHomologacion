"""HU-18: recorrido real Gateway/JWE/servicios/SQL con modelo deshabilitado.

No lee .env existentes. HTTP local aislado: no acredita HTTPS ni modelo real.
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
    project = "hu18-check-" + secrets.token_hex(5)
    assert re.fullmatch(r"hu18-check-[0-9a-f]{10}", project)
    output = ROOT / "TestResults" / "hu18-integration"
    output.mkdir(parents=True, exist_ok=True)
    settings = {
        "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "AUTH_JWT_ISSUER": "hu18-isolated",
        "AUTH_JWT_AUDIENCE": "hu18-isolated-api",
        "AUTH_JWT_KEY": secrets.token_hex(32),
        "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
        "APP_SECRET_KEY": secrets.token_urlsafe(32),
        "AUTH_ADMIN_USERNAME": "admin.hu18",
        "AUTH_ADMIN_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "CATALOGO_DATABASE": "Hu18Catalogo",
        "AUTH_SERVICE_URL": "http://auth-service:8080/",
        "CATALOGO_SERVICE_URL": "http://catalogo-api:8080/",
        "HOMOLOGACION_SERVICE_URL": "http://homologacion-service:8080/",
        "GATEWAY_HTTP_PORT": "0", "FRONTEND_HTTP_PORT": "0",
        "ANALISIS_INTERNO_HABILITADO": "false", "OLLAMA_URL": "", "OLLAMA_MODELO": "",
    }
    environment = {**os.environ, **settings}
    env_path = ROOT / (".env." + project)
    env_path.write_text("\n".join(f"{k}={v}" for k, v in settings.items()), encoding="utf-8")
    compose = ["docker", "compose", "--env-file", str(env_path), "-p", project,
               "-f", "compose.sprint1.yaml", "-f", "compose.hu10.yaml"]
    checks = []

    def run(args, timeout=120):
        result = subprocess.run(args, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=timeout)
        if result.returncode:
            diagnostic = result.stdout + result.stderr
            for value in settings.values():
                if value:
                    diagnostic = diagnostic.replace(value, "[oculto]")
            (output / "error.log").write_text(diagnostic, encoding="utf-8")
            raise RuntimeError("Comando Docker falló; diagnóstico saneado en TestResults/hu18-integration/error.log")
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
        config = json.loads(run(compose + ["config", "--format", "json"]))
        check("modelo deshabilitado", str(config["services"]["homologacion-service"]["environment"]["AnalisisInterno__Habilitado"]).lower(), "false")
        print("Construyendo servicios aislados...", flush=True)
        run(compose + ["build", "auth-service", "catalogo-api", "homologacion-service", "solicitudes-service", "gateway"], 900)
        run(compose + ["up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "gateway"], 240)
        base = "http://" + run(compose + ["port", "gateway", "8080"]).strip()
        deadline = time.monotonic() + 120
        while True:
            try:
                status, login = request("/api/auth/login", {"nombreUsuario": "admin.hu18", "password": settings["AUTH_ADMIN_PASSWORD"]})
                if status == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= deadline:
                raise RuntimeError("Login aislado no disponible")
            time.sleep(1)
        check("login JWE real", len(login["accessToken"].split(".")), 5)
        admin = login["accessToken"]
        actors = [("coord.hu18", 3), ("doc1.hu18", 2), ("doc2.hu18", 2), ("doc3.hu18", 2)]
        sql = "".join("INSERT INTO Usuarios (Id,NombreUsuario,NombreNormalizado,PasswordHash,Activo,RolId) "
                      f"SELECT NEWID(),N'{name}',N'{name.upper()}',PasswordHash,1,'00000000-0000-0000-0000-{role:012d}' "
                      "FROM Usuarios WHERE NombreNormalizado=N'ADMIN.HU18';" for name, role in actors)
        run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                       'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAuth -b -Q "' + sql + '"'])
        tokens = {name: post("/api/auth/login", {"nombreUsuario": name, "password": settings["AUTH_ADMIN_PASSWORD"]}, None, 200)["accessToken"] for name, _ in actors}
        coord, docente = tokens["coord.hu18"], tokens["doc1.hu18"]
        careers, subjects, units = [], [], []
        for suffix in ["O", "R"]:
            career = post("/api/carreras", {"nombre": "HU18 sintetica " + suffix, "facultad": "FISEI", "vigencia": True}, admin)
            careers.append(career["id"])
            subject = post("/api/asignaturas", {"carreraId": career["id"], "codigo": "HU18-" + suffix, "nombre": "Asignatura sintetica " + suffix, "nivel": 1, "creditos": 3, "horas": 100}, admin)
            subjects.append(subject["id"])
            units.append(post(f'/api/asignaturas/{subject["id"]}/unidades', {"numero": 1, "nombre": "Unidad sintetica", "horas": 100, "resultadoAprendizaje": "Resultado sintetico", "contenidos": ["Contenido sintetico"]}, admin))
            check("perfil sintetico", request(f'/api/asignaturas/{subject["id"]}/perfil-comparacion', {"planAnalitico": "plan-sintetico-" + suffix, "unidadHoras": "hora-reloj", "sistemaCreditos": "sistema-sintetico", "esSintetico": True}, admin, "PUT")[0], 200)
        application = post("/api/solicitudes", {"numeroQuipux": "HU18-SINTETICO", "identificacionEstudiante": "HU18-SINTETICO", "nombreEstudiante": "Estudiante sintetico", "tipoCambio": "Interno", "carreraOrigenId": careers[0], "carreraReceptoraId": careers[1]}, coord)
        sid = application["id"]
        approved = post(f"/api/solicitudes/{sid}/asignaturas-aprobadas", {"asignaturaOrigenId": subjects[0], "nota": 8, "fechaAprobacion": "2026-06-01", "numeroMatricula": "HU18-SINTETICA", "intentoMatricula": 1}, coord)
        teacher_status, teachers = request(f"/api/solicitudes/{sid}/docentes-disponibles", token=coord)
        check("directorio docente real", teacher_status, 200)
        selected = [t["id"] for t in teachers if t["nombreUsuario"] in ["doc1.hu18", "doc2.hu18"]]
        post(f"/api/solicitudes/{sid}/docentes", {"docenteIds": selected}, coord, 200)
        path = f"/api/homologaciones/solicitudes/{sid}/analisis-interno"
        body = {"asignaturaAprobadaId": approved["id"], "asignaturaReceptoraId": subjects[1]}
        check("sin JWT", request(path, body)[0], 401)
        check("coordinador no suplanta docente", request(path, body, coord)[0], 403)
        check("docente ajeno", request(path, body, tokens["doc3.hu18"])[0], 404)
        check("no acepta cantidades del cliente", request(path, {**body, "horas": 100}, docente)[0], 400)
        status, incomplete = request(path, body, docente)
        check("sin revision HTTP", status, 200)
        check("sin revision no inventa equivalencia", incomplete["estado"], "Incompleto")
        def link(a, b):
            return {"elementoOrigen": a, "elementoReceptor": b, "ubicacionOrigen": "plan-O/U1", "ubicacionReceptora": "plan-R/U1"}
        revision = {"contenidos": [link(units[0]["contenidos"][0]["id"], units[1]["contenidos"][0]["id"])], "resultados": [link(units[0]["id"], units[1]["id"])]}
        check("revision explicita", request(f"/api/asignaturas/{subjects[0]}/correspondencias/{subjects[1]}", revision, admin, "PUT")[0], 200)
        status, result = request(path, body, docente)
        check("orquestacion HTTP real", status, 200)
        check("sin modelo no declara exito", result["estado"], "Fallido")
        check("sin modelo resultado nulo", result["resultado"], None)
        check("motor conserva porcentaje academico", result["evaluacion"]["compatibilidadAcademicaPct"], 100)
        check("motor conserva porcentaje horas", result["evaluacion"]["compatibilidadHorariaPct"], 100)
        check("datos sinteticos identificados", result["datosSinteticos"], True)
        check("no expone datos personales", "HU18-SINTETICO" in json.dumps(result), False)
        check("contexto Solicitudes no publicado", request(f"/api/solicitudes/{sid}/contexto-analisis-interno", token=docente)[0], 404)
        check("contexto Catalogo no publicado", request(f"/api/asignaturas/{subjects[0]}/analisis-interno/contexto", token=docente)[0], 404)
        (output / "resultado.json").write_text(json.dumps({"proyecto": project, "casos": checks, "transporte": "HTTP local aislado; no acredita HTTPS", "modelo": "deshabilitado, no acredita Ollama real"}, ensure_ascii=False, indent=2), encoding="utf-8")
    except Exception:
        diagnostic = run(compose + ["logs", "--no-color", "--tail", "100"])
        for value in settings.values():
            if value:
                diagnostic = diagnostic.replace(value, "[oculto]")
        (output / "servicios-error.log").write_text(diagnostic, encoding="utf-8")
        raise
    finally:
        cleanup = subprocess.run(compose + ["down", "--volumes", "--remove-orphans"], cwd=ROOT, env=environment, capture_output=True, timeout=120)
        if cleanup.returncode:
            print("Limpieza pendiente:", project)
        env_path.unlink(missing_ok=True)


if __name__ == "__main__":
    main()
