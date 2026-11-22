"""HU-21: agrupaciones con Gateway/JWE/servicios/SQL reales y sin LLM.

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
    project = "hu21-check-" + secrets.token_hex(5)
    assert re.fullmatch(r"hu21-check-[0-9a-f]{10}", project)
    output = ROOT / "TestResults" / "hu21-integration"
    output.mkdir(parents=True, exist_ok=True)
    settings = {
        "AUTH_SQL_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "AUTH_JWT_ISSUER": "hu21-isolated",
        "AUTH_JWT_AUDIENCE": "hu21-isolated-api",
        "AUTH_JWT_KEY": secrets.token_hex(32),
        "AUTH_JWT_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode(),
        "APP_SECRET_KEY": secrets.token_urlsafe(32),
        "AUTH_ADMIN_USERNAME": "admin.hu21",
        "AUTH_ADMIN_PASSWORD": "Aa1!" + secrets.token_hex(20),
        "CATALOGO_DATABASE": "Hu18Catalogo",
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

    def run(args, timeout=120):
        result = subprocess.run(args, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=timeout)
        if result.returncode:
            diagnostic = result.stdout + result.stderr
            for value in private_values:
                if value:
                    diagnostic = diagnostic.replace(value, "[oculto]")
            (output / "error.log").write_text(diagnostic, encoding="utf-8")
            raise RuntimeError("Comando Docker falló; diagnóstico saneado en TestResults/hu21-integration/error.log")
        return result.stdout

    def check(name, actual, expected):
        assert actual == expected, f"{name}: esperado {expected}, obtenido {actual}"
        checks.append({"caso": name, "resultado": actual})
        print("PASS", name, flush=True)

    def request(path, body=None, token=None, method=None, etag=None):
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        if etag:
            headers["If-Match"] = etag
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
        overlay=output / "compose-test.json"
        overlay.write_text(json.dumps({"services":{"auth-db":{"ports":["127.0.0.1::1433"]}}}),encoding="utf-8")
        compose.extend(["-f",str(overlay)])
        config = json.loads(run(compose + ["config", "--format", "json"]))
        check("modelo deshabilitado", str(config["services"]["homologacion-service"]["environment"]["AnalisisInterno__Habilitado"]).lower(), "false")
        print("Construyendo servicios aislados...", flush=True)
        run(compose + ["build", "auth-service", "catalogo-api", "homologacion-service", "solicitudes-service", "gateway"], 900)
        run(compose + ["up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "gateway"], 240)
        base = "http://" + run(compose + ["port", "gateway", "8080"]).strip()
        deadline = time.monotonic() + 120
        while True:
            try:
                status, login = request("/api/auth/login", {"nombreUsuario": "admin.hu21", "password": settings["AUTH_ADMIN_PASSWORD"]})
                if status == 200:
                    break
            except (URLError, TimeoutError, RemoteDisconnected, ConnectionResetError):
                pass
            if time.monotonic() >= deadline:
                raise RuntimeError("Login aislado no disponible")
            time.sleep(1)
        check("login JWE real", len(login["accessToken"].split(".")), 5)
        admin = login["accessToken"]
        actors = [("coord.hu21", 3), ("doc1.hu21", 2), ("doc2.hu21", 2), ("doc3.hu21", 2)]
        sql = "".join("INSERT INTO Usuarios (Id,NombreUsuario,NombreNormalizado,PasswordHash,Activo,RolId) "
                      f"SELECT NEWID(),N'{name}',N'{name.upper()}',PasswordHash,1,'00000000-0000-0000-0000-{role:012d}' "
                      "FROM Usuarios WHERE NombreNormalizado=N'ADMIN.HU21';" for name, role in actors)
        run(compose + ["exec", "-T", "auth-db", "/bin/bash", "-c",
                       'exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d HomologacionAuth -b -Q "' + sql + '"'])
        tokens = {name: post("/api/auth/login", {"nombreUsuario": name, "password": settings["AUTH_ADMIN_PASSWORD"]}, None, 200)["accessToken"] for name, _ in actors}
        coord, docente = tokens["coord.hu21"], tokens["doc1.hu21"]
        careers, subjects, units = [], [], []
        for suffix in ["O", "R"]:
            career = post("/api/carreras", {"nombre": "HU21 sintetica " + suffix, "facultad": "FISEI", "vigencia": True}, admin)
            careers.append(career["id"])
            subject = post("/api/asignaturas", {"carreraId": career["id"], "codigo": "HU21-" + suffix, "nombre": "Asignatura sintetica " + suffix, "nivel": 1, "creditos": 3, "horas": 100}, admin)
            subjects.append(subject["id"])
            units.append(post(f'/api/asignaturas/{subject["id"]}/unidades', {"numero": 1, "nombre": "Unidad sintetica", "horas": 100, "resultadoAprendizaje": "Resultado sintetico", "contenidos": ["Contenido sintetico"]}, admin))
            check("perfil sintetico", request(f'/api/asignaturas/{subject["id"]}/perfil-comparacion', {"planAnalitico": "plan-sintetico-" + suffix, "unidadHoras": "hora-reloj", "sistemaCreditos": "sistema-sintetico", "esSintetico": True}, admin, "PUT")[0], 200)
        application = post("/api/solicitudes", {"numeroQuipux": "HU21-SINTETICO", "identificacionEstudiante": "HU21-SINTETICO", "nombreEstudiante": "Estudiante sintetico", "tipoCambio": "Interno", "carreraOrigenId": careers[0], "carreraReceptoraId": careers[1]}, coord)
        sid = application["id"]
        approved = post(f"/api/solicitudes/{sid}/asignaturas-aprobadas", {"asignaturaOrigenId": subjects[0], "nota": 8, "fechaAprobacion": "2026-06-01", "numeroMatricula": "HU21-SINTETICA", "intentoMatricula": 1}, coord)
        teacher_status, teachers = request(f"/api/solicitudes/{sid}/docentes-disponibles", token=coord)
        check("directorio docente real", teacher_status, 200)
        selected = [t["id"] for t in teachers if t["nombreUsuario"] in ["doc1.hu21", "doc2.hu21"]]
        post(f"/api/solicitudes/{sid}/docentes", {"docenteIds": selected}, coord, 200)
        # Segunda receptora de la misma carrera; comparte elementos solo mediante revisión explícita.
        second=post('/api/asignaturas',{'carreraId':careers[1],'codigo':'HU21-R2','nombre':'Receptora sintetica 2','nivel':2,'creditos':1.5,'horas':40},admin)
        r2=second['id']
        u2=post(f'/api/asignaturas/{r2}/unidades',{'numero':1,'nombre':'Unidad sintetica 2','horas':40,'resultadoAprendizaje':'Resultado sintetico','contenidos':['Contenido sintetico']},admin)
        request(f'/api/asignaturas/{r2}/perfil-comparacion',{'planAnalitico':'plan-sintetico-R2','unidadHoras':'hora-reloj','sistemaCreditos':'sistema-sintetico','esSintetico':True},admin,'PUT')
        for target,unit in [(subjects[1],units[1]),(r2,u2)]:
            def link(a,b):return {'elementoOrigen':a,'elementoReceptor':b,'ubicacionOrigen':'plan-O/U1','ubicacionReceptora':'plan-R/U1'}
            revision={'contenidos':[link(units[0]['contenidos'][0]['id'],unit['contenidos'][0]['id'])],'resultados':[link(units[0]['id'],unit['id'])]}
            check('revision explicita '+target,request(f'/api/asignaturas/{subjects[0]}/correspondencias/{target}',revision,admin,'PUT')[0],200)
        ruta=f'/api/homologaciones/solicitudes/{sid}/agrupaciones'
        aid=approved['id']
        body={'origenes':[aid],'receptoras':[subjects[1],r2],'aportes':[
            {'asignaturaAprobadaId':aid,'asignaturaReceptoraId':subjects[1],'horasAportadas':60,'creditosAportados':1.5},
            {'asignaturaAprobadaId':aid,'asignaturaReceptoraId':r2,'horasAportadas':40,'creditosAportados':1.5}]}
        check('sin JWT',request(ruta)[0],401)
        check('otro rol',request(ruta,token=coord)[0],403)
        check('docente ajeno',request(ruta,token=tokens['doc3.hu21'])[0],404)
        code,options=request(ruta+'/opciones',token=docente);check('opciones autorizadas',code,200)
        check('celdas revisadas habilitadas',all(v['habilitado'] for v in options['vinculos']),True)
        created=post(ruta,body,docente)
        gid=created['grupoId'];path=ruta+'/'+gid;version=created['version']
        check('ETag fuerte opaco',version.startswith('"') and version.endswith('"') and not version.startswith('W/'),True)
        check('PUT sin version',request(path,body,docente,'PUT')[0],428)
        check('DELETE version obsoleta',request(path,token=docente,method='DELETE',etag='"obsoleta"')[0],409)
        check('otro grupo mismo origen',request(ruta,body,docente)[0],409)
        pareja=f'/api/homologaciones/solicitudes/{sid}/analisis-interno'
        check('pareja no roba reserva grupal',request(pareja,{'asignaturaAprobadaId':aid,'asignaturaReceptoraId':subjects[1]},docente)[0],409)
        check('revision manual sin agente no roba reserva grupal',request(f'/api/homologaciones/solicitudes/{sid}/revisiones-docentes',{'asignaturaAprobadaId':aid,'asignaturaReceptoraId':subjects[1],'postura':'Ajuste','decision':'NoHomologable','observacion':'Revision manual sin agente'},docente,'PUT')[0],409)
        pendiente={**body,'aportes':[{**body['aportes'][0],'horasAportadas':None},body['aportes'][1]]}
        code,g=request(path,pendiente,docente,'PUT',version);check('reemplazo pendiente',code,200);version=g['version']
        code,r=request(path+'/analisis',{},docente,etag=version);check('analisis pendiente HTTP',code,200)
        check('null no guarda ni congela',r['guardado'],False)
        code,g=request(path,body,docente,'PUT',version);check('completar borrador',code,200);version=g['version']
        code,r=request(path+'/analisis',{},docente,etag=version);check('motor agrupado real HTTP',code,200);check('primer analisis guardado',r['guardado'],True)
        version=r['detalle']['version'];check('dos receptoras separadas',len(r['evaluacion']['receptoras']),2)
        check('sugerencia unica sin promediar',r['evaluacion']['estado'],'NoHomologable')
        resultados={x['asignaturaReceptora']:x for x in r['evaluacion']['receptoras']}
        check('cobertura receptora uno',resultados[subjects[1]]['compatibilidadAcademicaPct'],100)
        check('horas receptora uno',resultados[subjects[1]]['compatibilidadHorariaPct'],60)
        check('creditos receptora uno',resultados[subjects[1]]['compatibilidadCreditosPct'],50)
        check('horas receptora dos',resultados[r2]['compatibilidadHorariaPct'],100)
        check('creditos receptora dos',resultados[r2]['compatibilidadCreditosPct'],100)
        check('version HU03 fotografiada',bool(r['evaluacion']['configuracionVersionId']),True)
        check('evidencia fotografiada',len(r['evaluacion']['evidencias']),4)
        check('composicion congelada',request(path,body,docente,'PUT',version)[0],409)
        review={'postura':'Ajuste','decision':'NoHomologable','observacion':'Revision sintetica N:M'}
        check('revision primer docente',request(path+'/revisiones',review,docente,'PUT')[0],200)
        check('correccion misma revision',request(path+'/revisiones',review,docente,'PUT')[0],200)
        informe=f'/api/homologaciones/solicitudes/{sid}/informe'
        check('una revision no genera informe',request(informe,{},docente)[0],400)
        check('revision segundo docente',request(path+'/revisiones',review,tokens['doc2.hu21'],'PUT')[0],200)
        code,i=request(informe,{},docente);check('informe real con grupo',code,201)
        check('grupo aparece una vez',len(i['grupos']),1);check('dos docentes por grupo',len(i['grupos'][0]['docentes']),2)
        check('revision cerrada tras informe',request(path+'/revisiones',review,docente,'PUT')[0],409)
        check('reanalisis cerrado tras informe',request(path+'/analisis',{},docente,etag=version)[0],409)
        run(compose+['restart','homologacion-service'])
        deadline=time.monotonic()+60
        while True:
            try:
                code,g=request(path,token=docente)
                if code==200:break
            except (URLError,TimeoutError,RemoteDisconnected,ConnectionResetError):pass
            if time.monotonic()>=deadline:raise RuntimeError('No recupero grupo tras reinicio')
            time.sleep(1)
        check('ultimo analisis durable',g['ultimoAnalisis']['grupoId'],gid)
        check('informe durable',len(request(informe,token=docente)[1]['grupos']),1)
        # Infraestructura real: bases vacía/develop+datos, carreras entre contextos y rollback.
        sql_address=run(compose+['port','auth-db','1433']).strip()
        environment['HU21_TEST_SQL']=f'Server={sql_address.replace(":",",")};Database=master;User Id=sa;Password={settings["AUTH_SQL_PASSWORD"]};Encrypt=True;TrustServerCertificate=True'
        environment['HU20_TEST_SQL']=environment['HU21_TEST_SQL']
        print('Ejecutando integracion SQL y regresion entre contextos...',flush=True)
        run(['dotnet','test','tests/Homologacion.Infrastructure.Tests/Homologacion.Infrastructure.Tests.csproj','-c','Release','--logger','trx;LogFilePrefix=hu21-sql-real','--results-directory','TestResults/hu21/sql-real'],180)
        check('migracion historicos concurrencia y rollback SQL',True,True)
        (output/'resultado.json').write_text(json.dumps({'proyecto':project,'casos':checks,'datos':'sinteticos','motor':'EvaluarAgrupacion real, sin LLM','transporte':'HTTP local aislado, no acredita HTTPS'},ensure_ascii=False,indent=2),encoding='utf-8')
    except Exception:
        try:
            diagnostic=run(compose+['logs','--no-color','--tail','80'])
        except Exception:
            diagnostic='No fue posible recuperar los registros del proyecto temporal.'
        for value in private_values:
            if value:diagnostic=diagnostic.replace(value,'[oculto]')
        (output/'servicios-error.log').write_text(diagnostic,encoding='utf-8')
        raise
    finally:
        cleanup=subprocess.run(compose+['down','--volumes','--remove-orphans'],cwd=ROOT,env=environment,capture_output=True,timeout=120)
        if cleanup.returncode:print('Limpieza pendiente:',project)
        env_path.unlink(missing_ok=True)

if __name__=='__main__':main()
