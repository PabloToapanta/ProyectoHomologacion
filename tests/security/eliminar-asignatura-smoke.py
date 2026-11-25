"""Eliminar asignatura (HU-05): borrado en cascada controlado con Gateway/JWE/servicios/SQL Server reales.

Proyecto Docker aislado y aleatorio, con contraseñas y claves aleatorias que no se guardan.
No lee .env existentes. HTTP local aislado: no acredita HTTPS. Solo elimina el proyecto creado por esta ejecución.
PDF sintético generado en la prueba.
"""

import base64
from http.client import RemoteDisconnected
import hashlib
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
PDF = b"%PDF-1.4\n% normativa sintetica HU-38\n%%EOF\n"


def main():
    project = "hu38-check-" + secrets.token_hex(5)
    assert re.fullmatch(r"hu38-check-[0-9a-f]{10}", project)
    output = ROOT / "TestResults" / "hu05-eliminar-integration"
    output.mkdir(parents=True, exist_ok=True)
    settings = {
        "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20), "AUTH_JWT_ISSUER": "hu38-isolated",
        "AUTH_JWT_AUDIENCE": "hu38-isolated-api", "AUTH_JWT_KEY": secrets.token_hex(32),
        "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
        "APP_SECRET_KEY": secrets.token_urlsafe(32), "AUTH_ADMIN_USERNAME": "admin.hu38",
        "AUTH_ADMIN_PASSWORD": "Aa1!" + secrets.token_hex(20), "CATALOGO_DATABASE": "Hu38Catalogo",
        "AUTH_SERVICE_URL": "http://auth-service:8080/", "CATALOGO_SERVICE_URL": "http://catalogo-api:8080/",
        "HOMOLOGACION_SERVICE_URL": "http://homologacion-service:8080/", "GATEWAY_HTTP_PORT": "0", "FRONTEND_HTTP_PORT": "0",
        "ANALISIS_INTERNO_HABILITADO": "false", "OLLAMA_URL": "", "OLLAMA_MODELO": "",
    }
    environment = {**os.environ, **settings}
    private_values = [settings[k] for k in ["AUTH_SQL_PASSWORD", "AUTH_JWT_KEY", "AUTH_JWT_ENCRYPTION_KEY", "APP_SECRET_KEY", "AUTH_ADMIN_PASSWORD"]]
    env_path = ROOT / (".env." + project)
    env_path.write_text("\n".join(f"{k}={v}" for k, v in settings.items()), encoding="utf-8")
    compose = ["docker", "compose", "--env-file", str(env_path), "-p", project, "-f", "compose.sprint1.yaml", "-f", "compose.hu10.yaml"]
    checks = []
    base = ""

    def sanear(text):
        for value in private_values:
            text = text.replace(value, "[oculto]")
        return text

    def run(args, timeout=120, permitir_fallo=False):
        result = subprocess.run(args, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=timeout)
        if result.returncode and not permitir_fallo:
            (output / "error.log").write_text(sanear(result.stdout + result.stderr), encoding="utf-8")
            raise RuntimeError("Comando Docker falló; diagnóstico saneado en TestResults/hu05-eliminar-integration/error.log")
        return result if permitir_fallo else result.stdout

    def check(name, actual, expected):
        assert actual == expected, f"{name}: esperado {expected!r}, obtenido {actual!r}"
        checks.append({"caso": name, "resultado": json.loads(json.dumps(actual, default=lambda v: f"{len(v)} bytes" if isinstance(v, bytes) else str(v)))})
        print("PASS", name, flush=True)

    def request(path, body=None, token=None, method=None, raw=None, content_type="application/json", headers=None):
        h = {"Content-Type": content_type, **(headers or {})}
        if token:
            h["Authorization"] = "Bearer " + token
        data = raw if raw is not None else (None if body is None else json.dumps(body).encode())
        try:
            with urlopen(Request(base + path, data=data, headers=h, method=method), timeout=30) as response:
                contenido = response.read()
                return response.status, contenido, dict(response.headers)
        except HTTPError as error:
            return error.code, error.read(), dict(error.headers)

    def as_json(contenido):
        return json.loads(contenido) if contenido else None

    def multipart(campos, archivo):
        frontera = "hu38" + secrets.token_hex(8)
        partes = []
        for nombre, valor in campos.items():
            partes.append(f'--{frontera}\r\nContent-Disposition: form-data; name="{nombre}"\r\n\r\n{valor}\r\n'.encode())
        partes.append(f'--{frontera}\r\nContent-Disposition: form-data; name="archivo"; filename="instructivo.pdf"\r\n'
                      f"Content-Type: application/pdf\r\n\r\n".encode() + archivo + b"\r\n")
        partes.append(f"--{frontera}--\r\n".encode())
        return b"".join(partes), "multipart/form-data; boundary=" + frontera

    def registrar(token, codigo, reforma_a=None, vigente="2023-03-02", esperado=201):
        campos = {"codigo": codigo, "titulo": "Instructivo sintetico " + codigo, "emisor": "Consejo sintetico",
                  "fechaEmision": "2023-03-01", "vigenteDesde": vigente}
        if reforma_a:
            campos["reformaAId"] = reforma_a
        cuerpo, tipo = multipart(campos, PDF)
        status, contenido, _ = request("/api/normativas", token=token, method="POST", raw=cuerpo, content_type=tipo)
        check("registrar " + codigo, status, esperado)
        return as_json(contenido)

    def sql(base_datos, query, permitir_fallo=False):
        return run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
            f'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d {base_datos} -b -h -1 -W -Q "SET NOCOUNT ON; ' + query + '"'],
            permitir_fallo=permitir_fallo)

    try:
        print("Construyendo servicios aislados...", flush=True)
        run(compose + ["build", "auth-service", "catalogo-api", "homologacion-service", "solicitudes-service", "gateway"], 900)
        run(compose + ["up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "gateway"], 240)
        base = "http://" + run(compose + ["port", "gateway", "8080"]).strip()
        limite = time.monotonic() + 120
        while True:
            try:
                status, contenido, _ = request("/api/auth/login", {"nombreUsuario": "admin.hu38", "password": settings["AUTH_ADMIN_PASSWORD"]}, method="POST")
                if status == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= limite:
                raise RuntimeError("Login aislado no disponible")
            time.sleep(1)
        admin = as_json(contenido)["accessToken"]
        actores = [("coord.hu38", 3), ("doc.hu38", 2)]
        alta = "".join("INSERT INTO Usuarios (Id,NombreUsuario,NombreNormalizado,PasswordHash,Activo,RolId) "
                       f"SELECT NEWID(),N'{n}',N'{n.upper()}',PasswordHash,1,'00000000-0000-0000-0000-{r:012d}' "
                       "FROM Usuarios WHERE NombreNormalizado=N'ADMIN.HU38';" for n, r in actores)
        sql("HomologacionAuth", alta)
        tokens = {}
        for n, _ in actores:
            status, contenido, _ = request("/api/auth/login", {"nombreUsuario": n, "password": settings["AUTH_ADMIN_PASSWORD"]}, method="POST")
            check("login " + n, status, 200)
            tokens[n] = as_json(contenido)["accessToken"]
        coord, docente = tokens["coord.hu38"], tokens["doc.hu38"]

        def crear(path, body, esperado=201, metodo="POST"):
            status, contenido, _ = request(path, body, admin, metodo)
            check(f"{metodo} {path}", status, esperado)
            return as_json(contenido)
        carrera = crear("/api/carreras", {"nombre": "Software sintetica", "facultad": "FISEI", "vigencia": True})
        asignaturas = {}
        for codigo, horas in (("ALG-SINT", 3), ("OTRA-SINT", 144)):
            a = crear("/api/asignaturas", {"carreraId": carrera["id"], "codigo": codigo, "nombre": codigo, "nivel": 1, "creditos": 3, "horas": horas})
            crear(f"/api/asignaturas/{a['id']}/unidades", {"numero": 1, "nombre": "Unidad sintetica", "horas": horas,
                  "resultadoAprendizaje": "Resultado sintetico", "contenidos": ["Tema A", "Tema B"]})
            crear(f"/api/asignaturas/{a['id']}/perfil-comparacion", {"planAnalitico": "plan-" + codigo, "unidadHoras": "hora-reloj",
                  "sistemaCreditos": "sintetico", "esSintetico": True}, 200, "PUT")
            asignaturas[codigo] = as_json(request(f"/api/asignaturas/{a['id']}", token=admin)[1])
        o, r = asignaturas["OTRA-SINT"], asignaturas["ALG-SINT"]
        vinculo = {"elementoOrigen": o["unidades"][0]["contenidos"][0]["id"], "elementoReceptor": r["unidades"][0]["contenidos"][0]["id"],
                   "ubicacionOrigen": "U1", "ubicacionReceptora": "U1"}
        crear(f"/api/asignaturas/{o['id']}/correspondencias/{r['id']}", {"contenidos": [vinculo], "resultados": []}, 200, "PUT")

        check("Coordinador no elimina", request(f"/api/asignaturas/{r['id']}", token=coord, method="DELETE")[0], 403)
        check("Administrador elimina con unidades, perfil y correspondencia (SQL Server)", request(f"/api/asignaturas/{r['id']}", token=admin, method="DELETE")[0], 204)
        check("ya no existe", request(f"/api/asignaturas/{r['id']}", token=admin)[0], 404)
        filas = sql("Hu38Catalogo", f"SELECT (SELECT COUNT(*) FROM UnidadesCurriculares WHERE AsignaturaId='{r['id']}') + (SELECT COUNT(*) FROM PerfilesComparacion WHERE AsignaturaId='{r['id']}') + (SELECT COUNT(*) FROM RevisionesComparacion WHERE ReceptoraId='{r['id']}' OR OrigenId='{r['id']}')").strip().splitlines()[-1]
        check("sin filas huérfanas", filas, "0")
        otra = as_json(request(f"/api/asignaturas/{o['id']}", token=admin)[1])
        check("la otra conserva sus unidades", len(otra["unidades"]), 1)
        crear("/api/asignaturas", {"carreraId": carrera["id"], "codigo": "ALG-SINT", "nombre": "ALGEBRA LINEAL", "nivel": 1, "creditos": 3, "horas": 144})
        (output / "resultado.json").write_text(json.dumps({"proyecto": project, "casos": checks,
            "transporte": "HTTP local aislado; no acredita HTTPS"}, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"Eliminar asignatura smoke: {len(checks)} comprobaciones superadas", flush=True)
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
