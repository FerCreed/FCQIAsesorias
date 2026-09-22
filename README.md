# Sistema de Asesorías FCQI

Plataforma para digitalizar las asesorías académicas de la Facultad de Ciencias Químicas e Ingeniería (UABC). El alumno busca materia y solicita cita, el tutor atiende solicitudes y **dirección asigna qué tutores cubren cada materia**.

Catálogo dummy 2026-2 tomado de los horarios oficiales (14 tutores, materias y bloques). UI Avalonia WASM con paleta institucional (verde `#00723F`, oro `#DD971A`), menú horizontal y scroll de página.

## Estructura

| Proyecto | Qué es |
|----------|--------|
| `src/FCQI.Domain` | Entidades (alumno, asesor, materia, horario, cita, admin) |
| `src/FCQI.Application` | Casos de uso y DTOs |
| `src/FCQI.Infrastructure` | EF Core, MySQL, Google/JWT, zona horaria |
| `src/FCQI.Api` | REST + Swagger |
| `src/FCQI.Web` | Pantallas Avalonia |
| `src/FCQI.Web.Browser` | Host en el navegador (WASM) |
| `tests/FCQI.UnitTests` | Pruebas de mapeo, identidad, horario y reglas de negocio |
| `db/` | **Definición de la base: estructura y datos** |
| `tools/` | Utilidades: diagrama ER y pruebas de la API en ejecución |
| `docs/` | [Checklist de validación](docs/checklist-validacion.md) |

GitFlow: `main` (estable), `develop` (integración), `feature/*`.

## Requisitos

- .NET SDK 8 y workload `wasm-tools`
- MySQL 8.0 (servicio Windows `MySQL80`)
- MySQL Workbench (opcional, para ver las tablas a ojo)

## Base de datos

- **Motor:** MySQL 8 en `localhost:3306`
- **Nombre:** `fcqi_asesorias`
- **Usuario:** `root`
- **Contraseña:** la de tu instalación local (User Secrets; no se sube al repo)

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=fcqi_asesorias;User=root;Password=TU_PASSWORD;" --project src/FCQI.Api
```

La base la definen **dos scripts SQL**, no el código. La aplicación ya no crea
ni siembra nada: no hay migraciones de EF Core ni seeder en C#.

| Script | Contenido |
|--------|-----------|
| `db/01-schema.sql` | Estructura: tablas, llaves, índices, triggers y vistas |
| `db/02-seed.sql` | Datos de prueba (21 personas, 44 materias, 104 bloques, 6 citas) |

```bash
mysql -u root -p < db/01-schema.sql
mysql -u root -p < db/02-seed.sql
```

Dentro del contenedor esto es automático: `start-api.sh` los ejecuta contra
MySQL antes de arrancar la API, y solo la primera vez. Para rehacer la base
desde cero sin destruir el volumen, arranca con `FCQI_DB_RESET=1`.

El diagrama entidad-relación está en [`db/diagrama-er.md`](db/diagrama-er.md),
generado por introspección de la base con `python3 tools/generar-er.py`
(regenéralo tras cambiar el esquema). El
modelo anterior y la migración entre ambos quedan en `db/legacy/` y
`db/migrations/`, como registro.

### Horario y zona horaria

`asesorias.ProgramadaEn` guarda **UTC**. La hora local del campus
(`America/Tijuana`) se calcula en la API con las reglas reales de horario de
verano, no con un desplazamiento fijo. La API recibe y devuelve hora local;
`scheduledAtUtc` lleva además el instante absoluto.

### Cómo ver los datos (MySQL Workbench)

1. Abre **MySQL Workbench**.
2. Conéctate a `localhost` / `127.0.0.1`, puerto `3306`, usuario `root`, tu contraseña.
3. En el panel izquierdo (SCHEMAS) abre **`fcqi_asesorias`**.
4. Tablas principales:
   - `personas` — identidad única por correo institucional
   - `perfiles_alumno` / `perfiles_asesor` / `perfiles_directivo` — roles; una
     persona puede tener varios. En los datos de prueba, Vladimir Ramírez
     (`v1299027`) y Jimena Beltrán (`j2207105`) son **asesores pares**: tienen
     perfil de asesor y de alumno a la vez. Son el caso que el modelo anterior
     no podía representar, y con ellos se prueba el selector de rol
   - `materias` — materias
   - `programas` — carreras · `ciclos_escolares` — ciclos escolares
   - `asesores_materias` — qué materias asignó dirección a cada tutor, por ciclo
   - `horarios` — bloques de horario · `lugares` — sedes
   - `asesorias` — solicitudes / citas
   - `historial_estados_sesion` — auditoría de cambios de estado

   Las tablas y las columnas están en español; las clases de C# conservan sus
   nombres en inglés y las enlazan las configuraciones de EF Core
   (`src/FCQI.Infrastructure/Persistence/Configurations/`).

   Vistas útiles: `v_roles_persona` (todos los roles de una persona),
   `v_ocupacion_horarios` (cupo libre por bloque).

Clic derecho en una tabla → **Select Rows - Limit 1000**.

Consultas útiles:

```sql
USE fcqi_asesorias;

SELECT Id, Codigo, Nombre FROM materias ORDER BY Nombre;

-- Asesores con su carrera y modalidad
SELECT * FROM v_asesores ORDER BY NombreCompleto;

-- Materias asignadas a cada asesor en el ciclo vigente
SELECT p.NombreCompleto AS asesor, m.Nombre AS materia
FROM asesores_materias xs
JOIN personas p ON p.Id = xs.AsesorId
JOIN materias m ON m.Id = xs.MateriaId
JOIN ciclos_escolares c ON c.Id = xs.CicloId AND c.EsActual = 1
ORDER BY p.NombreCompleto, m.Nombre;

-- Citas, con la hora convertida a la zona del campus
SELECT s.Id, s.Tema, e.Nombre AS estado,
       CONVERT_TZ(s.ProgramadaEn, 'UTC', 'America/Tijuana') AS hora_local
FROM asesorias s
JOIN estados_sesion e ON e.Id = s.EstadoId
ORDER BY s.ProgramadaEn DESC;

-- Cupo libre por bloque
SELECT * FROM v_ocupacion_horarios WHERE LugaresDisponibles > 0;
```

### Cómo ver los datos (consola)

```powershell
& "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" -u root -p -e "USE fcqi_asesorias; SHOW TABLES; SELECT COUNT(*) AS materias FROM materias; SELECT COUNT(*) AS tutores FROM perfiles_asesor;"
```

(`-p` pide la contraseña.)

## OAuth Google (`@uabc.edu.mx`)

`POST /api/auth/google` valida el token y **rechaza correos que no sean `@uabc.edu.mx`**. Luego busca a la persona en `personas` y devuelve **todos** sus roles en `roles`, no solo el primero: un asesor par puede entrar como asesor o como alumno.

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "TU_CLIENT_ID.apps.googleusercontent.com" --project src/FCQI.Api
```

Orígenes autorizados en Google Cloud: `http://localhost:5016` y `http://localhost:5235`.  
Si el ClientId está vacío, usa el selector de perfiles dummy (`POST /api/auth/demo`).

## Autorización

Salvo `/api/auth/*`, **todos los endpoints exigen un JWT**. El token se obtiene
al iniciar sesión y el cliente lo manda en `Authorization: Bearer`.

| Endpoint | Quién |
|----------|-------|
| `/api/auth/*` | Público: aquí se obtiene el token |
| `/api/subjects`, `/api/advisors` | Cualquier usuario autenticado |
| `GET /api/sessions` | Alumno ve las suyas, asesor las que le tocan, dirección todas |
| `POST /api/sessions` | Alumno (o dirección, en ventanilla) |
| `PATCH /api/sessions/{id}/status` | El asesor dueño confirma o rechaza; el alumno dueño solo cancela; dirección todo. Cancelada y Rechazada son estados finales |
| `/api/admin/*` | Solo rol Directivo |

Cuatro reglas que conviene tener claras:

**La identidad sale del token, nunca del cuerpo.** `studentId` en el cuerpo de
`POST /api/sessions` se ignora salvo que quien llame sea dirección. Antes se
usaba tal cual, lo que permitía agendar a nombre de otra persona.

**El token lleva todos los roles.** Un asesor par viaja con `Asesor` y
`Alumno`, y puede actuar como cualquiera de los dos sin volver a autenticarse.
La interfaz lo aprovecha con un **selector «Entrar como»** en el encabezado,
que solo aparece si la persona tiene más de un rol: cambiarlo cambia el menú,
las pantallas y la lista de asesorías, sin pedir el token otra vez. Quien tiene
un solo rol no ve el selector.

**Cancelada y Rechazada son estados finales.** Confirmar una asesoría cancelada
la devolvía a la vida y volvía a ocupar el lugar; si otro alumno ya lo había
tomado, la API respondía 500. Ahora se rechaza con un mensaje: si hace falta la
cita, se solicita de nuevo.

**Nadie se asesora a sí mismo.** Un asesor par aparece en su propia lista de
tutores; agendarse consigo mismo se rechaza.

El **login de demostración** (`/api/auth/demo*`) expone el directorio de
personas del programa, así que solo existe mientras `Authentication:Google:ClientId`
esté vacío. En cuanto se configura OAuth, esos dos endpoints devuelven 404.

## Ejecutar

Si sale error de DLL “being used by another process”, ya hay una API corriendo: Ctrl+C en esa terminal o cierra `FCQI.Api` en el Administrador de tareas.

Terminal 1 — API (Swagger: http://localhost:5016/swagger):

```powershell
dotnet run --project src/FCQI.Api --launch-profile http
```

Terminal 2 — UI (http://localhost:5235):

```powershell
dotnet run --project src/FCQI.Web.Browser
```

## Comprobar que funciona

El recorrido completo, paso a paso, está en
**[docs/checklist-validacion.md](docs/checklist-validacion.md)**. En resumen:

```bash
dotnet test tests/FCQI.UnitTests          # 56 pruebas de las reglas, sin base ni red
python3 tools/probar-api.py               # 58 pruebas contra la API en :5080
```

`tools/probar-api.py` ejerce la API completa —autenticación, autorización,
catálogo, cupo, cambios de estado y panel de dirección— contra la base sembrada,
y cancela al terminar todo lo que creó. Con la API nativa en el puerto 5016:
`API=http://localhost:5016 python3 tools/probar-api.py`.

Prueba rápida a mano: entra como **Alumno: Yesua** → Buscar → Cálculo
Diferencial → solicita → Salir. Luego entra como **Alumno: Jimena Beltrán**
(`j2207105`), que es asesora par: cambia el selector «Entrar como» del
encabezado a **Asesor** y verás su bandeja de solicitudes sin haber salido.
