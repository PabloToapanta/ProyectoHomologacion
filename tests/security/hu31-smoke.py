"""HU-31: análisis propio de los docentes sobre un cambio externo, con Gateway/JWE/servicios/SQL reales.

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
    project = "hu31-check-" + secrets.token_hex(5)
    assert re.fullmatch(r"hu31-check-[0-9a-f]{10}", project)
    output = ROOT / "TestResults" / "hu31-integration"
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
            raise RuntimeError("Comando Docker falló; diagnóstico saneado en TestResults/hu31-integration/error.log")
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
        check("analisis del agente externo", request(path, body, docente)[1]["estado"], "Incompleto")
        rev = f"/api/homologaciones/solicitudes/{sid}/revisiones-docentes"
        segundo = tokens["doc2.hu19"]
        check("sin JWT", request(rev, {**body, "postura": "Ajuste"}, method="PUT")[0], 401)
        check("coordinador no registra", request(rev, {**body, "postura": "Ajuste", "decision": "Homologable", "observacion": "x"}, coord, "PUT")[0], 403)
        check("docente ajeno no registra", request(rev, {**body, "postura": "Ajuste", "decision": "Homologable", "observacion": "x"}, ajeno, "PUT")[0], 404)
        check("de acuerdo sin sugerencia rechazado", request(rev, {**body, "postura": "DeAcuerdo"}, docente, "PUT")[0], 400)
        check("ajuste sin observacion rechazado", request(rev, {**body, "postura": "Ajuste", "decision": "NoHomologable"}, docente, "PUT")[0], 400)
        check("no suplanta a otro docente", request(rev, {**body, "postura": "Ajuste", "decision": "Homologable", "observacion": "x", "docenteId": str(secrets.token_hex(16))}, docente, "PUT")[0], 400)
        status, propio = request(rev, {**body, "postura": "Ajuste", "decision": "NoHomologable", "observacion": "Faltan contenidos sinteticos."}, docente, "PUT")
        check("ajuste del primer docente", (status, propio["decision"], propio["estadoAgente"], propio["docente"]), (200, "NoHomologable", "Incompleto", "doc1.hu19"))
        status, otro = request(rev, {**body, "postura": "Ajuste", "decision": "Homologable", "observacion": "Coincide en lo esencial."}, segundo, "PUT")
        check("ajuste del segundo docente", (status, otro["docente"]), (200, "doc2.hu19"))
        status, corregido = request(rev, {**body, "postura": "Ajuste", "decision": "Homologable", "observacion": "Corrijo tras revisar."}, docente, "PUT")
        check("correccion del propio", (status, corregido["decision"]), (200, "Homologable"))
        status, lista = request(rev, token=segundo)
        check("ambos docentes se ven", (status, sorted(r["docente"] for r in lista)), (200, ["doc1.hu19", "doc2.hu19"]))
        check("lectura ajena denegada", request(rev, token=ajeno)[0], 404)
        run(compose + ["restart", "homologacion-service"])
        limit = time.monotonic() + 60
        while True:
            try:
                code, saved = request(rev, token=docente)
                if code == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= limit:
                raise RuntimeError("No fue posible recuperar las revisiones tras reiniciar")
            time.sleep(1)
        check("revisiones conservadas tras reinicio", sorted((r["docente"], r["observacion"]) for r in saved),
              [("doc1.hu19", "Corrijo tras revisar."), ("doc2.hu19", "Coincide en lo esencial.")])
        def sql_homologacion(query):
            return run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAnalisis -b -Q "' + query + '"'])
        sql_homologacion("CREATE TRIGGER HU31_FalloPrueba ON RevisionesDocentes AFTER UPDATE AS THROW 51000, 'Fallo sintetico de persistencia', 1;")
        check("fallo de almacenamiento controlado", request(rev, {**body, "postura": "Ajuste", "decision": "NoHomologable", "observacion": "No debe guardarse."}, docente, "PUT")[0], 503)
        check("fallo no reemplaza el registro", sorted((r["docente"], r["observacion"]) for r in request(rev, token=docente)[1]),
              [("doc1.hu19", "Corrijo tras revisar."), ("doc2.hu19", "Coincide en lo esencial.")])
        sql_homologacion("DROP TRIGGER HU31_FalloPrueba;")
        (output / "resultado.json").write_text(json.dumps({"proyecto": project, "casos": checks, "transporte": "HTTP local aislado; no acredita HTTPS"},
                                                          ensure_ascii=False, indent=2), encoding="utf-8")
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
