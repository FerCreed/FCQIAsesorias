# Checklist de validación

Qué hay que comprobar para dar por buena una entrega, en el orden en que
conviene hacerlo: primero lo automático, que tarda segundos y cubre casi todo;
después lo que solo se ve en pantalla.

Los tres primeros bloques se ejecutan solos. El cuarto es a mano, porque nadie
ha escrito todavía pruebas de interfaz que corran en el navegador.

---

## 1. La base de datos se reconstruye desde los scripts

La base **no la crea la aplicación**: la definen `db/01-schema.sql` (estructura)
y `db/02-seed.sql` (datos de prueba). Que la aplicación funcione contra una base
vieja no demuestra que los scripts estén bien; hay que rehacerla.

```bash
docker exec -i fcqi-dev mysql -uroot -pdev_root < db/01-schema.sql
docker exec -i fcqi-dev mysql -uroot -pdev_root < db/02-seed.sql
```

- [ ] Los dos scripts terminan sin un solo error.
- [ ] Los conteos cuadran:

```bash
docker exec fcqi-dev mysql -uroot -pdev_root fcqi_asesorias -e "
SELECT 'personas',COUNT(*) FROM personas
UNION ALL SELECT 'asesores',COUNT(*) FROM perfiles_asesor
UNION ALL SELECT 'perfiles de alumno',COUNT(*) FROM perfiles_alumno
UNION ALL SELECT 'materias',COUNT(*) FROM materias
UNION ALL SELECT 'bloques',COUNT(*) FROM horarios
UNION ALL SELECT 'sesiones',COUNT(*) FROM asesorias;"
```

  | Tabla | Esperado |
  |-------|----------|
  | `personas` | 21 |
  | `perfiles_asesor` | 14 |
  | `perfiles_alumno` | 8 |
  | `materias` | 44 |
  | `horarios` | 104 |
  | `asesorias` | 6 |

- [ ] **Hay asesores pares**: dos personas con más de un rol. Si esta consulta
      sale vacía, el selector de rol no tiene nada que seleccionar y medio
      sistema queda sin probar.

```bash
docker exec fcqi-dev mysql -uroot -pdev_root fcqi_asesorias -e "
SELECT PersonaId, Correo, GROUP_CONCAT(Rol) AS roles
FROM v_roles_persona GROUP BY PersonaId, Correo HAVING COUNT(*) > 1;"
```

  Esperado: `v1299027@uabc.edu.mx` y `j2207105@uabc.edu.mx`, cada uno con
  `Asesor,Alumno`.

- [ ] El historial de estados se escribió solo, por los triggers:
      `SELECT COUNT(*) FROM historial_estados_sesion;` devuelve 6 (una fila de
      creación por sesión), sin que el seed lo inserte.
- [ ] El diagrama sigue describiendo la base real:
      `python3 tools/generar-er.py` y `git diff db/diagrama-er.md` solo cambia
      la fecha o nada. (El script escribe el archivo él mismo: **no** se
      redirige su salida.)

## 2. Pruebas unitarias

```bash
dotnet test tests/FCQI.UnitTests
```

- [ ] 56 pruebas, 0 fallos.
- [ ] Cubren, entre otras cosas: el asesor par con sus dos roles, los estados
      finales de una asesoría, el mapeo de entidades a tablas y el horario de
      verano de `America/Tijuana`.

## 3. Pruebas de la API en ejecución

Con la aplicación levantada (contenedor en `:5080`, o la API nativa en `:5016`)
y la base **recién sembrada**:

```bash
python3 tools/probar-api.py                             # contenedor
API=http://localhost:5016 python3 tools/probar-api.py   # API nativa
```

- [ ] 58 comprobaciones, 0 fallas. El script cancela al terminar todo lo que
      creó, así que puede repetirse.

Lo que cubre, por si hay que revisarlo a mano:

| Bloque | Qué comprueba |
|--------|----------------|
| A | Todo endpoint fuera de `/api/auth` exige token; correos no institucionales y no registrados se rechazan; el token del asesor par trae sus dos roles |
| B | Catálogo de materias, tutores por materia y bloques con sede y modalidad |
| C | Alta de una solicitud y sus doce formas de estar mal: duplicada, día o hora equivocados, bloque de otro tutor, materia no asignada, fuera del ciclo, en el pasado, suplantando a otro alumno |
| D | El asesor par: sus dos bandejas por separado, sin mezclarse, sin poder agendarse consigo mismo y sin ver lo ajeno |
| E | El cupo del bloque se respeta: el segundo alumno recibe «lleno», no una sobreventa |
| F | Cada quien ve lo suyo; dirección lo ve todo; pedir lo de otro da 403, no una lista vacía |
| G | Quién puede confirmar, rechazar y cancelar; y que una asesoría cancelada **no** vuelve a la vida |
| H | Panel de dirección: solo el Directivo entra, las materias se asignan y retiran, y no se puede quitar una materia con asesorías vivas |

## 4. Interfaz (a mano, en el navegador)

Abre `http://localhost:5080`. El acceso de demostración solo existe mientras
`Authentication:Google:ClientId` esté vacío.

### Alumno

- [ ] Entra como **Alumno: Yesua Fernando Díaz Hernández**.
- [ ] El encabezado ofrece Inicio · Buscar · Mis asesorías · Salir, y **no**
      muestra Dirección.
- [ ] Buscar → *Cálculo Diferencial* → sale la lista de tutores que dirección
      le asignó → elige tutor → salen sus bloques de horario.
- [ ] «Solicitar asesoría» → **la pantalla salta sola a Mis asesorías** y la
      nueva aparece como Pendiente.
- [ ] Pedir dos veces el mismo bloque deja un mensaje que lo explica, no un
      volcado de excepción.
- [ ] Con una asesoría seleccionada, «Cancelar mi asesoría» la pasa a Cancelada.
- [ ] Al alumno no le aparecen los botones Confirmar ni Rechazar.

### Asesor par: el cambio de rol

Este es el caso que motivó el selector. **Entra como `j2207105@uabc.edu.mx`
(Jimena Beltrán) o `v1299027@uabc.edu.mx` (Vladimir Ramírez).**

- [ ] En el selector de acceso, esa persona aparece **dos veces**, una por rol.
      Elegir «Alumno: …» entra como alumna, no como asesora.
- [ ] En el encabezado aparece **«Entrar como»** con una lista de sus dos roles.
      A quien solo tiene un rol no le aparece.
- [ ] El recuadro del hero dice **ESTÁS COMO: Alumno**.
- [ ] Como alumna ve *sus* asesorías (las que pidió) y puede usar Buscar.
- [ ] Cambia el selector a **Asesor**: vuelve al inicio, desaparece Buscar,
      aparece Solicitudes y la lista cambia a las asesorías **que ella atiende**.
      El recuadro dice ESTÁS COMO: Asesor.
- [ ] **No hubo que salir ni volver a entrar.**
- [ ] Como asesora, Confirmar/Rechazar funcionan sobre la solicitud que le
      hicieron.
- [ ] Vuelve a **Alumno**: reaparece Buscar y vuelve su propia lista.
- [ ] Agendarse consigo misma se rechaza con un mensaje claro.

### Dirección

- [ ] Entra como **Directivo: Lizeth Carolina Aguilar Dodier**.
- [ ] El encabezado ofrece Citas y Dirección; no ofrece Buscar.
- [ ] Citas muestra **todas** las asesorías del programa.
- [ ] Dirección → elige un tutor → marca una materia → Guardar: el mensaje
      confirma y **el panel se queda en el mismo tutor**, no salta al primero.
- [ ] Esa materia aparece luego al alumno cuando la busca.
- [ ] Desmarcar una materia con asesorías vivas se rechaza **con el motivo en
      pantalla**, no en silencio.

### Salir

- [ ] «Salir» vuelve al acceso y no queda nada del usuario anterior en las
      listas.

## 5. Operabilidad

- [ ] `docker compose -f docker-compose.dev.yml up --build` levanta la
      aplicación completa en `:5080` sin nada instalado en la máquina.
- [ ] La API se niega a arrancar, con un mensaje claro, si la base no está
      sembrada (no hay ciclo escolar vigente).
- [ ] Swagger responde en `/swagger` en Desarrollo.
- [ ] MySQL es accesible desde Workbench en `127.0.0.1:3307`.
- [ ] `docker logs fcqi-dev` no muestra excepciones sin tratar (`500`) durante
      un recorrido normal.

---

## Pendientes conocidos

No son fallos de lo entregado, pero conviene tenerlos escritos:

- **Entrada con Google desde la interfaz.** La API valida el `id_token` de
  Google y rechaza lo que no sea `@uabc.edu.mx` (`POST /api/auth/google`), pero
  la pantalla todavía no tiene el botón: falta el puente de JavaScript de
  Google Identity Services hacia el WASM. Al configurar el ClientId, el acceso
  de demostración se retira y la pantalla lo dice en lugar de fallar.
- **La carrera del alumno** (`perfiles_alumno.ProgramaId`) va en NULL para los
  seis alumnos que venían del modelo anterior, que no la registraba. Los dos
  asesores pares sí la traen, porque su perfil de asesor la declara.
- **No hay pruebas automáticas de la interfaz en el navegador**; el bloque 4 es
  a mano.
