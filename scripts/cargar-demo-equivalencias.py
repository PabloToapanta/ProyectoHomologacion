"""Carga explícita e idempotente de datos SINTÉTICOS HU-14 / Issue #5.
No contiene programas oficiales ni modifica registros históricos. Solo biblioteca estándar.
Uso: python scripts/cargar-demo-equivalencias.py --env-file .env.hu05-validacion
"""
import argparse
import json
from pathlib import Path
from urllib.request import Request, urlopen

CARRERAS = [
    ("SW", "Ingeniería de Software", "Diseño de Software", ["Requisitos", "Modelado UML", "Arquitectura en capas", "Patrones de diseño", "Pruebas unitarias", "Control de versiones"]),
    ("TI", "Ingeniería en Tecnologías de la Información", "Administración de Sistemas", ["Procesos Linux", "Permisos de archivos", "Servicios de red", "Virtualización", "Respaldos", "Monitoreo"]),
    ("IND", "Ingeniería Industrial", "Gestión de Procesos Industriales", ["Diagramas de procesos", "Estudio de tiempos", "Balance de líneas", "Control de calidad", "Inventarios", "Mejora continua"]),
    ("ROB", "Ingeniería en Robótica y Automatización", "Control y Robótica", ["Sensores", "Actuadores", "Control PID", "Cinemática directa", "Cinemática inversa", "Programación de robots"]),
    ("TEL", "Ingeniería en Telecomunicaciones", "Señales y Comunicaciones", ["Señales continuas", "Señales discretas", "Muestreo", "Transformada de Fourier", "Modulación", "Canales de comunicación"]),
]
ALG = ["Vectores y espacios vectoriales", "Operaciones con matrices", "Sistemas de ecuaciones lineales", "Determinantes", "Valores y vectores propios", "Transformaciones lineales"]
PROG = ["Variables y tipos de datos", "Operadores y expresiones", "Estructuras condicionales", "Estructuras repetitivas", "Funciones", "Arreglos"]
RESULTADOS = {"ALG": ["Resolver sistemas de ecuaciones mediante operaciones matriciales", "Representar transformaciones lineales en espacios vectoriales"],
              "PROG": ["Construir algoritmos con decisiones y repeticiones", "Descomponer un problema en funciones y estructuras de datos"]}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--env-file", required=True, type=Path)
    args = parser.parse_args()
    settings = dict(line.strip().split("=", 1) for line in args.env_file.read_text(encoding="utf-8-sig").splitlines() if line.strip() and not line.startswith("#"))
    base = "http://127.0.0.1:" + settings.get("GATEWAY_HTTP_PORT", "5080")
    token = None

    def api(path, body=None, method=None):
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        req = Request(base + path, data=json.dumps(body).encode() if body is not None else None, headers=headers, method=method)
        with urlopen(req, timeout=30) as response:
            data = response.read()
            return json.loads(data) if data else None

    token = api("/api/auth/login", {"nombreUsuario": settings["AUTH_ADMIN_USERNAME"], "password": settings["AUTH_ADMIN_PASSWORD"]})["accessToken"]
    carreras = api("/api/carreras")
    materias = []
    for prefijo, nombre_carrera, especialidad, temas_especialidad in CARRERAS:
        carrera = next((c for c in carreras if c["nombre"].casefold() == nombre_carrera.casefold() and c["facultad"] == "FISEI"), None)
        if carrera is None:
            carrera = api("/api/carreras", {"nombre": nombre_carrera, "facultad": "FISEI", "vigencia": True, "modalidad": "Presencial"})
        listado = api(f'/api/carreras/{carrera["id"]}/asignaturas')
        for familia, nombre, temas in [("ALG", "Álgebra Lineal", ALG), ("PROG", "Fundamentos de Programación", PROG), ("ESP", especialidad, temas_especialidad)]:
            codigo = f"DEMO-{prefijo}-{familia}"
            horas = 180 if (prefijo, familia) == ("TI", "ALG") else 96 if (prefijo, familia) == ("IND", "PROG") else 144
            creditos = 2 if (prefijo, familia) == ("IND", "PROG") else 3
            materia = next((a for a in listado if a["codigo"] == codigo), None)
            if materia is None:
                materia = api("/api/asignaturas", {"carreraId": carrera["id"], "codigo": codigo, "nombre": nombre + " [DEMO]", "nivel": 3 if familia == "ESP" else 1, "horas": horas, "creditos": creditos})
            elif materia["horas"] != horas or materia["creditos"] != creditos or materia["nombre"] != nombre + " [DEMO]":
                raise RuntimeError(f"{codigo} fue modificado; se conserva sin sobrescribirlo. Revisa manualmente.")
            plan = {"planAnalitico": f"{codigo}-v1 · Programa sintético de demostración, no oficial", "unidadHoras": "hora-reloj", "sistemaCreditos": "DEMO-CREDITOS-COMPARABLES", "esSintetico": True}
            ruta = f'/api/asignaturas/{materia["id"]}'
            anterior = api(ruta + "/perfil-comparacion")
            if anterior is not None and anterior != plan:
                raise RuntimeError(f"El perfil de {codigo} cambió; no se sobrescribe.")
            if anterior is None:
                api(ruta + "/perfil-comparacion", plan, "PUT")
            detalle = api(ruta)
            resultados = RESULTADOS.get(familia, [f"Analizar fundamentos de {especialidad.lower()} en un caso de prueba", f"Aplicar herramientas de {especialidad.lower()} en un ejercicio de demostración"])
            for numero in [1, 2]:
                temas_unidad = temas[(numero - 1) * 3:numero * 3]
                if not any(u["numero"] == numero for u in detalle["unidades"]):
                    api(ruta + "/unidades", {"numero": numero, "nombre": f"Unidad {numero} · DEMO sintética", "horas": horas // 2,
                        "resultadoAprendizaje": resultados[numero - 1], "tematicas": [{"nombre": f"Bloque {numero} · DEMO", "contenidos": temas_unidad}]})
            detalle = api(ruta)
            unidades = sorted(detalle["unidades"], key=lambda u: u["numero"])
            if len(unidades) != 2 or [c["tema"] for u in unidades for c in u["contenidos"]] != temas or [u["resultadoAprendizaje"] for u in unidades] != resultados:
                raise RuntimeError(f"Los contenidos de {codigo} cambiaron; no se reutilizan revisiones sintéticas.")
            materias.append({"id": materia["id"], "prefijo": prefijo, "familia": familia, "codigo": codigo, "unidades": unidades})
        print(f"PASS demostración sintética: {nombre_carrera} · 3 asignaturas", flush=True)

    # Matriz definida exclusivamente como fixture de pruebas, nunca como inferencia de equivalencia real.
    # ALG↔ALG y PROG↔PROG usan los índices explícitos de estos programas sintéticos.
    # SW ALG→TI ALG acredita solo 4 de 6 contenidos y ambos resultados: 75 % académico.
    # Las otras familias tienen revisiones sintéticas explícitas sin vínculos: 0 % académico.
    # ROB ESP→SW PROG se deja sin revisar deliberadamente para probar información incompleta.
    guardadas = 0
    for o in materias:
        for r in materias:
            if o["id"] == r["id"] or (o["codigo"], r["codigo"]) == ("DEMO-ROB-ESP", "DEMO-SW-PROG"):
                continue
            ruta = f'/api/asignaturas/{o["id"]}/correspondencias/{r["id"]}'
            actual = api(ruta)
            if actual["registrada"]:
                if not actual["vigente"]:
                    raise RuntimeError("Una revisión DEMO quedó obsoleta. Se conserva para revisión humana.")
                guardadas += 1
                continue
            contenidos, resultados = [], []
            if o["familia"] == r["familia"] and o["familia"] in ("ALG", "PROG"):
                oc = [(u, c) for u in o["unidades"] for c in u["contenidos"]]
                rc = [(u, c) for u in r["unidades"] for c in u["contenidos"]]
                indices = range(4) if (o["codigo"], r["codigo"]) == ("DEMO-SW-ALG", "DEMO-TI-ALG") else range(6)
                for i in indices:
                    ou, c1 = oc[i]; ru, c2 = rc[i]
                    contenidos.append({"elementoOrigen": c1["id"], "elementoReceptor": c2["id"], "ubicacionOrigen": f'DEMO v1, unidad {ou["numero"]}, contenido {c1["orden"]}', "ubicacionReceptora": f'DEMO v1, unidad {ru["numero"]}, contenido {c2["orden"]}'})
                for i in range(2):
                    uo, ur = o["unidades"][i], r["unidades"][i]
                    resultados.append({"elementoOrigen": uo["id"], "elementoReceptor": ur["id"], "ubicacionOrigen": f'DEMO v1, resultado de unidad {i+1}', "ubicacionReceptora": f'DEMO v1, resultado de unidad {i+1}'})
            api(ruta, {"contenidos": contenidos, "resultados": resultados}, "PUT")
            guardadas += 1
    print(f"PASS datos DEMO: 5 carreras, 15 asignaturas, 30 unidades, 90 contenidos, 30 resultados y {guardadas} revisiones sintéticas. No son registros oficiales.", flush=True)

if __name__ == "__main__":
    main()
