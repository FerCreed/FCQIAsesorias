#!/usr/bin/env python3
"""
Pruebas de funcionalidad y operabilidad de la API del Sistema de Asesorías.

No sustituyen a `dotnet test`: aquellas prueban las reglas en aislamiento,
estas prueban la API COMPLETA en ejecución —autenticación, autorización,
catálogo, cupo y cambios de estado— contra la base sembrada por db/02-seed.sql.

    python3 tools/probar-api.py                      # contenedor en :5080
    API=http://localhost:5016 python3 tools/probar-api.py   # API nativa

Requiere la base recién sembrada (los conteos y los bloques libres dependen
de ella). Todo lo que crea lo deja cancelado al terminar.

Devuelve 0 si todo pasa; 1 si algo falla, para poder encadenarlo en CI.
"""
import datetime
import json
import os
import sys
import urllib.error
import urllib.request

API = os.environ.get("API", "http://localhost:5080")
OK, FALLAS = [], []

# Bloques de horario del tutor 1 que el seed deja libres. Los que usa
# 02-seed.sql (1 y 2) se evitan a propósito: ocuparlos daría "lleno" y la
# prueba fallaría por el dato, no por el código.
MARTES, JUEVES, VIERNES = 4, 3, 5          # ids de availabilities
DIA_MARTES, DIA_JUEVES, DIA_VIERNES = 2, 4, 5   # 0=domingo, como System.DayOfWeek
HORA_MARTES, HORA_JUEVES, HORA_VIERNES = "13:00:00", "12:00:00", "10:00:00"

CALCULO = 2          # materia asignada al tutor 1
SIN_ASIGNAR = 44     # materia que el tutor 1 no imparte


def call(method, path, token=None, body=None):
    data = json.dumps(body).encode() if body is not None else None
    request = urllib.request.Request(API + path, data=data, method=method)
    request.add_header("Content-Type", "application/json")
    if token:
        request.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            raw = response.read().decode()
            return response.status, (json.loads(raw) if raw.strip() else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw) if raw.strip() else None
        except json.JSONDecodeError:
            return e.code, raw


def check(nombre, condicion, detalle=""):
    (OK if condicion else FALLAS).append(nombre)
    marca = "  OK   " if condicion else "  FALLA"
    print(f"{marca} {nombre}" + (f"  -> {detalle}" if detalle and not condicion else ""))


def login(email):
    status, body = call("POST", "/api/auth/demo", body={"email": email})
    if status != 200:
        print(f"No se pudo entrar como {email}: {status} {body}", file=sys.stderr)
        sys.exit(2)
    return body


def proxima(dow, desplazamiento=1):
    """Próxima fecha, contando desde mañana, con ese día de la semana."""
    dia = datetime.date.today() + datetime.timedelta(days=desplazamiento)
    while (dia.weekday() + 1) % 7 != dow:
        dia += datetime.timedelta(days=1)
    return dia


def cuando(dow, hora, desplazamiento=1):
    h, m, s = (int(x) for x in hora.split(":"))
    return datetime.datetime.combine(proxima(dow, desplazamiento), datetime.time(h, m, s)).isoformat()


creadas = []


def solicitar(token, **campos):
    status, body = call("POST", "/api/sessions", token, campos)
    if status == 201 and isinstance(body, dict):
        creadas.append(body["id"])
    return status, body


print(f"Probando {API}\n")
print("== A. AUTENTICACIÓN Y ACCESO ==")
check("A1 /api/subjects sin token responde 401", call("GET", "/api/subjects")[0] == 401)
check("A2 /api/sessions sin token responde 401", call("GET", "/api/sessions")[0] == 401)
check("A3 /api/admin sin token responde 401", call("GET", "/api/admin/advisors")[0] == 401)
s, b = call("POST", "/api/auth/demo", body={"email": "alguien@gmail.com"})
check("A4 correo no institucional rechazado (400)", s == 400, (s, b))
s, b = call("POST", "/api/auth/demo", body={"email": "nadie@uabc.edu.mx"})
check("A5 correo UABC no registrado rechazado (401)", s == 401, (s, b))
check("A6 token inválido rechazado (401)", call("GET", "/api/subjects", "token.falso")[0] == 401)

alumno = login("yesua.diaz@uabc.edu.mx")
otro_alumno = login("juan.laguna@uabc.edu.mx")
asesor = login("felipe.marquez63@uabc.edu.mx")
otro_asesor = login("edgar.tsuchiya@uabc.edu.mx")
par = login("j2207105@uabc.edu.mx")
direccion = login("progasesorias.fcqi@uabc.edu.mx")
TA, TA2, TS, TO, TP, TD = (alumno["token"], otro_alumno["token"], asesor["token"],
                           otro_asesor["token"], par["token"], direccion["token"])

check("A7 el login devuelve la lista de roles", alumno["roles"] == ["Alumno"], alumno["roles"])
check("A8 el asesor par viaja con SUS DOS roles en el mismo token",
      sorted(par["roles"]) == ["Alumno", "Asesor"], par["roles"])
check("A9 dirección entra como Directivo", direccion["role"] == "Directivo", direccion["role"])
perfiles = call("GET", "/api/auth/demo-profiles")[1]
check("A10 el directorio lista al asesor par una vez por rol",
      len([p for p in perfiles if p["email"] == "j2207105@uabc.edu.mx"]) == 2)

print("\n== B. CATÁLOGO ==")
s, materias = call("GET", "/api/subjects", TA)
check("B1 el alumno ve el catálogo de materias", s == 200 and len(materias) == 44,
      (s, len(materias) if materias else None))
s, tutores = call("GET", f"/api/advisors?subjectId={CALCULO}", TA)
check("B2 filtra tutores por materia", s == 200 and len(tutores) > 0, s)
s, bloques = call("GET", "/api/advisors/1/availabilities", TA)
check("B3 devuelve los bloques del tutor", s == 200 and len(bloques) > 0, s)
check("B4 cada bloque trae sede y modalidad", all(b["modality"] and b["location"] for b in bloques))
s, vacio = call("GET", "/api/advisors/9999/availabilities", TA)
check("B5 tutor inexistente devuelve lista vacía, no error", s == 200 and vacio == [], (s, vacio))

print("\n== C. EL ALUMNO SOLICITA ==")
s, b = solicitar(TA, studentId=alumno["profileId"], advisorId=1, subjectId=CALCULO,
                 availabilityId=MARTES, scheduledAt=cuando(DIA_MARTES, HORA_MARTES),
                 topic="Prueba automática")
check("C1 una solicitud válida se registra (201)", s == 201, (s, b))
if s == 201:
    check("C2 nace en estado Pendiente", b["status"] == "Pendiente", b.get("status"))
    check("C3 la hora vuelve en local y en UTC",
          b["scheduledAt"][11:16] == HORA_MARTES[:5] and b["scheduledAtUtc"].endswith("Z"),
          (b["scheduledAt"], b["scheduledAtUtc"]))

s, b = call("POST", "/api/sessions", TA, dict(studentId=0, advisorId=1, subjectId=CALCULO,
            availabilityId=MARTES, scheduledAt=cuando(DIA_MARTES, HORA_MARTES), topic="Duplicada"))
check("C4 la misma solicitud dos veces se rechaza", s == 400 and "activa" in str(b), (s, b))

s, b = solicitar(TA, studentId=999, advisorId=1, subjectId=CALCULO, availabilityId=JUEVES,
                 scheduledAt=cuando(DIA_JUEVES, HORA_JUEVES), topic="Suplantación")
check("C5 studentId ajeno en el cuerpo se ignora: manda el token",
      s == 201 and b["studentId"] == alumno["profileId"], (s, b))

casos = [
    ("C6 fecha que no cae en el día del bloque", "es de",
     dict(availabilityId=MARTES, scheduledAt=cuando(DIA_JUEVES, HORA_MARTES))),
    ("C7 hora distinta a la de inicio del bloque", "empieza",
     dict(availabilityId=MARTES, scheduledAt=cuando(DIA_MARTES, "09:00:00"))),
    ("C8 bloque que no pertenece a ese tutor", "no pertenece",
     dict(advisorId=2, availabilityId=MARTES, scheduledAt=cuando(DIA_MARTES, HORA_MARTES))),
    ("C9 materia que dirección no asignó a ese tutor", "asignad",
     dict(subjectId=SIN_ASIGNAR, availabilityId=MARTES, scheduledAt=cuando(DIA_MARTES, HORA_MARTES))),
    ("C10 fecha fuera del ciclo escolar", "ciclo",
     dict(availabilityId=MARTES, scheduledAt=cuando(DIA_MARTES, HORA_MARTES, 120))),
]
for nombre, esperado, extra in casos:
    cuerpo = dict(studentId=0, advisorId=1, subjectId=CALCULO, topic="Caso inválido")
    cuerpo.update(extra)
    s, b = call("POST", "/api/sessions", TA, cuerpo)
    check(f"{nombre} se rechaza", s == 400 and esperado in str(b), (s, b))

pasado = datetime.datetime.now().replace(hour=13, minute=0, second=0, microsecond=0)
while (pasado.weekday() + 1) % 7 != DIA_MARTES or pasado >= datetime.datetime.now():
    pasado -= datetime.timedelta(days=1)
s, b = call("POST", "/api/sessions", TA, dict(studentId=0, advisorId=1, subjectId=CALCULO,
            availabilityId=MARTES, scheduledAt=pasado.isoformat(), topic="En el pasado"))
check("C11 no se puede agendar en el pasado", s == 400 and "pasado" in str(b), (s, b))

s, b = call("POST", "/api/sessions", TS, dict(studentId=0, advisorId=1, subjectId=CALCULO,
            availabilityId=MARTES, scheduledAt=cuando(DIA_MARTES, HORA_MARTES), topic="Asesor agendando"))
check("C12 un asesor sin perfil de alumno no puede solicitar (403)", s == 403, (s, b))

print("\n== D. EL ASESOR PAR, CON SUS DOS ROLES ==")
s, suyas = call("GET", f"/api/sessions?studentId={par['profileId']}", TP)
check("D1 ve sus asesorías COMO ALUMNA", s == 200 and len(suyas) > 0 and
      all(x["studentId"] == par["profileId"] for x in suyas), (s, suyas))
s, agenda = call("GET", f"/api/sessions?advisorId={par['profileId']}", TP)
check("D2 ve su agenda COMO ASESORA", s == 200 and len(agenda) > 0 and
      all(x["advisorId"] == par["profileId"] for x in agenda), (s, agenda))
check("D3 las dos bandejas no se mezclan",
      not ({x["id"] for x in suyas} & {x["id"] for x in agenda}))
s, b = call("POST", "/api/sessions", TP, dict(studentId=0, advisorId=par["profileId"],
            subjectId=30, availabilityId=85, scheduledAt=cuando(4, "14:00:00"), topic="Conmigo misma"))
check("D4 no puede agendarse consigo misma", s == 400 and "contigo mismo" in str(b), (s, b))
s, b = call("GET", f"/api/sessions?studentId={alumno['profileId']}", TP)
check("D5 tener dos roles no le da acceso a lo ajeno (403)", s == 403, (s, b))

print("\n== E. CUPO ==")
s, b = solicitar(TA2, studentId=0, advisorId=1, subjectId=CALCULO, availabilityId=VIERNES,
                 scheduledAt=cuando(DIA_VIERNES, HORA_VIERNES), topic="Ocupa el único lugar")
check("E1 otro alumno toma el último lugar del bloque", s == 201, (s, b))
s, b = call("POST", "/api/sessions", TA, dict(studentId=0, advisorId=1, subjectId=CALCULO,
            availabilityId=VIERNES, scheduledAt=cuando(DIA_VIERNES, HORA_VIERNES), topic="Sobreventa"))
check("E2 el bloque lleno rechaza a un tercero", s == 400 and "lleno" in str(b), (s, b))

print("\n== F. VISIBILIDAD ==")
check("F1 pedir las asesorías de otro alumno da 403",
      call("GET", f"/api/sessions?studentId={otro_alumno['profileId']}", TA)[0] == 403)
check("F2 un alumno no puede listar la agenda de un tutor",
      call("GET", "/api/sessions?advisorId=1", TA)[0] == 403)
s, mias = call("GET", "/api/sessions", TA)
check("F3 sin filtro, el alumno ve solo las suyas",
      s == 200 and all(x["studentId"] == alumno["profileId"] for x in mias), s)
s, suyas_asesor = call("GET", "/api/sessions", TS)
check("F4 sin filtro, el asesor ve solo las suyas",
      s == 200 and all(x["advisorId"] == asesor["profileId"] for x in suyas_asesor), s)
s, todas = call("GET", "/api/sessions", TD)
check("F5 dirección ve todas", s == 200 and len(todas) > len(mias), s)
s, pendientes = call("GET", "/api/sessions?status=Pendiente", TD)
check("F6 el filtro por estado funciona",
      s == 200 and all(x["status"] == "Pendiente" for x in pendientes), s)

print("\n== G. CAMBIOS DE ESTADO ==")
objetivo = creadas[0]
check("G1 el alumno no puede confirmar su propia asesoría (403)",
      call("PATCH", f"/api/sessions/{objetivo}/status", TA, {"status": "Confirmada"})[0] == 403)
check("G2 un tutor ajeno no puede tocarla (403)",
      call("PATCH", f"/api/sessions/{objetivo}/status", TO, {"status": "Confirmada"})[0] == 403)
check("G3 el tutor dueño confirma (204)",
      call("PATCH", f"/api/sessions/{objetivo}/status", TS, {"status": "Confirmada"})[0] == 204)
check("G4 el alumno dueño cancela (204)",
      call("PATCH", f"/api/sessions/{objetivo}/status", TA, {"status": "Cancelada"})[0] == 204)
s, b = call("PATCH", f"/api/sessions/{objetivo}/status", TS, {"status": "Confirmada"})
check("G5 una asesoría cancelada NO revive (400, sin 500)",
      s == 400 and "cancelada" in str(b), (s, str(b)[:120]))
check("G6 repetir el estado que ya tiene es inocuo (204)",
      call("PATCH", f"/api/sessions/{objetivo}/status", TD, {"status": "Cancelada"})[0] == 204)
check("G7 estado inexistente se rechaza (400)",
      call("PATCH", f"/api/sessions/{objetivo}/status", TD, {"status": "Inventada"})[0] == 400)
check("G8 asesoría inexistente se rechaza (400)",
      call("PATCH", "/api/sessions/999999/status", TD, {"status": "Confirmada"})[0] == 400)

print("\n== H. DIRECCIÓN ==")
check("H1 un alumno no entra al panel de dirección (403)",
      call("GET", "/api/admin/advisors", TA)[0] == 403)
check("H2 un asesor tampoco (403)", call("GET", "/api/admin/advisors", TS)[0] == 403)
s, panel = call("GET", "/api/admin/advisors", TD)
check("H3 dirección lista a los 14 tutores", s == 200 and len(panel) == 14,
      (s, len(panel) if panel else None))

tutor = next(t for t in panel if t["id"] == 14)
antes = [m["id"] for m in materias if m["name"] in tutor["subjects"]]
check("H4 dirección asigna una materia a un tutor (204)",
      call("PUT", "/api/admin/advisors/14/subjects", TD, antes + [1])[0] == 204)
s, despues = call("GET", "/api/advisors?subjectId=1", TA)
check("H5 la materia asignada aparece al alumno",
      s == 200 and any(a["id"] == 14 for a in despues), s)
check("H6 dirección retira la materia (204)",
      call("PUT", "/api/admin/advisors/14/subjects", TD, antes)[0] == 204)
s, b = call("PUT", "/api/admin/advisors/9999/subjects", TD, [1])
check("H7 tutor inexistente se rechaza (400)", s == 400, (s, b))
s, b = call("PUT", "/api/admin/advisors/14/subjects", TD, [9999])
check("H8 materia inexistente se rechaza (400, sin 500)",
      s == 400 and "9999" in str(b), (s, str(b)[:120]))
check("H9 un alumno no puede reasignar materias (403)",
      call("PUT", "/api/admin/advisors/14/subjects", TA, [1])[0] == 403)
s, b = call("PUT", "/api/admin/advisors/1/subjects", TD, [])
check("H10 no se puede quitar una materia con asesorías del ciclo",
      s == 400 and "asesor" in str(b).lower(), (s, str(b)[:120]))

print("\n== LIMPIEZA ==")
for sid in creadas:
    call("PATCH", f"/api/sessions/{sid}/status", TD, {"status": "Cancelada"})
print(f"  {len(creadas)} solicitudes de prueba canceladas")

print(f"\nRESULTADO: {len(OK)} correctas / {len(FALLAS)} fallas")
for f in FALLAS:
    print("  FALLA:", f)
sys.exit(1 if FALLAS else 0)
