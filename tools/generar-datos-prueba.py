#!/usr/bin/env python3
"""Genera db/03-datos-prueba.sql: usuarios y un año de asesorías de prueba.

Sirve para probar la app como dirección con volumen real: reportes,
filtros, calendario e historial. No toca 02-seed.sql.

    python3 tools/generar-datos-prueba.py [--hoy AAAA-MM-DD] [--asesorias 650] [--semilla 2026]

Rangos de Id reservados (el script borra y reescribe solo estos):
  personas 5000-5999 · horarios 50000-59999 · asesorías 900000-999999
  ciclos 101 (2025-2) y 102 (2026-1)
Los correos empiezan con "demo." para que nadie los confunda con personas reales,
salvo las cuatro cuentas de CUENTAS_PRUEBA (admin/asesor/asesor par/alumno .prueba).
Las fechas son relativas a --hoy: las citas futuras dejan de serlo con el
tiempo, así que conviene regenerar el archivo antes de cargarlo.
"""
import argparse
import ast
import random
import re
import unicodedata
from datetime import date, datetime, time, timedelta, timezone
from pathlib import Path
from zoneinfo import ZoneInfo

RAIZ = Path(__file__).resolve().parent.parent
SEED = RAIZ / "db" / "02-seed.sql"
SALIDA = RAIZ / "db" / "03-datos-prueba.sql"
CAMPUS = ZoneInfo("America/Tijuana")

PENDIENTE, CONFIRMADA, CANCELADA, RECHAZADA, REALIZADA = 1, 2, 3, 4, 5
ACTIVOS = {PENDIENTE, CONFIRMADA}
DIRECTIVO_DEMO = 5000
CUENTAS_PRUEBA = {  # Id -> (nombre, correo)
    5000: ("Admin", "admin.prueba@uabc.edu.mx"),                      # directivo
    5001: ("Asesor", "asesor.prueba@uabc.edu.mx"),                    # profesor
    5011: ("Asesor Estudiante", "asesor.estudiante.prueba@uabc.edu.mx"),  # asesor par (también alumno)
    5101: ("Alumno", "alumno.prueba@uabc.edu.mx"),
}

CICLOS = [  # Id, código, nombre, inicio, fin, actual
    (101, "2025-2", "Otoño 2025", date(2025, 8, 11), date(2025, 12, 12), 0),
    (102, "2026-1", "Primavera 2026", date(2026, 1, 26), date(2026, 6, 5), 0),
    (1, "2026-2", "Otoño 2026", date(2026, 8, 10), date(2026, 12, 11), 1),  # ya existe en 02-seed
]

NOMBRES = ["Ana Sofía", "Diego", "Valeria", "Emiliano", "Ximena", "Santiago", "Camila", "Mateo",
           "Regina", "Sebastián", "Renata", "Leonardo", "Mariana", "Daniel", "Fernanda", "Andrés",
           "Natalia", "Joaquín", "Paula", "Iker", "Daniela", "Rodrigo", "Andrea", "Gael",
           "Sofía", "Alejandro", "Isabella", "Héctor", "Lucía", "Óscar", "Abril", "Julián"]
APELLIDOS = ["García", "Hernández", "López", "Martínez", "González", "Pérez", "Rodríguez",
             "Sánchez", "Ramírez", "Cruz", "Flores", "Gómez", "Morales", "Vázquez", "Reyes",
             "Jiménez", "Torres", "Díaz", "Gutiérrez", "Ruiz", "Mendoza", "Aguilar", "Ortiz",
             "Castillo", "Romero", "Álvarez", "Navarro", "Ibarra", "Valenzuela", "Ochoa"]
TEMAS = ["Dudas del parcial {n}", "Repaso de ejercicios del tema {n}", "Preparación para el ordinario",
         "Revisión de la tarea {n}", "Conceptos que no quedaron claros en clase",
         "Resolución de problemas del libro, capítulo {n}", "Preparación para el extraordinario",
         "Revisión del reporte de laboratorio {n}", "Dudas sobre el proyecto final"]
NOTAS = ["Se resolvieron los ejercicios pendientes; el alumno avanza bien.",
         "Repasamos la teoría y dejé ejercicios de práctica.",
         "Buen avance; falta reforzar el planteamiento de problemas.",
         "Se aclararon dudas puntuales del examen.",
         "El alumno trajo preguntas preparadas; sesión productiva."]
MOTIVOS_CANCELA = ["Se me empalmó con un examen.", "Ya resolví mis dudas.", "Problema de transporte.",
                   "Reunión académica imprevista.", "Motivos de salud."]
MOTIVOS_RECHAZO = ["El tema no corresponde a la materia.", "Ese día tengo comisión fuera del campus.",
                   "Cupo ocupado por una asesoría grupal."]


def leer_tuplas(seccion: str) -> list[tuple]:
    """Lee las tuplas del INSERT INTO `seccion` de 02-seed.sql."""
    texto = SEED.read_text(encoding="utf-8")
    bloque = re.search(rf"INSERT INTO `{seccion}` \([^)]*\) VALUES(.*?);\s*$", texto, re.S | re.M).group(1)
    filas = []
    for linea in bloque.splitlines():
        m = re.match(r"\s*\((.*)\),?\s*(--.*)?$", linea)
        if m:
            filas.append(ast.literal_eval("(" + m.group(1).replace("NULL", "None") + ",)"))
    return filas


def sin_acentos(s: str) -> str:
    return unicodedata.normalize("NFKD", s).encode("ascii", "ignore").decode().lower().replace(" ", "")


def sql(v) -> str:
    if v is None:
        return "NULL"
    if isinstance(v, (int, float)):
        return str(v)
    if isinstance(v, datetime):
        return "'" + v.strftime("%Y-%m-%d %H:%M:%S") + "'"
    return "'" + str(v).replace("\\", "\\\\").replace("'", "''") + "'"


def insert(tabla: str, columnas: list[str], filas: list[tuple]) -> str:
    if not filas:
        return ""
    cols = ", ".join(f"`{c}`" for c in columnas)
    partes = []
    for i in range(0, len(filas), 200):  # lotes moderados para no exceder max_allowed_packet
        valores = ",\n    ".join("(" + ", ".join(sql(v) for v in f) + ")" for f in filas[i:i + 200])
        partes.append(f"INSERT INTO `{tabla}` ({cols}) VALUES\n    {valores};\n")
    return "\n".join(partes)


def utc(dia: date, hora: time) -> datetime:
    return datetime.combine(dia, hora, CAMPUS).astimezone(timezone.utc).replace(tzinfo=None)


def generar(hoy: date, total: int, rnd: random.Random) -> str:
    ahora = (datetime.now(timezone.utc) if hoy == date.today()
             else datetime.combine(hoy, time(12, 0), CAMPUS).astimezone(timezone.utc)).replace(tzinfo=None, microsecond=0)
    materias = {m[0]: m[2] for m in leer_tuplas("materias")}
    prog_materias: dict[int, list[int]] = {}
    for p, m in leer_tuplas("programas_materias"):
        prog_materias.setdefault(p, []).append(m)
    asesores_base = {a[0]: a[1] for a in leer_tuplas("perfiles_asesor")}  # Id -> programa
    alumnos_base = [a[0] for a in leer_tuplas("perfiles_alumno")]
    am_base: dict[int, list[int]] = {}
    for _, a, m in leer_tuplas("asesores_materias"):
        am_base.setdefault(a, []).append(m)
    horarios_base = leer_tuplas("horarios")

    personas, p_alumno, p_asesor = [], [], []
    usados = set()

    def persona(pid, tratamiento=None):
        while True:
            n, a1, a2 = rnd.choice(NOMBRES), rnd.choice(APELLIDOS), rnd.choice(APELLIDOS)
            if (n, a1, a2) not in usados:
                usados.add((n, a1, a2))
                break
        correo = f"demo.{sin_acentos(n.split()[0])}.{sin_acentos(a1)}{pid}@uabc.edu.mx"
        personas.append((pid, tratamiento, n, a1, a2, correo))

    # Dirección adicional, 10 profesores, 4 estudiantes que asesoran y 120 alumnos.
    personas.append((DIRECTIVO_DEMO, "Mtra.", "Laura Elena", "Domínguez", "Ruiz", "demo.direccion@uabc.edu.mx"))
    profesores = list(range(5001, 5011))
    pares = list(range(5011, 5015))
    alumnos = list(range(5101, 5221))
    for pid in profesores:
        persona(pid, rnd.choice(["Dr.", "Dra.", "Mtro.", "Mtra.", None]))
    for pid in pares + alumnos:
        persona(pid)
    # Cuentas fáciles de recordar, una por rol, para entrar a probar cada apartado.
    for i, (pid, *_) in enumerate(personas):
        if pid in CUENTAS_PRUEBA:
            personas[i] = (pid, None, CUENTAS_PRUEBA[pid][0], "Prueba", None, CUENTAS_PRUEBA[pid][1])
    for pid in pares + alumnos:
        p_alumno.append((pid, f"2{pid:06d}", rnd.randint(1, 5)))
    asesores_demo = {}
    for pid in profesores + pares:
        programa = rnd.randint(1, 5)
        asesores_demo[pid] = programa
        p_asesor.append((pid, programa, 2 if rnd.random() < 0.3 else 1))
    todos_alumnos = alumnos_base + pares + alumnos

    # Materias por asesor: las del seed para los asesores base, 3-6 de su programa para los nuevos.
    materias_de = dict(am_base)
    for pid, programa in asesores_demo.items():
        materias_de[pid] = rnd.sample(prog_materias[programa], min(len(prog_materias[programa]), rnd.randint(3, 6)))

    asesores_materias = []
    for ciclo in (101, 102):
        for a, ms in materias_de.items():
            asesores_materias += [(ciclo, a, m) for m in ms]
    asesores_materias += [(1, a, m) for a in asesores_demo for m in materias_de[a]]

    # Horarios: los del seed se copian a los ciclos anteriores; los nuevos asesores tienen
    # 2-4 bloques en días distintos (así nunca se empalman), de 1-2 h y cupo 1-3.
    horarios = []  # (Id, ciclo, asesor, día, inicio, fin, cupo, lugar)
    hid = 50000
    for ciclo in (101, 102):
        for (_, _, a, d, i, f, c, l) in horarios_base:
            horarios.append((hid, ciclo, a, d, i, f, c, l))
            hid += 1
    for ciclo in (101, 102, 1):
        for a in asesores_demo:
            for d in rnd.sample([1, 2, 3, 4, 5], rnd.randint(2, 4)):
                ini = rnd.randint(8, 18)
                lugar = 2 if rnd.random() < 0.35 else 1
                horarios.append((hid, ciclo, a, d, f"{ini:02d}:00:00", f"{ini + rnd.randint(1, 2):02d}:00:00",
                                 rnd.randint(1, 3), lugar))
                hid += 1
    bloques = [(h[0], 1, h[2], h[3], h[4], h[5], h[6], h[7]) for h in horarios_base] + horarios

    # Asesorías: de hoy - 365 días al fin del ciclo vigente.
    inicio_ventana, fin_ventana = hoy - timedelta(days=365), CICLOS[-1][4]
    ocupado_lugar, ocupado_alumno = set(), set()
    asesorias, historial, bitacoras = [], [], []
    aid = 900000
    intentos = 0
    while len(asesorias) < total and intentos < total * 50:
        intentos += 1
        # Primero la fecha (uniforme en la ventana, con un extra de citas próximas para tener
        # solicitudes que atender) y luego un bloque de ese ciclo y ese día (DiaSemana: 0 = domingo).
        if rnd.random() < 0.12:
            d = hoy + timedelta(days=rnd.randint(1, (fin_ventana - hoy).days))
        else:
            d = inicio_ventana + timedelta(days=rnd.randint(0, (fin_ventana - inicio_ventana).days))
        ciclo = next((c[0] for c in CICLOS if c[3] <= d <= c[4]), None)
        candidatos = [b for b in bloques if b[1] == ciclo and b[3] == d.isoweekday() % 7]
        if not candidatos:
            continue  # vacaciones o fin de semana
        bid, ciclo, asesor, dia, hi, hf, cupo, lugar = rnd.choice(candidatos)
        hi_t, hf_t = time.fromisoformat(hi), time.fromisoformat(hf)
        programada = utc(d, hi_t)
        fin = utc(d, hf_t)
        futura = programada > ahora

        if futura:
            estado = rnd.choices([PENDIENTE, CONFIRMADA, CANCELADA, RECHAZADA], [45, 40, 10, 5])[0]
        else:
            estado = rnd.choices([REALIZADA, CANCELADA, RECHAZADA, CONFIRMADA], [74, 13, 8, 5])[0]
        activa = estado in ACTIVOS
        # Las activas solo en bloques de prueba: los del seed los usa tools/probar-api.py.
        if activa and bid < 50000:
            continue
        alumno = rnd.choice(todos_alumnos)
        if alumno == asesor or (alumno, programada) in ocupado_alumno:
            continue
        lugar_n = 1
        if activa:
            libres = [n for n in range(1, cupo + 1) if (bid, programada, n) not in ocupado_lugar]
            if not libres:
                continue
            lugar_n = libres[0]
            ocupado_lugar.add((bid, programada, lugar_n))
        ocupado_alumno.add((alumno, programada))

        materia = rnd.choice(materias_de[asesor])
        tema = rnd.choice(TEMAS).format(n=rnd.randint(1, 4))
        creada = programada - timedelta(days=rnd.randint(2, 20), hours=rnd.randint(0, 10), minutes=rnd.randint(0, 59))
        if creada > ahora:
            creada = ahora - timedelta(hours=rnd.randint(1, 72))
        cambios = [(None, PENDIENTE, alumno, creada, None)]
        t = creada
        if estado in (CONFIRMADA, REALIZADA) or (estado == CANCELADA and rnd.random() < 0.5):
            t = min(t + timedelta(hours=rnd.randint(1, 30)), programada - timedelta(hours=25), ahora)
            t = max(t, creada + timedelta(minutes=5))
            cambios.append((PENDIENTE, CONFIRMADA, asesor, t, None))
        if estado == CANCELADA:
            t = max(t + timedelta(minutes=10), min(t + timedelta(hours=rnd.randint(2, 96)), programada - timedelta(hours=25), ahora))
            quien = alumno if rnd.random() < 0.7 else asesor
            cambios.append((cambios[-1][1], CANCELADA, quien, t, rnd.choice(MOTIVOS_CANCELA)))
        elif estado == RECHAZADA:
            t = max(t + timedelta(minutes=10), min(t + timedelta(hours=rnd.randint(1, 48)), ahora))
            cambios.append((PENDIENTE, RECHAZADA, asesor, t, rnd.choice(MOTIVOS_RECHAZO)))
        elif estado == REALIZADA:
            t = fin + timedelta(minutes=rnd.randint(5, 600))
            cambios.append((CONFIRMADA, REALIZADA, asesor, t, None))
            asistio = 1 if rnd.random() < 0.85 else 0
            bitacoras.append((aid, asistio, rnd.choice(NOTAS) if asistio else "El alumno no se presentó.", asesor, t))

        enlace = None
        if lugar == 2 and estado in (CONFIRMADA, REALIZADA):
            enlace = "https://meet.google.com/demo-" + "".join(rnd.choices("abcdefghijkmnpqrstuvwxyz", k=7))
        asesorias.append((aid, ciclo, bid, asesor, alumno, materia, programada, lugar_n, estado, tema, enlace, creada, t))
        historial += [(aid, de, a, por, en, motivo) for de, a, por, en, motivo in cambios]
        aid += 1

    if len(asesorias) < total:
        raise SystemExit(f"Solo cupieron {len(asesorias)} asesorías de {total}; baja --asesorias.")
    # Autocomprobación: cada historial va en orden y nunca antes de la creación.
    por_id = {}
    for h in historial:
        por_id.setdefault(h[0], []).append(h[4])
    for a in asesorias:
        fechas = por_id[a[0]]
        assert fechas == sorted(fechas) and fechas[0] == a[11] and fechas[-1] == a[12], a

    out = [f"""-- =============================================================================
--  03-datos-prueba.sql — GENERADO por tools/generar-datos-prueba.py; no se edita a mano.
--  Referencia: {hoy.isoformat()} · {len(personas)} personas · {len(horarios)} horarios · {len(asesorias)} asesorías.
--  Idempotente: borra y reescribe solo los rangos de prueba (ver el script).
--  Requiere 01-schema.sql, 02-seed.sql y la migración 002 aplicados.
-- =============================================================================
SET NAMES utf8mb4;
START TRANSACTION;

-- Limpieza: lo de prueba, y lo que la app haya creado encima durante las pruebas.
DELETE FROM `bitacoras_asesoria` WHERE `RegistradoPor` BETWEEN 5000 AND 5999;
DELETE FROM `asesorias` WHERE `Id` BETWEEN 900000 AND 999999
    OR `AlumnoId` BETWEEN 5000 AND 5999 OR `AsesorId` BETWEEN 5000 AND 5999
    OR `HorarioId` BETWEEN 50000 AND 59999 OR `CicloId` IN (101, 102);
DELETE FROM `horarios` WHERE `Id` BETWEEN 50000 AND 59999 OR `AsesorId` BETWEEN 5000 AND 5999 OR `CicloId` IN (101, 102);
DELETE FROM `asesores_materias` WHERE `AsesorId` BETWEEN 5000 AND 5999 OR `CicloId` IN (101, 102);
DELETE FROM `personas` WHERE `Id` BETWEEN 5000 AND 5999;
DELETE FROM `ciclos_escolares` WHERE `Id` IN (101, 102);
"""]
    out.append(insert("ciclos_escolares", ["Id", "Codigo", "Nombre", "FechaInicio", "FechaFin", "EsActual"],
                      [(c[0], c[1], c[2], c[3].isoformat(), c[4].isoformat(), c[5]) for c in CICLOS if c[0] != 1]))
    out.append(insert("personas", ["Id", "Tratamiento", "Nombres", "ApellidoPaterno", "ApellidoMaterno", "Correo"], personas))
    out.append(insert("perfiles_directivo", ["PersonaId", "Cargo"], [(DIRECTIVO_DEMO, "Coordinación de Asesorías (prueba)")]))
    out.append(insert("perfiles_asesor", ["PersonaId", "ProgramaId", "ModalidadPredeterminadaId"], p_asesor))
    out.append(insert("perfiles_alumno", ["PersonaId", "Matricula", "ProgramaId"], p_alumno))
    out.append(insert("asesores_materias", ["CicloId", "AsesorId", "MateriaId"], asesores_materias))
    out.append(insert("horarios", ["Id", "CicloId", "AsesorId", "DiaSemana", "HoraInicio", "HoraFin", "CupoMaximo", "LugarId"], horarios))
    out.append(insert("asesorias", ["Id", "CicloId", "HorarioId", "AsesorId", "AlumnoId", "MateriaId", "ProgramadaEn",
                                    "NumeroLugar", "EstadoId", "Tema", "EnlaceReunion", "CreadoEn", "ActualizadoEn"], asesorias))
    out.append("-- El trigger de inserción dejó una fila de historial sin autor ni fecha real; se reemplaza\n"
               "-- por la secuencia completa con quién y cuándo.\n"
               "DELETE FROM `historial_estados_sesion` WHERE `AsesoriaId` BETWEEN 900000 AND 999999;\n")
    out.append(insert("historial_estados_sesion", ["AsesoriaId", "EstadoAnteriorId", "EstadoNuevoId", "CambiadoPor", "CambiadoEn", "Motivo"], historial))
    out.append(insert("bitacoras_asesoria", ["AsesoriaId", "Asistio", "Notas", "RegistradoPor", "RegistradoEn"], bitacoras))
    out.append("COMMIT;\n")
    return "\n".join(o for o in out if o)


if __name__ == "__main__":
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--hoy", type=date.fromisoformat, default=date.today())
    ap.add_argument("--asesorias", type=int, default=650)
    ap.add_argument("--semilla", type=int, default=2026)
    args = ap.parse_args()
    SALIDA.write_text(generar(args.hoy, args.asesorias, random.Random(args.semilla)), encoding="utf-8")
    print(f"Escrito {SALIDA.relative_to(RAIZ)}")
