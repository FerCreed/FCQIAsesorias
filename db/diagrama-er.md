# Diagrama Entidad–Relación

Generado por introspección de `information_schema` sobre la base `fcqi_asesorias` en ejecución, no escrito a mano: refleja la estructura real, no la intención.

**15 tablas · 4 vistas · 23 llaves foráneas · 6 restricciones CHECK · 4 triggers**

<sub>Regenerar: `python3 tools/generar-er.py` · última generación 2026-09-21</sub>

```mermaid
erDiagram
    PERFILES_ASESOR ||--o{ ASESORES_MATERIAS : "AsesorId"
    CICLOS_ESCOLARES ||--o{ ASESORES_MATERIAS : "CicloId"
    MATERIAS ||--o{ ASESORES_MATERIAS : "MateriaId"
    PERFILES_ALUMNO ||--o{ ASESORIAS : "AlumnoId"
    ESTADOS_SESION ||--o{ ASESORIAS : "EstadoId"
    HORARIOS ||--o{ ASESORIAS : "HorarioId+AsesorId (compuesta)"
    ASESORES_MATERIAS ||--o{ ASESORIAS : "CicloId+AsesorId+MateriaId (compuesta)"
    ASESORIAS ||--o{ HISTORIAL_ESTADOS_SESION : "AsesoriaId"
    ESTADOS_SESION |o--o{ HISTORIAL_ESTADOS_SESION : "EstadoAnteriorId"
    ESTADOS_SESION ||--o{ HISTORIAL_ESTADOS_SESION : "EstadoNuevoId"
    PERSONAS |o--o{ HISTORIAL_ESTADOS_SESION : "CambiadoPor"
    PERFILES_ASESOR ||--o{ HORARIOS : "AsesorId"
    CICLOS_ESCOLARES ||--o{ HORARIOS : "CicloId"
    LUGARES ||--o{ HORARIOS : "LugarId"
    MODALIDADES ||--o{ LUGARES : "ModalidadId"
    PERSONAS ||--o| PERFILES_ALUMNO : "PersonaId"
    PROGRAMAS |o--o{ PERFILES_ALUMNO : "ProgramaId"
    MODALIDADES ||--o{ PERFILES_ASESOR : "ModalidadPredeterminadaId"
    PERSONAS ||--o| PERFILES_ASESOR : "PersonaId"
    PROGRAMAS ||--o{ PERFILES_ASESOR : "ProgramaId"
    PERSONAS ||--o| PERFILES_DIRECTIVO : "PersonaId"
    MATERIAS ||--o{ PROGRAMAS_MATERIAS : "MateriaId"
    PROGRAMAS ||--o{ PROGRAMAS_MATERIAS : "ProgramaId"

    ASESORES_MATERIAS {
        smallint CicloId PK
        int AsesorId PK
        int MateriaId PK
        datetime CreadoEn "valor por omisión"
    }
    ASESORIAS {
        bigint Id PK "autoincremental"
        smallint CicloId FK
        int HorarioId FK
        int AsesorId FK
        int AlumnoId FK
        int MateriaId FK
        datetime ProgramadaEn "UTC; la hora local es America/Tijuana"
        smallint NumeroLugar
        tinyint EstadoId FK
        varchar Tema
        datetime CreadoEn "valor por omisión"
        datetime ActualizadoEn "valor por omisión"
        datetime ActivaEn "nullable"
    }
    CICLOS_ESCOLARES {
        smallint Id PK "autoincremental"
        varchar Codigo UK "p. ej. 2026-2"
        varchar Nombre
        date FechaInicio
        date FechaFin
        tinyint EsActual
    }
    ESTADOS_SESION {
        tinyint Id PK
        varchar Codigo UK
        varchar Nombre
        tinyint Activo
    }
    HISTORIAL_ESTADOS_SESION {
        bigint Id PK "autoincremental"
        bigint AsesoriaId FK
        tinyint EstadoAnteriorId FK "nullable, NULL = creación"
        tinyint EstadoNuevoId FK
        int CambiadoPor FK "nullable, NULL = proceso automático"
        datetime CambiadoEn "valor por omisión"
        varchar Motivo "nullable"
    }
    HORARIOS {
        int Id PK "autoincremental"
        smallint CicloId FK
        int AsesorId FK
        tinyint DiaSemana
        time HoraInicio
        time HoraFin
        smallint CupoMaximo
        smallint LugarId FK
        tinyint Activo
        datetime CreadoEn "valor por omisión"
    }
    LUGARES {
        smallint Id PK "autoincremental"
        varchar Nombre UK
        tinyint ModalidadId FK
        varchar Detalles "nullable"
        tinyint Activo
    }
    MATERIAS {
        int Id PK "autoincremental"
        varchar Codigo UK
        varchar Nombre
        tinyint Activo
    }
    MODALIDADES {
        tinyint Id PK
        varchar Codigo UK
        varchar Nombre
    }
    PERFILES_ALUMNO {
        int PersonaId PK
        varchar Matricula UK
        smallint ProgramaId FK "nullable"
        tinyint Activo
        datetime CreadoEn "valor por omisión"
    }
    PERFILES_ASESOR {
        int PersonaId PK
        smallint ProgramaId FK "era advisors.Area"
        tinyint ModalidadPredeterminadaId FK
        tinyint Activo
        datetime CreadoEn "valor por omisión"
    }
    PERFILES_DIRECTIVO {
        int PersonaId PK
        varchar Cargo
        tinyint Activo
        datetime CreadoEn "valor por omisión"
    }
    PERSONAS {
        int Id PK "autoincremental"
        varchar Tratamiento "nullable, Dra., Dr., Mtro. — opcional"
        varchar Nombres
        varchar ApellidoPaterno
        varchar ApellidoMaterno "nullable"
        varchar NombreCompleto "calculada por la base"
        varchar Correo UK
        tinyint Activo
        datetime CreadoEn "valor por omisión"
        datetime ActualizadoEn "valor por omisión"
    }
    PROGRAMAS {
        smallint Id PK "autoincremental"
        varchar Codigo UK
        varchar Nombre
        tinyint Activo
    }
    PROGRAMAS_MATERIAS {
        smallint ProgramaId PK
        int MateriaId PK
        tinyint Semestre "nullable, semestre sugerido del plan"
    }
```

## Volumen de los datos de prueba

| Tabla | Filas |
|-------|-------|
| `asesores_materias` | 92 |
| `asesorias` | 6 |
| `ciclos_escolares` | 1 |
| `estados_sesion` | 4 |
| `historial_estados_sesion` | 6 |
| `horarios` | 104 |
| `lugares` | 2 |
| `materias` | 44 |
| `modalidades` | 2 |
| `perfiles_alumno` | 8 |
| `perfiles_asesor` | 14 |
| `perfiles_directivo` | 1 |
| `personas` | 21 |
| `programas` | 5 |
| `programas_materias` | 57 |

## Reglas que impone la base

Estas no dependen del código de la aplicación: valen para cualquier cliente que escriba en la base, incluido un script suelto.

### Llaves foráneas compuestas

- `asesorias(HorarioId, AsesorId)` → `horarios(Id, AsesorId)`
- `asesorias(CicloId, AsesorId, MateriaId)` → `asesores_materias(CicloId, AsesorId, MateriaId)`

### Restricciones CHECK

- `CK_asesorias_Lugar`: `(`NumeroLugar` >= 1)`
- `CK_ciclos_escolares_Rango`: `(`FechaFin` > `FechaInicio`)`
- `CK_horarios_Cupo`: `(`CupoMaximo` >= 1)`
- `CK_horarios_DiaSemana`: `(`DiaSemana` between 0 and 6)`
- `CK_horarios_RangoHoras`: `(`HoraFin` > `HoraInicio`)`
- `CK_personas_CorreoInstitucional`: `((`Correo` like _utf8mb4\\'%@uabc.edu.mx\\') and (`Correo` = lower(`Correo`)))`

### Triggers

- `TRG_asesorias_antes_actualizar` — BEFORE UPDATE en `asesorias`
- `TRG_asesorias_antes_insertar` — BEFORE INSERT en `asesorias`
- `TRG_asesorias_historial_actualizar` — AFTER UPDATE en `asesorias`
- `TRG_asesorias_historial_insertar` — AFTER INSERT en `asesorias`

### Vistas

- `v_alumnos`
- `v_asesores`
- `v_ocupacion_horarios`
- `v_roles_persona`

## Cómo leer las relaciones clave

**Una persona, varios roles.** `PERSONAS` se relaciona con los tres perfiles como
`||--o|`: cero o un perfil de cada tipo, y los tres a la vez si hace falta. Es
lo que permite que un alumno sea también asesor —los asesores pares del
programa— sin duplicar su identidad. El modelo anterior tenía tres tablas de
identidad separadas y el rol lo decidía el orden de los `SELECT`, así que esa
persona quedaba atrapada en uno solo.

**`ASESORIAS` cuelga de dos llaves foráneas compuestas**, y no son
adorno:

- `(HorarioId, AsesorId)` → `HORARIOS (Id, AsesorId)` hace
  imposible que una sesión declare un asesor distinto al dueño del horario.
- `(CicloId, AsesorId, MateriaId)` → `ASESORES_MATERIAS` impide agendar una
  materia que ese asesor no imparte en ese ciclo.

Ambas reglas existían antes solo como `if` en C#, de modo que cualquier
`INSERT` por SQL podía saltárselas.

**`ActivaEn` es el mecanismo de cupo.** Vale la fecha mientras la sesión ocupa
lugar y `NULL` cuando se cancela o se rechaza. Como los `NULL` no colisionan en
un índice único, cancelar libera el asiento sin borrar la fila ni perder el
historial. La mantienen los triggers, no la aplicación.

**`LUGARES` absorbe la modalidad.** Antes `availabilities` guardaba
`Modality` y `Location` por separado, y el valor `'Enlace virtual (Meet/Teams)'`
implicaba modalidad virtual: una dependencia transitiva. Ahora la sede declara
su modalidad una sola vez.

**Todo cuelga de un ciclo.** `CICLOS_ESCOLARES` aparece en
`ASESORES_MATERIAS`, `HORARIOS` y `ASESORIAS`. Antes el ciclo era la cadena
`'FCQI 2026-2'` repetida en las 44 materias, y reasignar borraba el historial
del semestre anterior.

## Lo que el diagrama no dice

El cupo (`CupoMaximo`) se respeta con índices únicos sobre `ActivaEn` más un
trigger que compara el asiento contra el cupo del bloque; un `CHECK` no habría
podido, porque no puede consultar otra tabla.

Que `ProgramadaEn` caiga en el día y la hora del bloque **no** lo valida la
base: comprobarlo exige `CONVERT_TZ` entre UTC y `America/Tijuana`, y esa
validación vive en `CreateSessionCommandHandler`, donde la zona horaria es
explícita.

Quién puede ver o modificar cada asesoría tampoco está aquí: son reglas de
autorización, y viven en la capa de aplicación.

