# Especificación de Requisitos de Software (SRS)
## Sistema de Asesorías Académicas FCQI

| Campo | Valor |
|-------|-------|
| Organización | Facultad de Ciencias Químicas e Ingeniería (FCQI), UABC Tijuana |
| Versión del documento | 1.1 |
| Fecha | 7 de noviembre de 2026 |
| Estándar de referencia | ISO/IEC/IEEE 29148:2018 (sucesor de IEEE 830) |
| Fuente de los procesos | Diagramas BPMN en `AsesoriasBizagi/` (Diagrama 1 a 19) |

---

## 1. Introducción

### 1.1 Propósito

Este documento define qué debe hacer el Sistema de Asesorías Académicas de la
FCQI y bajo qué condiciones. Lo usan el equipo de desarrollo para implementar y
probar, la coordinación del programa para validar que el sistema cubre sus
procesos, y quien mantenga el sistema después.

### 1.2 Alcance

El sistema digitaliza el programa de asesorías del ciclo escolar vigente:

- El **alumno** consulta materias, asesores y horarios sin cuenta; con su correo
  `@uabc.edu.mx` solicita, cancela o reprograma citas y consulta su historial.
- El **asesor** (profesor o asesor estudiante) publica sus horarios semanales,
  confirma o rechaza solicitudes y registra asistencia y bitácora.
- **Dirección** asigna qué materias cubre cada asesor y obtiene reportes y
  constancias de horas.
- El **sistema** valida reglas, notifica por correo, genera enlaces de reunión
  virtual y archivos de calendario.

Pendiente de esta versión: ampliar el catálogo con los horarios reales de la
facultad (dos PDF en Google Drive), a la espera de autorizar su descarga.

Fuera de alcance de esta versión: conexión con el directorio de cuentas UABC
(la contraseña no se verifica todavía, ver RNF-05), generación de reuniones de
Google Meet por cita (hay un puerto listo, ver §5.3) y aplicaciones nativas
para Android/iOS (en el teléfono se usa el navegador, ver RNF-01).

### 1.3 Definiciones

| Término | Significado |
|---------|-------------|
| Asesoría / cita | Sesión entre un alumno y un asesor sobre una materia, en un bloque de horario y fecha concretos. |
| Bloque de horario | Franja semanal recurrente de un asesor (p. ej. miércoles 12:00–14:00) con cupo y sede. |
| Ciclo escolar | Periodo académico (p. ej. 2026-2). Todo dato operativo cuelga del ciclo vigente. |
| Asesor par | Alumno que también asesora; tiene los roles Alumno y Asesor en la misma cuenta. |
| Bitácora | Registro del asesor al terminar: si el alumno asistió y notas de avance. |
| Hora del campus | Hora local de America/Tijuana. La base guarda UTC. |
| BPMN *n* | Diagrama de proceso *n* en `AsesoriasBizagi/Diagrama n.bpmn`. |

### 1.4 Referencias

- `AsesoriasBizagi/*.bpmn` — modelos de proceso (Bizagi Modeler).
- `db/01-schema.sql`, `db/diagrama-er.md` — modelo de datos.
- `docs/checklist-validacion.md` — procedimiento de validación de una entrega.
- `README.md` — instalación y ejecución.

---

## 2. Descripción general

### 2.1 Perspectiva del producto

Sistema web autónomo en cuatro capas, desplegado como un solo contenedor:

```
 Navegador (WASM) ─┐                      ┌─ MySQL 8 (datos)
 Escritorio ───────┼─ HTTPS ─ nginx ─ API ─┤
 (Win/macOS/Linux) ┘        (mismo origen) └─ SMTP (correo, opcional)
```

| Proyecto | Capa | Responsabilidad |
|----------|------|-----------------|
| `FCQI.Domain` | Dominio | Entidades y ciclo de vida de la asesoría (`SessionState`). |
| `FCQI.Application` | Aplicación | Casos de uso (handlers), reglas de autorización, eventos y puertos externos. |
| `FCQI.Infrastructure` | Infraestructura | EF Core + MySQL, JWT, reloj del campus, adaptadores de correo y reuniones. |
| `FCQI.Api` | Presentación | REST + Swagger; traduce errores de negocio a HTTP. |
| `FCQI.Web` | Cliente | Interfaz Avalonia compartida (MVVM). |
| `FCQI.Web.Browser` / `FCQI.Desktop` | Anfitriones | Ejecutan el mismo cliente en navegador (WebAssembly) o en escritorio. |

### 2.2 Usuarios

| Actor | Descripción | Acceso |
|-------|-------------|--------|
| Visitante | Cualquier persona sin sesión. | Catálogo de solo lectura. |
| Alumno | Estudiante dado de alta en el programa. | Solicitar, cancelar, reprogramar, historial, contactar asesor. |
| Asesor | Asesor profesor o asesor estudiante. | Horarios propios, bandeja de solicitudes, bitácora. |
| Directivo | Dirección o coordinación del programa. | Asignación de materias, reportes; puede actuar sobre cualquier asesoría. |
| Sistema | Procesos automáticos. | Validaciones, notificaciones, enlaces, historial de estados. |

Una persona puede tener varios roles; elige con cuál trabajar en el selector
"rol" de la cabecera sin volver a entrar.

### 2.3 Restricciones

- .NET 8 LTS, Avalonia 11.3, MySQL 8.0/8.4, Docker (amd64 y arm64).
- Todas las dependencias se obtienen por Docker; el equipo de desarrollo no
  necesita MySQL ni herramientas instaladas para ejecutar el sistema.
- El esquema de la base lo definen scripts SQL versionados, no la aplicación.

### 2.4 Suposiciones

- Existe exactamente un ciclo escolar marcado como vigente.
- Las cuentas de correo de los participantes son `@uabc.edu.mx`.

---

## 3. Requisitos funcionales

Prioridad: **A** imprescindible, **M** importante. Estado: **✔** implementado,
**◐** implementado con adaptador por omisión (ver §5.3).

### 3.1 Acceso

| ID | Requisito | BPMN | Prioridad | Estado |
|----|-----------|------|-----------|--------|
| RF-01 | El sistema debe permitir entrar con un correo `@uabc.edu.mx`; rechaza otros dominios antes de llamar al servidor. | 1 | A | ✔ |
| RF-02 | El sistema debe resolver todos los roles activos de la persona y emitir un token JWT con ellos; si no tiene ninguno, niega el acceso con un mensaje claro. | 1 | A | ✔ |
| RF-03 | Si la persona tiene más de un rol, debe poder cambiar de rol sin volver a entrar. | 1 | A | ✔ |
| RF-04 | Al salir, el cliente debe borrar el token, limpiar listas y selecciones y regresar al inicio. | 17 | A | ✔ |
| RF-05 | Si una acción requiere sesión, el sistema lleva al acceso y, al entrar, regresa a la pantalla de origen y retoma la solicitud pendiente. | 1, 8 | M | ✔ |

### 3.2 Catálogo y solicitud (alumno)

| ID | Requisito | BPMN | Prioridad | Estado |
|----|-----------|------|-----------|--------|
| RF-10 | Cualquier visitante debe poder buscar materias por nombre o clave. | 5 | A | ✔ |
| RF-11 | Al elegir materia, se muestran solo los asesores que dirección asignó a esa materia en el ciclo vigente; si no hay, se indica. | 4, 5 | A | ✔ |
| RF-12 | Al elegir asesor, se muestran sus bloques activos con día, hora, modalidad y sede; si no hay, se indica. | 3 | A | ✔ |
| RF-13 | El alumno solicita un bloque con un tema. El sistema valida: alumno activo, asesor asignado a la materia, bloque activo del asesor y del ciclo, fecha futura dentro del ciclo y en el día y hora del bloque, cupo libre, sin duplicado, y que no sea consigo mismo. La cita queda **Pendiente**. | 8 | A | ✔ |
| RF-15 | Las listas se pueden filtrar: materias por texto, programa educativo y "solo con asesores"; asesores por tipo (profesor o estudiante), modalidad y días con horario; horarios por día, franja del día (mañana, tarde, noche) y modalidad. Cada lista indica "N de M" resultados y ofrece "Limpiar filtros". | 8 | A | ✔ |
| RF-16 | La fecha de la cita se elige en un calendario que solo habilita las fechas del ciclo que caen en el día del bloque elegido. | 8, 11 | A | ✔ |
| RF-14 | El alumno puede escribir al asesor; el mensaje (1–2000 caracteres) llega a su correo institucional con respuesta directa al alumno. Máximo 5 mensajes por minuto por persona (HTTP 429 al exceder). | 15 | M | ◐ |

### 3.3 Ciclo de vida de la asesoría

```
Pendiente ──confirmar (asesor)──▶ Confirmada ──bitácora (asesor)──▶ Realizada
    │  └──rechazar (asesor)──▶ Rechazada         │
    └──cancelar (alumno/asesor)──▶ Cancelada ◀────┘ cancelar
Cancelada, Rechazada y Realizada son finales.  Dirección: cualquier transición válida.
```

| ID | Requisito | BPMN | Prioridad | Estado |
|----|-----------|------|-----------|--------|
| RF-20 | El asesor dueño puede confirmar o rechazar una solicitud pendiente; el alumno no. | 12, 18 | A | ✔ |
| RF-21 | El alumno puede cancelar su asesoría pendiente o confirmada hasta *N* horas antes (`Sessions:ChangeLimitHours`, 24 por omisión). El asesor puede cancelar sin ese límite. | 11 | A | ✔ |
| RF-22 | El alumno puede reprogramar a otro bloque del mismo asesor con el mismo límite; la cita nueva queda Pendiente y la anterior Cancelada. | 11 | M | ✔ |
| RF-23 | Un estado final no puede cambiar; se informa que hay que solicitar de nuevo. | 11, 12, 18 | A | ✔ |
| RF-24 | Cada cambio de estado se registra en `historial_estados_sesion` (por trigger), con la persona que hizo el cambio, y libera el cupo cuando corresponde. | 11, 12, 18 | A | ✔ |
| RF-25 | En cada cambio de estado se notifica por correo, en paralelo, al alumno y al asesor de la cita, nombrando a quien actuó. | 8, 11, 12, 18 | M | ◐ |
| RF-26 | Al confirmar una cita virtual se genera y guarda su enlace de reunión; las presenciales no llevan enlace. | 10 | M | ◐ |
| RF-27 | Cualquier participante puede descargar la cita como archivo iCalendar (`.ics`) para su calendario personal o institucional. | 12 | M | ✔ |
| RF-28 | El asesor registra bitácora (asistencia y notas, ≤ 2000 caracteres) de una cita confirmada que ya empezó; la cita queda **Realizada**. | 9 | A | ✔ |

### 3.4 Consultas

| ID | Requisito | BPMN | Prioridad | Estado |
|----|-----------|------|-----------|--------|
| RF-30 | "Mis asesorías" muestra las próximas citas vivas (pendientes y confirmadas), de la más cercana a la más lejana. | 19 | A | ✔ |
| RF-31 | El historial muestra las citas pasadas o cerradas, de la más reciente a la más antigua. | 14 | A | ✔ |
| RF-32 | Las fechas se muestran en hora del campus aunque la base guarde UTC. | 14, 19 | A | ✔ |
| RF-33 | Cada persona solo consulta sus propias asesorías; pedir las de otra devuelve 403. Dirección ve todas. | 14, 19 | A | ✔ |

### 3.5 Horarios del asesor

| ID | Requisito | BPMN | Prioridad | Estado |
|----|-----------|------|-----------|--------|
| RF-40 | El asesor publica bloques semanales (día, inicio, fin, cupo 1–30, sede) en el ciclo vigente. Se rechazan rangos inválidos y traslapes con sus otros bloques activos. | 7 | A | ✔ |
| RF-41 | El asesor edita un bloque con las mismas validaciones; no puede mover día u hora si hay citas por venir, ni bajar el cupo por debajo de los lugares ocupados. | 13 | A | ✔ |
| RF-42 | El asesor retira un bloque (baja lógica) solo si no tiene citas por venir. Volver a publicar el mismo día y hora reactiva el bloque. | 16 | A | ✔ |

### 3.6 Dirección

| ID | Requisito | BPMN | Prioridad | Estado |
|----|-----------|------|-----------|--------|
| RF-50 | Dirección asigna las materias que cubre cada asesor en el ciclo; se validan materias existentes y no se retira una materia con asesorías registradas. | 2 | A | ✔ |
| RF-51 | Dirección consulta un tablero de las asesorías creadas en un periodo (fechas en calendario, filtros por asesor, materia y estado): totales por estado, % de asistencia e inasistencia, % de cancelación y rechazo, horas trabajadas, anticipación promedio de la solicitud, y desgloses por asesor, materia, día de la semana y modalidad. Sin datos, se indica. | 6 | A | ✔ |
| RF-52 | Cada participante de una asesoría (alumno y asesor) y dirección pueden descargar su **reporte individual** en PDF: datos completos, historial de estados y bitácora. Nadie más puede. | 6, 14, 19 | A | ✔ |
| RF-53 | El asesor descarga su **resumen de asesorías y horas trabajadas** del periodo (PDF y CSV); dirección puede descargar el de cualquier asesor. Las horas cuentan solo asesorías realizadas con asistencia. | 6 | A | ✔ |
| RF-54 | Dirección descarga **todas las asesorías creadas a partir de una fecha** (y hasta otra opcional) en CSV (25 columnas por asesoría) y en PDF (métricas, desgloses y anexo con cada asesoría). Los reportes se calculan sobre las asesorías, su bitácora y su historial, no sobre bitácoras técnicas del programa. | 6 | A | ✔ |

---

## 4. Requisitos no funcionales

| ID | Categoría | Requisito | Cómo se cumple |
|----|-----------|-----------|----------------|
| RNF-01 | Portabilidad / adaptabilidad | La interfaz debe usarse en teléfonos en vertical desde 360 px de ancho y en escritorio, sin desplazamiento horizontal. | `ContainerQuery` de Avalonia: < 640 px usa cabecera compacta y pestañas inferiores; tarjetas a ancho completo; objetivos táctiles ≥ 44 px; área segura del teléfono en `app.css`. |
| RNF-02 | Portabilidad | El mismo cliente debe ejecutarse en navegador y en escritorio (Windows, macOS, Linux). | `FCQI.Web` compartido; anfitriones `FCQI.Web.Browser` (WebAssembly) y `FCQI.Desktop`. |
| RNF-03 | Despliegue | El sistema completo debe levantarse con un solo comando y sin software instalado más allá de Docker. | `docker compose up --build`; imagen multiarquitectura con MySQL, API, nginx y frontend. |
| RNF-04 | Seguridad | La identidad sale del token firmado, nunca del cuerpo de la petición. Cada operación valida dueño y rol. | `CurrentUser` → `Caller`; reglas en la capa de aplicación con pruebas. |
| RNF-05 | Seguridad | Solo cuentas `@uabc.edu.mx` dadas de alta. El acceso sin contraseña (correo solamente) existe para pruebas y está **apagado por omisión fuera de desarrollo**; con la app publicada (Cloudflare Tunnel) el acceso debe ser con Google. | `Authentication:PasswordlessLogin` (`FCQI_PASSWORDLESS_LOGIN` en `.env`, `false` por omisión en producción); sin él `/login` y `/demo` responden 403. **Pendiente:** botón de Google en el cliente y verificación contra el directorio UABC. |
| RNF-06 | Integridad | La base impide sobrecupo, citas con un asesor que no es dueño del bloque y estados inválidos, aunque falle la aplicación. | Llaves compuestas, índices únicos y triggers en `01-schema.sql`. |
| RNF-07 | Exactitud temporal | Las citas no se corren con el horario de verano. | UTC en la base; `CampusClock` convierte con las reglas de America/Tijuana. |
| RNF-08 | Disponibilidad | Un fallo de correo o de enlace no debe impedir el cambio de estado. | Los observadores corren después de guardar y sus errores se registran sin propagarse. |
| RNF-09 | Usabilidad | Todo error de negocio se muestra en español, con la causa y qué hacer; los fallos internos no exponen detalles. | Las reglas lanzan `BusinessRuleException`; `ApiExceptionFilter` la traduce a 400 con `{ message }` y deja el resto como 500 en el log. |
| RNF-10 | Mantenibilidad | Capas desacopladas, patrones de diseño donde aportan y pruebas automáticas de las reglas de negocio. | Ver §6 y §8. |
| RNF-12 | Accesibilidad | Zoom de la interfaz de 80 % a 200 % (botones A− / A+ y Ctrl/Cmd + − 0); foco de teclado visible; contraste AA; objetivos táctiles de 44 px o más; nombres accesibles en los botones con ícono; cada elemento pulsable cambia al pasar el cursor y al presionarse. | Zoom con `LayoutTransformControl`; estilos globales en `App.axaml`; control `IconText`. |
| RNF-13 | Usabilidad | Íconos Fluent System Icons de Microsoft en menú, botones y listas; aspecto institucional (encabezado blanco, banda verde, fondo papel). | `Themes/Icons.axaml` (trazos oficiales, licencia MIT). |
| RNF-14 | Vocabulario | El rol se llama **asesor** en todo el sistema ("tutor" es otro rol en la facultad). El tipo de asesor es "Profesor" o "Estudiante". | Textos, API, correos, reportes y documentación. |
| RNF-11 | Rendimiento | Respuesta < 1 s para el catálogo y las consultas con los volúmenes de un ciclo (miles de citas). | Índices por asesor, alumno, ciclo y estado; caché de materias y sedes en el cliente. |

---

## 5. Interfaces externas

### 5.1 Interfaz de usuario

| Pantalla | Rol | Requisitos |
|----------|-----|------------|
| Inicio | Todos | Accesos según rol |
| Entrar | Visitante | RF-01–RF-05 |
| Buscar (Materia → Asesor → Horario y fecha) | Todos | RF-10–RF-16 |
| Mis asesorías / Solicitudes (Próximas, Historial) | Alumno, Asesor | RF-20–RF-33 |
| Mis horarios (y resumen de horas) | Asesor | RF-40–RF-42, RF-53 |
| Dirección | Directivo | RF-50 |
| Reportes | Directivo | RF-51–RF-54 |

### 5.2 API REST (resumen; detalle en `/swagger`)

| Método y ruta | Requisito |
|---------------|-----------|
| `POST /api/auth/login`, `POST /api/auth/google`, `GET /api/auth/config` | RF-01, RF-02 |
| `GET /api/subjects`, `GET /api/advisors?subjectId=`, `GET /api/advisors/{id}/availabilities`, `GET /api/locations` | RF-10–RF-12 |
| `POST /api/advisors/{id}/messages` | RF-14 |
| `GET/POST /api/advisors/me/availabilities`, `PUT/DELETE /api/advisors/me/availabilities/{id}` | RF-40–RF-42 |
| `GET /api/sessions?scope=upcoming\|history`, `POST /api/sessions` | RF-13, RF-30–RF-33 |
| `PATCH /api/sessions/{id}/status` | RF-20, RF-21, RF-23 |
| `PATCH /api/sessions/{id}/schedule` | RF-22 |
| `POST /api/sessions/{id}/log` | RF-28 |
| `GET /api/sessions/{id}/calendar.ics` | RF-27 |
| `GET /api/admin/advisors`, `PUT /api/admin/advisors/{id}/subjects` | RF-50 |
| `GET /api/terms/current` | RF-16 |
| `GET /api/sessions/{id}/report.pdf` | RF-52 |
| `GET /api/advisors/me/report.pdf\|.csv?from&to`, `GET /api/admin/advisors/{id}/report.pdf\|.csv?from&to` | RF-53 |
| `GET /api/admin/reports?from&to&…`, `GET /api/admin/sessions.csv\|.pdf?from&to&…` | RF-51, RF-54 |

Códigos: 400 regla de negocio incumplida, 401 sin sesión, 403 sin permiso,
404 recurso inexistente. El cuerpo de error es siempre `{ "message": "…" }`.

### 5.3 Servicios externos (puertos y adaptadores)

| Puerto | Adaptador por omisión | Para activar el servicio real |
|--------|----------------------|-------------------------------|
| `IEmailSender` | `LogEmailSender`: escribe el correo en el log de la API. | Definir `Smtp:Host` (y `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:From`); se usa `SmtpEmailSender`. |
| `IMeetingLinkProvider` | `LocationMeetingLinkProvider`: usa el enlace HTTPS fijo de la sala virtual (`lugares.Detalles`). | Implementar un adaptador de Google Meet con la misma interfaz y registrarlo en `FCQI.Infrastructure/DependencyInjection.cs`. |
| Calendario | Archivo iCalendar estándar (RF-27): no requiere credenciales. | — |
| `IReportRenderer` | `QuestPdfReportRenderer` (QuestPDF, licencia Community para uso académico). | — |

---

## 6. Arquitectura y patrones de diseño

| Patrón | Dónde | Para qué |
|--------|-------|----------|
| Estado | `Domain/Sessions/SessionState.cs` | Cada estado conoce sus transiciones y quién puede hacerlas. |
| Comando | Handlers de `FCQI.Application`; `RelayCommand` en los ViewModels | Cada caso de uso o acción de pantalla es un objeto ejecutable. |
| Observador | `ISessionObserver` + `SessionEventPublisher`; `SessionContext` observable en el cliente | Notificar y generar enlaces sin acoplar el handler; la UI reacciona a la sesión. |
| Adaptador | `IEmailSender`, `IMeetingLinkProvider`, `IReportRenderer`, `IPlatformServices` | Servicios externos o de plataforma intercambiables. |
| Fachada | `FcqiApiClient` (`IFcqiApi`, `ICatalogApi`) | Un único punto de acceso a la API REST para el cliente. |
| Proxy | `AuthTokenHandler` | Añade el token a cada petición HTTP. |
| Decorador | `CachedCatalogApi` | Cachea materias y sedes sin tocar la fachada. |
| Método plantilla | `PageViewModel.RunAsync` | Esqueleto común ocupado → ejecutar → avisar/mostrar error. |
| Fábrica | `AppServices` (fábrica de páginas por `AppPage`) | El shell crea páginas sin conocer clases concretas. |
| Singleton | `SessionContext`, `AppStatus`, `CampusClock`, `JwtTokenFactory` (DI) | Una instancia compartida por la app. |
| Composición | `ShellViewModel` + menú de `NavItem` | El contenedor compone páginas y menú según rol. |
| Iterador | LINQ / `IQueryable` en consultas | Recorrido y filtrado declarativo. |

---

## 7. Modelo de datos

Tablas principales (ver `db/diagrama-er.md` para el diagrama completo):
`personas` y perfiles (`perfiles_alumno`, `perfiles_asesor`, `perfiles_directivo`),
catálogos (`programas`, `ciclos_escolares`, `materias`, `lugares`, `modalidades`,
`estados_sesion`), operación (`asesores_materias`, `horarios`, `asesorias`,
`bitacoras_asesoria`) y auditoría (`historial_estados_sesion`).

Cambios de esta versión (`db/migrations/002_procesos_bpmn.sql`, idempotente y
aplicada al arrancar el contenedor): estado 5 *Realizada*, columna
`asesorias.EnlaceReunion` y tabla `bitacoras_asesoria`.

---

## 8. Verificación y trazabilidad

| Requisito | Prueba automática (`tests/FCQI.UnitTests`) |
|-----------|---------------------------------------------|
| RF-01, RF-05 | `Client/ClientLogicTests` (validación de correo) |
| RF-02, RF-03 | `Auth/ResolveProfileHandlerTests` |
| RF-13 | `Sessions/PeerAdvisorTests`, `Client/ClientLogicTests` (próxima fecha) |
| RF-20, RF-23, RF-33 | `Auth/SessionAuthorizationTests`, `Sessions/StatusTransitionTests`, `Processes/SessionStateTests` |
| RF-21, RF-22, RF-26–RF-28, RF-30, RF-31 | `Processes/SessionProcessTests` |
| RF-40–RF-42 | `Processes/AvailabilityProcessTests` |
| RF-50 | `Admin/AdvisorSubjectsTests` |
| RF-15, RF-16 | `Client/FilterAndLogoutTests`, `Client/CatalogRaceTests` |
| RF-51–RF-54 | `Processes/ReportTests` (permisos, horas, filtro "desde", 25 columnas, PDF) y `tools/probar-api.py` I15–I26 |
| RF-04 (limpiar al salir) | `Client/FilterAndLogoutTests` |
| RNF-06 | `Persistence/ModelMappingTests` + checklist §1 |
| RNF-07 | `Time/CampusClockTests` |
| RF-14, RF-25 | `Processes/ContactAndNoticeTests` (límite de mensajes: `tools/probar-api.py` I27) |
| RF-11 (respuestas tardías) | `Client/CatalogRaceTests` |
| RNF-01, RNF-02, RNF-03 | Manual: `docs/checklist-validacion.md` y `tools/probar-api.py` |

Criterio de aceptación de una entrega: `dotnet test` en verde dentro del
contenedor, `tools/probar-api.py` sin fallas y el recorrido manual del
checklist en un teléfono (o vista de 375 × 812) y en escritorio.
