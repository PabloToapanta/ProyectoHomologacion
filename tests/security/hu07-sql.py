"""HU-07: pruebas SQL en un contenedor efímero propio, sin leer .env ni bases del equipo."""
import json
import os
from pathlib import Path
import secrets
import subprocess
import time
import uuid

root = Path(__file__).resolve().parents[2]
name = "hu07-sql-" + uuid.uuid4().hex[:10]
password = "Hu07!" + secrets.token_hex(24)
logs = root / "TestResults" / "hu07-sql"
logs.mkdir(parents=True, exist_ok=True)


def docker(*args):
    return subprocess.run(["docker", *args], capture_output=True, text=True, check=True)


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
    with (logs / "pruebas.log").open("w", encoding="utf-8") as output:
        result = subprocess.run(["dotnet", "test", "tests/Catalogo.Tests/Catalogo.Tests.csproj", "--no-restore",
                                 "--nologo", "--logger", "trx;LogFileName=catalogo-sql.trx",
                                 "--results-directory", str(logs)], cwd=root, env=env,
                                stdout=output, stderr=subprocess.STDOUT)
    print(json.dumps({"contenedor": name, "resultado_pruebas": result.returncode,
                      "log": "TestResults/hu07-sql/pruebas.log"}))
    if result.returncode:
        raise RuntimeError("Fallaron las pruebas; revisar log saneado, no se oculta el fallo.")
finally:
    assert name.startswith("hu07-sql-")
    subprocess.run(["docker", "rm", "--force", name], capture_output=True)
    print("Limpieza del contenedor propio solicitada; no se crearon volúmenes persistentes.")
