# Sistema de Asesorías FCQI

Plataforma académica para digitalizar la gestión de asesorías de la Facultad de Ciencias Químicas e Ingeniería (UABC). Este repositorio cubre el **Sprint 1**: arquitectura por capas, MySQL, API REST y un cliente Avalonia WASM.

## Estructura

| Proyecto | Capa |
|----------|------|
| `src/FCQI.Domain` | Entidades puras |
| `src/FCQI.Application` | Casos de uso y DTOs |
| `src/FCQI.Infrastructure` | EF Core + MySQL |
| `src/FCQI.Api` | ASP.NET Core + Swagger |
| `src/FCQI.Web` | UI Avalonia (MVVM) |
| `src/FCQI.Web.Browser` | Host WebAssembly |
| `tests/FCQI.UnitTests` | Pruebas de mapeo EF |

## GitFlow

- `main`: entregables estables de sprint
- `develop`: integración del equipo
- `feature/*`: trabajo individual → PR a `develop`

## Requisitos

- .NET SDK 8
- Workload `wasm-tools`
- MySQL 8.0 (servicio `MySQL80`)

## Base de datos

1. Inicia MySQL (Servicios de Windows → `MySQL80`).
2. Ajusta la cadena en `src/FCQI.Api/appsettings.Development.json` o con User Secrets:

```powershell
dotnet user-secrets init --project src/FCQI.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=fcqi_asesorias;User=root;Password=TU_PASSWORD;" --project src/FCQI.Api
```

3. Aplica la migración:

```powershell
dotnet ef database update --project src/FCQI.Infrastructure --startup-project src/FCQI.Api
```

## Ejecutar

Terminal 1 — API (Swagger en http://localhost:5016/swagger):

```powershell
dotnet run --project src/FCQI.Api --launch-profile http
```

Terminal 2 — frontend WASM (http://localhost:5235):

```powershell
dotnet run --project src/FCQI.Web.Browser
```

Pruebas:

```powershell
dotnet test tests/FCQI.UnitTests
```