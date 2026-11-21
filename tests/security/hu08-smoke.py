"""HU-08: carga masiva de programas analíticos con Gateway/JWE/Catálogo/SQL Server reales.

Credenciales aleatorias por ejecución; no lee .env existentes. HTTP local aislado: no acredita HTTPS.
Usa PDF sintéticos generados aquí. Si se define HU08_REAL_DIR (solo en un equipo local), además analiza
esos PDF reales sin confirmarlos y registra únicamente conteos: el repositorio es público.
Solo elimina el proyecto aleatorio creado por esta ejecución.
"""

import base64
import hashlib
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


def pdf_sintetico(lineas):
    """PDF mínimo con Helvetica/WinAnsi; lineas = [(pagina, x, y, texto)] con la disposición del plan FISEI."""
    paginas = max(p for p, *_ in lineas)
    objetos = [b"<< /Type /Catalog /Pages 2 0 R >>",
               ("<< /Type /Pages /Kids [" + " ".join(f"{4 + i * 2} 0 R" for i in range(paginas)) + f"] /Count {paginas} >>").encode(),
               b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"]
    for n in range(1, paginas + 1):
        flujo = ""
        for p, x, y, texto in lineas:
            if p != n:
                continue
            cx = x
            for palabra in texto.split():
                limpia = palabra.replace("\\", "\\\\").replace("(", "\\(").replace(")", "\\)")
                flujo += f"BT /F1 7 Tf {cx:.2f} {y:.2f} Td ({limpia}) Tj ET\n"
                cx += len(palabra) * 4.6 + 3
        contenido = flujo.encode("latin-1")
        objetos.append(f"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {5 + (n - 1) * 2} 0 R >>".encode())
        objetos.append(f"<< /Length {len(contenido)} >>\nstream\n".encode() + contenido + b"\nendstream")
    salida = bytearray(b"%PDF-1.4\n")
    posiciones = []
    for i, objeto in enumerate(objetos):
        posiciones.append(len(salida))
        salida += f"{i + 1} 0 obj\n".encode() + objeto + b"\nendobj\n"
    xref = len(salida)
    salida += f"xref\n0 {len(objetos) + 1}\n0000000000 65535 f \n".encode()
    salida += b"".join(f"{p:010d} 00000 n \n".encode() for p in posiciones)
    salida += f"trailer\n<< /Size {len(objetos) + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n".encode()
    return bytes(salida)


def plan(codigo):
    return [
        (1, 30, 760, "Nombre de la asignatura"), (1, 30, 750, "ASIGNATURA SINTETICA HU08"), (1, 30, 740, "Carrera"), (1, 30, 730, "Software"),
        (1, 30, 708, f"Código {codigo}"), (1, 323, 708, "PRERREQUISITOS"), (1, 30, 620, "Créditos/Horas"), (1, 30, 610, "3"),
        (1, 30, 600, "Nivel"), (1, 30, 590, "SEGUNDO"), (2, 30, 700, "TOTAL DE HORAS AL SEMESTRE"), (2, 400, 700, "144"),
        (4, 257, 777, "III CONTENIDO"), (4, 38, 756, "Unidades curriculares"), (4, 157, 756, "Unidades temáticas"),
        (4, 282, 756, "Contenidos mínimos"), (4, 393, 756, "No horas"), (4, 461, 756, "Resultados de"), (4, 464, 746, "aprendizaje"),
        (4, 72, 732, "U.1"), (4, 132, 732, "Tema sintetico uno"), (4, 260, 732, "Contenido sintetico A."), (4, 406, 732, "72"),
        (4, 439, 732, "Resultado sintetico uno."), (4, 132, 679, "Tema sintetico dos"), (4, 260, 679, "Contenido sintetico B."),
        (4, 72, 616, "U.2"), (4, 132, 616, "Tema sintetico tres"), (4, 260, 616, "Contenido sintetico C."), (4, 406, 616, "72"),
        (4, 447, 616, "Resultado sintetico dos."), (4, 267, 358, "Resultado general sintetico"), (4, 61, 348, "Resultados del aprendizaje"),
        (4, 36, 326, "Total de horas"), (4, 393, 326, "144"), (4, 465, 49, "Página 5/9"),
    ]


def multipart(campos, archivos):
    limite = "hu08" + secrets.token_hex(8)
    cuerpo = bytearray()
    for nombre, valor in campos:
        cuerpo += f'--{limite}\r\nContent-Disposition: form-data; name="{nombre}"\r\n\r\n{valor}\r\n'.encode()
    for campo, nombre, datos in archivos:
        cuerpo += f'--{limite}\r\nContent-Disposition: form-data; name="{campo}"; filename="{nombre}"\r\nContent-Type: application/pdf\r\n\r\n'.encode() + datos + b"\r\n"
    cuerpo += f"--{limite}--\r\n".encode()
    return bytes(cuerpo), "multipart/form-data; boundary=" + limite


def main():
    project = "hu08-check-" + secrets.token_hex(5)
    assert re.fullmatch(r"hu08-check-[0-9a-f]{10}", project)
    output = ROOT / "TestResults" / "hu08-integration"
    output.mkdir(parents=True, exist_ok=True)
    settings = {
        "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20), "AUTH_JWT_ISSUER": "hu08-isolated", "AUTH_JWT_AUDIENCE": "hu08-isolated-api",
        "AUTH_JWT_KEY": secrets.token_hex(32), "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
        "APP_SECRET_KEY": secrets.token_urlsafe(32), "AUTH_ADMIN_USERNAME": "admin.hu08", "AUTH_ADMIN_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "CATALOGO_DATABASE": "Hu08Catalogo", "AUTH_SERVICE_URL": "http://auth-service:8080/", "CATALOGO_SERVICE_URL": "http://catalogo-api:8080/",
        "HOMOLOGACION_SERVICE_URL": "http://homologacion-service:8080/", "GATEWAY_HTTP_PORT": "0", "FRONTEND_HTTP_PORT": "0",
        "ANALISIS_INTERNO_HABILITADO": "false", "OLLAMA_URL": "", "OLLAMA_MODELO": "",
    }
    environment = {**os.environ, **settings}
    private_values = [settings[k] for k in ["AUTH_SQL_PASSWORD", "AUTH_JWT_KEY", "AUTH_JWT_ENCRYPTION_KEY", "APP_SECRET_KEY", "AUTH_ADMIN_PASSWORD"]]
    env_path = ROOT / (".env." + project)
    env_path.write_text("\n".join(f"{k}={v}" for k, v in settings.items()), encoding="utf-8")
    compose = ["docker", "compose", "--env-file", str(env_path), "-p", project, "-f", "compose.sprint1.yaml", "-f", "compose.hu10.yaml"]
    checks = []

    def sanear(texto):
        for value in private_values:
            texto = texto.replace(value, "[oculto]") if value else texto
        return texto

    def run(args, timeout=120):
        result = subprocess.run(args, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=timeout)
        if result.returncode:
            (output / "error.log").write_text(sanear(result.stdout + result.stderr), encoding="utf-8")
            raise RuntimeError("Comando Docker falló; diagnóstico saneado en TestResults/hu08-integration/error.log")
        return result.stdout

    def check(name, actual, expected):
        assert actual == expected, f"{name}: esperado {expected}, obtenido {actual}"
        checks.append({"caso": name, "resultado": actual})
        print("PASS", name, flush=True)

    def request(path, body=None, token=None, method=None, content_type="application/json", raw=False):
        headers = {"Content-Type": content_type}
        if token:
            headers["Authorization"] = "Bearer " + token
        data = None if body is None else (body if isinstance(body, bytes) else json.dumps(body).encode())
        req = Request(base + path, data=data, headers=headers, method=method)
        try:
            with urlopen(req, timeout=30) as response:
                contenido = response.read()
                if raw:
                    return response.status, contenido, dict(response.headers)
                texto = contenido.decode()
                assert settings["APP_SECRET_KEY"] not in texto
                return response.status, json.loads(texto) if texto else None
        except HTTPError as error:
            return (error.code, None, {}) if raw else (error.code, None)

    def upload(path, campos, archivos, token):
        cuerpo, tipo = multipart(campos, archivos)
        return request(path, cuerpo, token, "POST", tipo)

    try:
        print("Construyendo servicios aislados...", flush=True)
        run(compose + ["build", "auth-service", "catalogo-api", "homologacion-service", "solicitudes-service", "gateway"], 900)
        run(compose + ["up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "gateway"], 240)
        base = "http://" + run(compose + ["port", "gateway", "8080"]).strip()
        deadline = time.monotonic() + 120
        while True:
            try:
                status, login = request("/api/auth/login", {"nombreUsuario": "admin.hu08", "password": settings["AUTH_ADMIN_PASSWORD"]})
                if status == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= deadline:
                raise RuntimeError("Login aislado no disponible")
            time.sleep(1)
        admin = login["accessToken"]
        actors = [("coord.hu08", 3), ("doc.hu08", 2)]
        sql = "".join("INSERT INTO Usuarios (Id,NombreUsuario,NombreNormalizado,PasswordHash,Activo,RolId) "
                      f"SELECT NEWID(),N'{name}',N'{name.upper()}',PasswordHash,1,'00000000-0000-0000-0000-{role:012d}' "
                      "FROM Usuarios WHERE NombreNormalizado=N'ADMIN.HU08';" for name, role in actors)
        run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                       'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAuth -b -Q "' + sql + '"'])
        tokens = {name: request("/api/auth/login", {"nombreUsuario": name, "password": settings["AUTH_ADMIN_PASSWORD"]})[1]["accessToken"] for name, _ in actors}

        def sql_catalogo(query, conteo=True):
            # CREATE TRIGGER debe ser la primera instrucción de su lote: sin prefijo en ese caso.
            texto = ("SET NOCOUNT ON; " if conteo else "") + query
            return run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d Hu08Catalogo -h -1 -b -Q "' + texto + '"']).split()

        status, carrera = request("/api/carreras", {"nombre": "HU08 Software sintetica", "facultad": "FISEI", "vigencia": True}, admin)
        check("carrera sintetica", status, 201)
        cid = carrera["id"]
        pdf = pdf_sintetico(plan("HU08-SINT-01"))
        sin_texto = pdf_sintetico([(1, 30, 700, "")])
        lote = [("archivos", "plan.pdf", pdf), ("archivos", "escaneado.pdf", sin_texto), ("archivos", "disfrazado.pdf", b"no es un pdf"),
                ("archivos", "grande.pdf", b"%PDF-" + b"0" * (5 * 1024 * 1024))]
        analisis = "/api/importaciones-programas/analisis"
        check("sin JWT", upload(analisis, [("carreraId", cid)], lote[:1], None)[0], 401)
        check("docente no carga", upload(analisis, [("carreraId", cid)], lote[:1], tokens["doc.hu08"])[0], 403)
        check("coordinador no carga", upload(analisis, [("carreraId", cid)], lote[:1], tokens["coord.hu08"])[0], 403)
        status, resultado = upload(analisis, [("carreraId", cid)], lote, admin)
        check("lote HTTP real", status, 200)
        check("un resultado por archivo", [a["estado"] for a in resultado["archivos"]], ["Borrador", "SinTexto", "Invalido", "Invalido"])
        borrador = resultado["archivos"][0]["borrador"]
        check("borrador extraido", (borrador["codigo"], borrador["nivel"], borrador["creditos"], borrador["horas"], len(borrador["unidades"])), ("HU08-SINT-01", 2, 3, 144, 2))
        check("analisis no guarda", sql_catalogo("SELECT COUNT(*) FROM Asignaturas;"), ["0"])

        programa = {"carreraId": cid, "codigo": borrador["codigo"], "nombre": "Asignatura sintetica HU08", "nivel": borrador["nivel"],
                    "creditos": borrador["creditos"], "horas": borrador["horas"],
                    "unidades": [{"numero": u["numero"], "nombre": "Unidad revisada " + str(u["numero"]), "horas": u["horas"],
                                  "resultadoAprendizaje": u["resultadoAprendizaje"], "tematicas": u["tematicas"]} for u in borrador["unidades"]]}
        confirmacion = "/api/importaciones-programas/confirmacion"
        status, creada = upload(confirmacion, [("programa", json.dumps(programa))], [("archivo", "plan.pdf", pdf)], admin)
        check("confirmacion guarda", status, 201)
        status, detalle = request(f"/api/asignaturas/{creada['id']}", token=admin)
        check("unidades y tematicas en catalogo", (status, len(detalle["unidades"]), len(detalle["unidades"][0]["tematicas"])), (200, 2, 2))
        check("codigo existente no se sobrescribe", upload(confirmacion, [("programa", json.dumps(programa))], [("archivo", "plan.pdf", pdf)], admin)[0], 409)

        run(compose + ["restart", "catalogo-api"])
        limit = time.monotonic() + 60
        while True:
            try:
                status, descargado, cabeceras = request(f"/api/asignaturas/{creada['id']}/programa-analitico", token=admin, raw=True)
                if status == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= limit:
                raise RuntimeError("Catálogo no respondió tras reiniciar")
            time.sleep(1)
        check("PDF original conservado tras reinicio", descargado == pdf, True)
        check("huella SHA-256", cabeceras.get("X-Content-SHA256"), hashlib.sha256(pdf).hexdigest().upper())
        check("usuario que cargo", cabeceras.get("X-Cargado-Por"), "admin.hu08")
        check("docente no descarga", request(f"/api/asignaturas/{creada['id']}/programa-analitico", token=tokens["doc.hu08"], raw=True)[0], 403)

        # Un fallo al guardar el PDF deshace también la asignatura y sus unidades.
        sql_catalogo("CREATE TRIGGER HU08_FalloPrueba ON ProgramasAnaliticos AFTER INSERT AS THROW 51000, 'Fallo sintetico', 1;", conteo=False)
        otro = {**programa, "codigo": "HU08-SINT-02"}
        status = upload(confirmacion, [("programa", json.dumps(otro))], [("archivo", "plan.pdf", pdf)], admin)[0]
        check("fallo de guardado no confirma", status >= 500, True)
        check("transaccion sin asignatura huerfana", sql_catalogo("SELECT COUNT(*) FROM Asignaturas; SELECT COUNT(*) FROM UnidadesCurriculares; SELECT COUNT(*) FROM ProgramasAnaliticos;"), ["1", "2", "1"])
        sql_catalogo("DROP TRIGGER HU08_FalloPrueba;", conteo=False)

        reales = {}
        directorio = os.environ.get("HU08_REAL_DIR")
        if directorio:
            archivos = sorted(Path(directorio).glob("*.pdf"))
            estados = {"Borrador completo": 0, "Borrador con faltantes": 0, "SinTexto": 0, "Invalido": 0}
            for i in range(0, len(archivos), 20):
                status, parcial = upload(analisis, [("carreraId", cid)], [("archivos", f"real-{i + j}.pdf", a.read_bytes()) for j, a in enumerate(archivos[i:i + 20])], admin)
                check(f"lote real {i // 20 + 1}", status, 200)
                for a in parcial["archivos"]:
                    clave = a["estado"] if a["estado"] != "Borrador" else ("Borrador completo" if not a["borrador"]["faltantes"] else "Borrador con faltantes")
                    estados[clave] += 1
            reales = {"archivos": len(archivos), **estados}
            print("Planes reales (solo conteos):", reales, flush=True)
        (output / "resultado.json").write_text(json.dumps({"proyecto": project, "casos": checks, "planesReales": reales,
                                                          "transporte": "HTTP local aislado; no acredita HTTPS"}, ensure_ascii=False, indent=2), encoding="utf-8")
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
