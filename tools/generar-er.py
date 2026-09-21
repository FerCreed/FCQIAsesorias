import subprocess, collections, io, datetime

def q(sql, width=None):
    r=subprocess.run(['docker','exec','fcqi-dev','mysql','-ufcqi_app','-pdev_app',
        'fcqi_asesorias','-N','-B','-e',sql],capture_output=True,text=True)
    rows=[l.split('\t') for l in r.stdout.strip().split('\n') if l]
    # mysql -B puede recortar campos vacíos al final; se rellenan para que el
    # desempaquetado no dependa de si la última columna traía valor.
    if width:
        rows=[row + ['']*(width-len(row)) for row in rows]
    return rows

DB='fcqi_asesorias'
tables=[r[0] for r in q(f"SELECT TABLE_NAME FROM information_schema.tables "
    f"WHERE TABLE_SCHEMA='{DB}' AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME")]

cols=collections.defaultdict(list)
for t in tables:
    for c in q(f"""SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_KEY,
                          REPLACE(REPLACE(EXTRA,'\\t',' '),'\\n',' '),
                          REPLACE(REPLACE(COLUMN_COMMENT,'\\t',' '),'\\n',' ')
                   FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='{DB}' AND TABLE_NAME='{t}'
                   ORDER BY ORDINAL_POSITION""", width=6):
        cols[t].append(c)

# llaves foraneas agrupadas por constraint (para detectar las compuestas)
fks=collections.defaultdict(lambda: {'cols':[],'ref':None,'refcols':[]})
for r in q(f"""SELECT k.CONSTRAINT_NAME, k.TABLE_NAME, k.COLUMN_NAME,
                      k.REFERENCED_TABLE_NAME, k.REFERENCED_COLUMN_NAME
               FROM information_schema.KEY_COLUMN_USAGE k
               WHERE k.TABLE_SCHEMA='{DB}' AND k.REFERENCED_TABLE_NAME IS NOT NULL
               ORDER BY k.CONSTRAINT_NAME, k.ORDINAL_POSITION"""):
    name,tab,col,rt,rc=r
    key=(tab,name)
    fks[key]['cols'].append(col); fks[key]['ref']=rt; fks[key]['refcols'].append(rc)

pks=collections.defaultdict(list)
for r in q(f"""SELECT TABLE_NAME, COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE
               WHERE TABLE_SCHEMA='{DB}' AND CONSTRAINT_NAME='PRIMARY' ORDER BY TABLE_NAME, ORDINAL_POSITION"""):
    pks[r[0]].append(r[1])

# columnas NOT NULL en el lado hijo => relacion obligatoria
def nullable(tab,col):
    for c in cols[tab]:
        if c[0]==col: return c[2]=='YES'
    return True

checks={r[0]:r[1] for r in q(f"""SELECT cc.CONSTRAINT_NAME,
        REPLACE(REPLACE(cc.CHECK_CLAUSE,'\\t',' '),'\\n',' ')
    FROM information_schema.check_constraints cc WHERE cc.CONSTRAINT_SCHEMA='{DB}'
    ORDER BY cc.CONSTRAINT_NAME""", width=2)}
trgs=q(f"""SELECT TRIGGER_NAME, EVENT_MANIPULATION, ACTION_TIMING, EVENT_OBJECT_TABLE
           FROM information_schema.triggers WHERE TRIGGER_SCHEMA='{DB}' ORDER BY TRIGGER_NAME""", width=4)
views=[r[0] for r in q(f"SELECT TABLE_NAME FROM information_schema.views WHERE TABLE_SCHEMA='{DB}' ORDER BY TABLE_NAME")]
counts={t:int(q(f"SELECT COUNT(*) FROM `{t}`")[0][0]) for t in tables}

TYPE={'int':'int','bigint':'bigint','smallint':'smallint','tinyint':'tinyint',
      'varchar':'varchar','datetime':'datetime','date':'date','time':'time'}

out=[]
out.append("# Diagrama Entidad–Relación")
out.append("")
out.append(f"Generado por introspección de `information_schema` sobre la base "
           f"`{DB}` en ejecución, no escrito a mano: refleja la estructura real, "
           f"no la intención.")
out.append("")
out.append(f"**{len(tables)} tablas · {len(views)} vistas · {len(fks)} llaves foráneas · "
           f"{len(checks)} restricciones CHECK · {len(trgs)} triggers**")
out.append("")
# Sin redirección: el script escribe db/diagrama-er.md por su cuenta y el
# resumen sale por stdout. Redirigir stdout a ese mismo archivo lo trunca antes
# y luego le pisa las primeras líneas con el resumen.
out.append(f"<sub>Regenerar: `python3 tools/generar-er.py` · "
           f"última generación {datetime.date.today().isoformat()}</sub>")
out.append("")
out.append("```mermaid")
out.append("erDiagram")

# Relaciones, con la cardinalidad que la base realmente impone.
#
#   Lado del padre  : || si la FK del hijo es NOT NULL (el hijo exige padre),
#                     |o si admite NULL (el hijo puede no tener padre).
#   Lado del hijo   : o| si las columnas de la FK son TODA la clave primaria
#                        del hijo, porque entonces solo puede haber una fila
#                        por padre: es un 1:0..1, como los perfiles de rol.
#                     o{ en el resto. Nunca |{: que un programa tenga cero
#                        asesores es perfectamente legal, y la base no obliga
#                        a ningún padre a tener hijos.
for (tab,name),d in sorted(fks.items()):
    parent = "||" if all(not nullable(tab,c) for c in d['cols']) else "|o"
    child  = "o|" if set(d['cols']) == set(pks.get(tab, [])) else "o{"
    composite = " (compuesta)" if len(d['cols'])>1 else ""
    out.append(f"    {d['ref'].upper()} {parent}--{child} {tab.upper()} : \"{'+'.join(d['cols'])}{composite}\"")

out.append("")
for t in tables:
    if t.startswith('__'): continue
    out.append(f"    {t.upper()} {{")
    for name,dt,nul,key,extra,comment in cols[t]:
        k = 'PK' if name in pks.get(t,[]) else ('FK' if any(name in d['cols'] for (tb,_),d in fks.items() if tb==t) else ('UK' if key=='UNI' else ''))
        notes=[]
        if nul=='YES': notes.append('nullable')
        ex=(extra or '').upper()
        # DEFAULT_GENERATED solo significa "tiene valor por omisión"; columna
        # calculada de verdad es la que dice VIRTUAL/STORED GENERATED.
        if 'VIRTUAL GENERATED' in ex or 'STORED GENERATED' in ex:
            notes.append('calculada por la base')
        elif 'AUTO_INCREMENT' in ex:
            notes.append('autoincremental')
        elif 'DEFAULT_GENERATED' in ex:
            notes.append('valor por omisión')
        if comment: notes.append(comment)
        note=f' "{", ".join(notes)}"' if notes else ''
        out.append(f"        {TYPE.get(dt,dt)} {name}{(' '+k) if k else ''}{note}")
    out.append("    }")
out.append("```")
out.append("")

out.append("## Volumen de los datos de prueba")
out.append("")
out.append("| Tabla | Filas |")
out.append("|-------|-------|")
for t in tables:
    if t.startswith('__'): continue
    out.append(f"| `{t}` | {counts[t]} |")
out.append("")

out.append("## Reglas que impone la base")
out.append("")
out.append("Estas no dependen del código de la aplicación: valen para cualquier "
           "cliente que escriba en la base, incluido un script suelto.")
out.append("")
out.append("### Llaves foráneas compuestas")
out.append("")
for (tab,name),d in sorted(fks.items()):
    if len(d['cols'])>1:
        out.append(f"- `{tab}({', '.join(d['cols'])})` → `{d['ref']}({', '.join(d['refcols'])})`")
out.append("")
out.append("### Restricciones CHECK")
out.append("")
for n,c in sorted(checks.items()):
    out.append(f"- `{n}`: `{c}`")
out.append("")
out.append("### Triggers")
out.append("")
for n,ev,tm,tb in trgs:
    out.append(f"- `{n}` — {tm} {ev} en `{tb}`")
out.append("")
out.append("### Vistas")
out.append("")
for v in views:
    out.append(f"- `{v}`")
out.append("")

# ── Narrativa. Vive aquí y no en un archivo aparte porque el generador
#    sobrescribe el documento entero en cada ejecución; separarla dejaría una
#    segunda copia del diagrama envejeciendo sin que nadie lo note.
out.append("""## Cómo leer las relaciones clave

**Una persona, varios roles.** `PEOPLE` se relaciona con los tres perfiles como
`||--o|`: cero o un perfil de cada tipo, y los tres a la vez si hace falta. Es
lo que permite que un alumno sea también asesor —los asesores pares del
programa— sin duplicar su identidad. El modelo anterior tenía tres tablas de
identidad separadas y el rol lo decidía el orden de los `SELECT`, así que esa
persona quedaba atrapada en uno solo.

**`ADVISORY_SESSIONS` cuelga de dos llaves foráneas compuestas**, y no son
adorno:

- `(AvailabilityId, AdvisorId)` → `AVAILABILITIES (Id, AdvisorId)` hace
  imposible que una sesión declare un asesor distinto al dueño del horario.
- `(TermId, AdvisorId, SubjectId)` → `ADVISOR_SUBJECTS` impide agendar una
  materia que ese asesor no imparte en ese ciclo.

Ambas reglas existían antes solo como `if` en C#, de modo que cualquier
`INSERT` por SQL podía saltárselas.

**`ActiveAt` es el mecanismo de cupo.** Vale la fecha mientras la sesión ocupa
lugar y `NULL` cuando se cancela o se rechaza. Como los `NULL` no colisionan en
un índice único, cancelar libera el asiento sin borrar la fila ni perder el
historial. La mantienen los triggers, no la aplicación.

**`LOCATIONS` absorbe la modalidad.** Antes `availabilities` guardaba
`Modality` y `Location` por separado, y el valor `'Enlace virtual (Meet/Teams)'`
implicaba modalidad virtual: una dependencia transitiva. Ahora la sede declara
su modalidad una sola vez.

**Todo cuelga de un ciclo.** `ACADEMIC_TERMS` aparece en `ADVISOR_SUBJECTS`,
`AVAILABILITIES` y `ADVISORY_SESSIONS`. Antes el ciclo era la cadena
`'FCQI 2026-2'` repetida en las 44 materias, y reasignar borraba el historial
del semestre anterior.

## Lo que el diagrama no dice

El cupo (`MaxCapacity`) se respeta con índices únicos sobre `ActiveAt` más un
trigger que compara el asiento contra el cupo del bloque; un `CHECK` no habría
podido, porque no puede consultar otra tabla.

Que `ScheduledAt` caiga en el día y la hora del bloque **no** lo valida la
base: comprobarlo exige `CONVERT_TZ` entre UTC y `America/Tijuana`, y esa
validación vive en `CreateSessionCommandHandler`, donde la zona horaria es
explícita.

Quién puede ver o modificar cada asesoría tampoco está aquí: son reglas de
autorización, y viven en la capa de aplicación.
""")

io.open('db/diagrama-er.md','w',encoding='utf-8').write("\n".join(out)+"\n")
print(f"generado: {len(tables)} tablas, {len(fks)} FKs, {len(checks)} checks, {len(trgs)} triggers, {len(views)} vistas")
