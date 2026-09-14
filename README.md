# Sistema de Asesorías FCQI

Plataforma para digitalizar las asesorías académicas de la Facultad de Ciencias Químicas e Ingeniería (UABC). El alumno busca materia y solicita cita, el tutor atiende solicitudes y **dirección asigna qué tutores cubren cada materia**.

Catálogo dummy 2026-2 tomado de los horarios oficiales (14 tutores, materias y bloques). UI Avalonia WASM con paleta institucional (verde `#00723F`, oro `#DD971A`), menú horizontal y scroll de página.

## Estructura

| Proyecto | Qué es |
|----------|--------|
| `src/FCQI.Domain` | Entidades (alumno, asesor, materia, horario, cita, admin) |
| `src/FCQI.Application` | Casos de uso y DTOs |
| `src/FCQI.Infrastructure` | EF Core, MySQL, seeder, Google/JWT |
| `src/FCQI.Api` | REST + Swagger |
| `src/FCQI.Web` | Pantallas Avalonia |
| `src/FCQI.Web.Browser` | Host en el navegador (WASM) |
| `tests/FCQI.UnitTests` | Pruebas de mapeo |

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

Al arrancar la API (`dotnet run`) se aplican migraciones y, si no hay tutores, se carga el seeder 2026-2. También puedes aplicar a mano:

```powershell
dotnet ef database update --project src/FCQI.Infrastructure --startup-project src/FCQI.Api
```

### Cómo ver los datos (MySQL Workbench)

1. Abre **MySQL Workbench**.
2. Conéctate a `localhost` / `127.0.0.1`, puerto `3306`, usuario `root`, tu contraseña.
3. En el panel izquierdo (SCHEMAS) abre **`fcqi_asesorias`**.
4. Tablas principales:
   - `subjects` — materias
   - `advisors` — tutores
   - `advisor_subjects` — qué materias asignó dirección a cada tutor
   - `availabilities` — bloques de horario
   - `students` — alumnos dummy
   - `admins` — directivo(s)
   - `advisory_sessions` — solicitudes / citas
   - `__efmigrationshistory` — migraciones EF

Clic derecho en una tabla → **Select Rows - Limit 1000**.

Consultas útiles:

```sql
USE fcqi_asesorias;

SELECT id, code, name FROM subjects ORDER BY name;
SELECT id, full_name, email, area FROM advisors;
SELECT a.full_name, s.name AS materia
FROM advisor_subjects xs
JOIN advisors a ON a.id = xs.advisor_id
JOIN subjects s ON s.id = xs.subject_id
ORDER BY a.full_name, s.name;

SELECT id, topic, status, scheduled_at FROM advisory_sessions;
```

### Cómo ver los datos (consola)

```powershell
& "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" -u root -p -e "USE fcqi_asesorias; SHOW TABLES; SELECT COUNT(*) AS materias FROM subjects; SELECT COUNT(*) AS tutores FROM advisors;"
```

(`-p` pide la contraseña.)

## OAuth Google (`@uabc.edu.mx`)

`POST /api/auth/google` valida el token y **rechaza correos que no sean `@uabc.edu.mx`**. Luego busca al usuario en alumnos, asesores o directivos.

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "TU_CLIENT_ID.apps.googleusercontent.com" --project src/FCQI.Api
```

Orígenes autorizados en Google Cloud: `http://localhost:5016` y `http://localhost:5235`.  
Si el ClientId está vacío, usa el selector de perfiles dummy (`POST /api/auth/demo`).

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

```powershell
dotnet test tests/FCQI.UnitTests
```

Prueba rápida: entra como alumno Yesua → Buscar → Cálculo Diferencial → solicita → Salir → entra como Felipe Márquez → Solicitudes.
