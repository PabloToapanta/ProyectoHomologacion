"""HU-37: reutilización del análisis del agente con Gateway/JWE/servicios/SQL reales y doble HTTP de Ollama.

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
    project = "hu37-check-" + secrets.token_hex(5)
    assert re.fullmatch(r"hu37-check-[0-9a-f]{10}", project)
    output = ROOT / "TestResults" / "hu37-integration"
    output.mkdir(parents=True, exist_ok=True)
    settings = {
        "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "AUTH_JWT_ISSUER": "hu37-isolated",
        "AUTH_JWT_AUDIENCE": "hu37-isolated-api",
        "AUTH_JWT_KEY": secrets.token_hex(32),
        "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
        "APP_SECRET_KEY": secrets.token_urlsafe(32),
        "AUTH_ADMIN_USERNAME": "admin.hu37",
        "AUTH_ADMIN_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "CATALOGO_DATABASE": "Hu37Catalogo",
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
    # El doble de Ollama vive solo en la red del proyecto temporal, sin publicar puertos.
    overlay = output / "compose-simulado.json"
    overlay.write_text(json.dumps({"services": {
        "ollama-simulado": {"image": "python:3.12-alpine", "command": ["python", "/fixture.py"],
            "volumes": [{"type": "bind", "source": str(ROOT / "tests/security/hu18-ollama-fake.py"), "target": "/fixture.py", "read_only": True}]},
        "homologacion-service": {"environment": {"AnalisisInterno__Habilitado": "true", "AnalisisInterno__OllamaUrl": "http://ollama-simulado:11434/",
            "AnalisisInterno__Modelo": "qwen3:4b", "AnalisisInterno__TimeoutSegundos": "10"}}
    }}), encoding="utf-8")
    compose = ["docker", "compose", "--env-file", str(env_path), "-p", project,
               "-f", "compose.sprint1.yaml", "-f", "compose.hu10.yaml", "-f", str(overlay)]
    checks = []

    def sanear(text):
        for value in private_values:
            if value:
                text = text.replace(value, "[oculto]")
        return text

    def run(args, timeout=120, permitir_fallo=False):
        result = subprocess.run(args, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=timeout)
        if result.returncode and not permitir_fallo:
            (output / "error.log").write_text(sanear(result.stdout + result.stderr), encoding="utf-8")
            raise RuntimeError("Comando Docker falló; diagnóstico saneado en TestResults/hu37-integration/error.log")
        return result if permitir_fallo else result.stdout

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
            with urlopen(req, timeout=30) as response:
                text = response.read().decode()
                assert settings["APP_SECRET_KEY"] not in text
                return response.status, json.loads(text) if text else None
        except HTTPError as error:
            return error.code, None

    def post(path, body, token, expected=201):
        status, value = request(path, body, token)
        check(path, status, expected)
        return value

    def sql(query, permitir_fallo=False):
        return run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
            'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAnalisis -b -h -1 -W -Q "SET NOCOUNT ON; ' + query + '"'],
            permitir_fallo=permitir_fallo)

    def filas():
        return int(sql("SELECT COUNT(*) FROM ExplicacionesAgente").strip().splitlines()[-1])

    try:
        print("Construyendo servicios aislados...", flush=True)
        run(compose + ["build", "auth-service", "catalogo-api", "homologacion-service", "solicitudes-service", "gateway"], 900)
        run(compose + ["up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "gateway", "ollama-simulado"], 240)
        base = "http://" + run(compose + ["port", "gateway", "8080"]).strip()
        deadline = time.monotonic() + 120
        while True:
            try:
                status, login = request("/api/auth/login", {"nombreUsuario": "admin.hu37", "password": settings["AUTH_ADMIN_PASSWORD"]})
                if status == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= deadline:
                raise RuntimeError("Login aislado no disponible")
            time.sleep(1)
        check("login JWE real", len(login["accessToken"].split(".")), 5)
        admin = login["accessToken"]
        actors = [("coord.hu37", 3), ("doc1.hu37", 2), ("doc2.hu37", 2), ("doc3.hu37", 2)]
        alta = "".join("INSERT INTO Usuarios (Id,NombreUsuario,NombreNormalizado,PasswordHash,Activo,RolId) "
                       f"SELECT NEWID(),N'{name}',N'{name.upper()}',PasswordHash,1,'00000000-0000-0000-0000-{role:012d}' "
                       "FROM Usuarios WHERE NombreNormalizado=N'ADMIN.HU37';" for name, role in actors)
        run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                       'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAuth -b -Q "' + alta + '"'])
        tokens = {name: post("/api/auth/login", {"nombreUsuario": name, "password": settings["AUTH_ADMIN_PASSWORD"]}, None, 200)["accessToken"] for name, _ in actors}
        coord, docente = tokens["coord.hu37"], tokens["doc1.hu37"]
        careers, subjects, units = [], [], []
        for suffix in ["O", "R"]:
            career = post("/api/carreras", {"nombre": "HU37 sintetica " + suffix, "facultad": "FISEI", "vigencia": True}, admin)
            careers.append(career["id"])
            subject = post("/api/asignaturas", {"carreraId": career["id"], "codigo": "HU37-" + suffix, "nombre": "Asignatura sintetica " + suffix, "nivel": 1, "creditos": 3, "horas": 100}, admin)
            subjects.append(subject["id"])
            units.append(post(f'/api/asignaturas/{subject["id"]}/unidades', {"numero": 1, "nombre": "Unidad sintetica", "horas": 100, "resultadoAprendizaje": "Resultado sintetico", "contenidos": ["Contenido sintetico"]}, admin))
            check("perfil sintetico", request(f'/api/asignaturas/{subject["id"]}/perfil-comparacion', {"planAnalitico": "plan-sintetico-" + suffix, "unidadHoras": "hora-reloj", "sistemaCreditos": "sistema-sintetico", "esSintetico": True}, admin, "PUT")[0], 200)

        def link(a, b, ubicacion="plan-R/U1"):
            return {"elementoOrigen": a, "elementoReceptor": b, "ubicacionOrigen": "plan-O/U1", "ubicacionReceptora": ubicacion}

        def revisar(ubicacion):
            revision = {"contenidos": [link(units[0]["contenidos"][0]["id"], units[1]["contenidos"][0]["id"], ubicacion)],
                        "resultados": [link(units[0]["id"], units[1]["id"], ubicacion)]}
            check("revision explicita " + ubicacion, request(f"/api/asignaturas/{subjects[0]}/correspondencias/{subjects[1]}", revision, admin, "PUT")[0], 200)

        revisar("plan-R/U1")

        def solicitud(n):
            application = post("/api/solicitudes", {"numeroQuipux": f"HU37-Q{n}", "identificacionEstudiante": f"HU37-E{n}", "nombreEstudiante": f"Estudiante sintetico {n}",
                                "tipoCambio": "Interno", "carreraOrigenId": careers[0], "carreraReceptoraId": careers[1]}, coord)
            sid = application["id"]
            approved = post(f"/api/solicitudes/{sid}/asignaturas-aprobadas", {"asignaturaOrigenId": subjects[0], "nota": 8, "fechaAprobacion": "2026-06-01",
                            "numeroMatricula": f"HU37-M{n}", "intentoMatricula": 1}, coord)
            status, disponibles = request(f"/api/solicitudes/{sid}/docentes-disponibles", token=coord)
            check("directorio docente real", status, 200)
            post(f"/api/solicitudes/{sid}/docentes", {"docenteIds": [t["id"] for t in disponibles if t["nombreUsuario"] in ["doc1.hu37", "doc2.hu37"]]}, coord, 200)
            return f"/api/homologaciones/solicitudes/{sid}/analisis-interno", {"asignaturaAprobadaId": approved["id"], "asignaturaReceptoraId": subjects[1]}

        path, body = solicitud(1)
        run(compose + ["up", "-d", "--no-build", "--wait", "--wait-timeout", "120", "homologacion-service"], 180)

        primero = post(path, body, docente, 200)
        check("CA1 primer análisis completo", primero["estado"], "Completo")
        check("CA1 primera explicación nueva", primero["agente"]["reutilizada"], False)
        check("CA1 plantilla declarada", primero["agente"]["versionPlantilla"], "hu18-v1")
        check("CA1 una fila guardada", filas(), 1)
        segundo = post(path, body, docente, 200)
        check("CA1 segunda ejecución reutiliza", segundo["agente"]["reutilizada"], True)
        check("CA1 id original", segundo["agente"]["explicacionOriginalId"], primero["agente"]["explicacionId"])
        check("CA1 motor recalculado", segundo["evaluacion"]["compatibilidadHorariaPct"], primero["evaluacion"]["compatibilidadHorariaPct"])
        check("CA1 sin fila nueva", filas(), 1)
        otra_path, otra_body = solicitud(2)
        otra = post(otra_path, otra_body, docente, 200)
        check("CA1 otra solicitud reutiliza", otra["agente"]["reutilizada"], True)
        guardado = request(path, token=docente)[1]
        check("bloque agente persistido en HU-20", guardado[0]["analisis"]["agente"]["reutilizada"], True)
        check("explicación sin datos del estudiante",
              sql("SELECT COUNT(*) FROM ExplicacionesAgente WHERE Explicacion LIKE N'%HU37-E%' OR Explicacion LIKE N'%Estudiante sintetico%'").strip().splitlines()[-1], "0")

        check("CA5 sin JWT", request(path, body)[0], 401)
        check("CA5 coordinador no fuerza", request(path, {**body, "forzarNuevo": True, "motivo": "Motivo sintetico"}, coord)[0], 403)
        check("CA5 docente ajeno no reutiliza", request(path, body, tokens["doc3.hu37"])[0], 404)
        check("CA4 forzar sin motivo", request(path, {**body, "forzarNuevo": True}, docente)[0], 400)
        check("CA4 motivo sin forzar", request(path, {**body, "motivo": "Motivo sintetico"}, docente)[0], 400)
        forzado = post(path, {**body, "forzarNuevo": True, "motivo": "Revision sintetica del programa"}, docente, 200)
        check("CA4 forzar crea explicación nueva", forzado["agente"]["reutilizada"], False)
        check("CA4 conserva la anterior", filas(), 2)
        check("CA4 motivo registrado", sql("SELECT Motivo FROM ExplicacionesAgente WHERE ForzadoPorId IS NOT NULL").strip().splitlines()[-1], "Revision sintetica del programa")
        luego = post(path, body, docente, 200)
        check("CA4 luego reutiliza la más reciente", luego["agente"]["explicacionOriginalId"], forzado["agente"]["explicacionId"])

        revisar("plan-R/U2")
        cambio = post(path, body, docente, 200)
        check("CA2 cambio de correspondencia llama al agente", cambio["agente"]["reutilizada"], False)
        check("CA2 nueva clave guardada", filas(), 3)

        actualizar = sql("UPDATE ExplicacionesAgente SET Modelo = N'otro'", permitir_fallo=True)
        check("trigger impide UPDATE", actualizar.returncode != 0, True)
        borrar = sql("DELETE FROM ExplicacionesAgente", permitir_fallo=True)
        check("trigger impide DELETE", borrar.returncode != 0, True)
        check("filas intactas", filas(), 3)

        run(compose + ["stop", "ollama-simulado"])
        sin_modelo = post(path, body, docente, 200)
        check("reutiliza sin el modelo disponible", (sin_modelo["estado"], sin_modelo["agente"]["reutilizada"]), ("Completo", True))
        fallido = post(path, {**body, "forzarNuevo": True, "motivo": "Forzado sin modelo"}, docente, 200)
        check("CA3 forzar sin modelo queda Fallido", fallido["estado"], "Fallido")
        check("CA3 Fallido sin bloque agente", fallido["agente"], None)
        check("CA3 Fallido no se guarda", filas(), 3)
        (output / "resultado.json").write_text(json.dumps({"proyecto": project, "casos": checks, "transporte": "HTTP local aislado; no acredita HTTPS",
            "modelo": "doble HTTP qwen3:4b, sin inferencia real"}, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"HU-37 smoke: {len(checks)} comprobaciones superadas", flush=True)
    except Exception:
        diagnostic = run(compose + ["logs", "--no-color", "--tail", "100"], permitir_fallo=True)
        (output / "servicios-error.log").write_text(sanear(diagnostic.stdout + diagnostic.stderr), encoding="utf-8")
        raise
    finally:
        cleanup = subprocess.run(compose + ["down", "--volumes", "--remove-orphans"], cwd=ROOT, env=environment, capture_output=True, timeout=120)
        if cleanup.returncode:
            print("Limpieza pendiente:", project)
        env_path.unlink(missing_ok=True)


if __name__ == "__main__":
    main()
