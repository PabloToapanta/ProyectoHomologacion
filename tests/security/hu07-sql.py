"""HU-07: pruebas SQL en un contenedor efímero propio, sin leer .env ni bases del equipo."""
import json
import argparse
import os
from pathlib import Path
import secrets
import subprocess
import time
import uuid

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--homologacion", action="store_true", help="Incluye persistencia de CA3 y regresión backend con SQL aislado")
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
name = "hu07-sql-" + uuid.uuid4().hex[:10]
password = "Hu07!" + secrets.token_hex(24)
logs = root / "TestResults" / ("hu07-ca3-sql" if args.homologacion else "hu07-sql")
logs.mkdir(parents=True, exist_ok=True)


def docker(*args):
    result = subprocess.run(["docker", *args], capture_output=True, text=True)
    if result.returncode:
        diagnostic = (result.stdout + result.stderr)[-4000:].replace(password, "[oculto]")
        raise RuntimeError(f"Docker falló ({result.returncode}): {diagnostic}")
    return result


try:
    docker("run", "--detach", "--rm", "--name", name, "--label", "prueba=hu07-aislada",
           "-e", "ACCEPT_EULA=Y", "-e", "MSSQL_PID=Developer", "-e", "MSSQL_SA_PASSWORD=" + password,
           "-p", "127.0.0.1::1433", "mcr.microsoft.com/mssql/server:2022-latest")
    port = docker("port", name, "1433/tcp").stdout.strip().rsplit(":", 1)[1]
    for attempt in range(45):
        result = subprocess.run(["docker", "exec", name, "/opt/mssql-tools18/bin/sqlcmd", "-C",
                                 "-S", "localhost", "-U", "sa", "-P", password, "-Q", "SELECT 1"],
                                capture_output=True)
        if result.returncode == 0:
            break
        time.sleep(2)
    else:
        raise RuntimeError("SQL Server aislado no estuvo listo en 90 segundos.")
    env = dict(os.environ)
    env["HU07_TEST_SQL"] = (f"Server=127.0.0.1,{port};Database=master;User Id=sa;Password={password};"
                            "TrustServerCertificate=True;Application Name=hu07-aislado")
    projects = [("catalogo", "tests/Catalogo.Tests/Catalogo.Tests.csproj")]
    if args.homologacion:
        env["HU20_TEST_SQL"] = env["HU07_TEST_SQL"]
        env["HU21_TEST_SQL"] = env["HU07_TEST_SQL"]
        # CA3 no necesita un modelo real; se conserva la omisión explícita del caso optativo de HU-18.
        env.pop("HU18_OLLAMA_URL", None)
        env.pop("HU18_OLLAMA_MODEL", None)
        projects += [("infraestructura", "tests/Homologacion.Infrastructure.Tests/Homologacion.Infrastructure.Tests.csproj"),
                     ("solucion", "Homologacion.sln"), ("solicitudes", "tests/Solicitudes.Tests/Solicitudes.Tests.csproj")]
    for suite, project in projects:
        logger = "trx" if suite == "solucion" else f"trx;LogFileName={suite}.trx"
        with (logs / (suite + ".log")).open("w", encoding="utf-8") as output:
            result = subprocess.run(["dotnet", "test", project, "--no-restore", "--nologo", "--logger",
                                     logger, "--results-directory", str(logs / suite)],
                                    cwd=root, env=env, stdout=output, stderr=subprocess.STDOUT)
        print(json.dumps({"contenedor": name, "suite": suite, "resultado_pruebas": result.returncode}), flush=True)
        if result.returncode:
            raise RuntimeError("Fallaron las pruebas; revisar log saneado, no se oculta el fallo.")
finally:
    assert name.startswith("hu07-sql-")
    subprocess.run(["docker", "rm", "--force", name], capture_output=True)
    print("Limpieza del contenedor propio solicitada; no se crearon volúmenes persistentes.")
