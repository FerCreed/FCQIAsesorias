-- =============================================================================
--  Migración 001 — del modelo v1 al modelo v2
--  MySQL 8.0.16+
--
--  REGISTRO HISTÓRICO. El camino normal para levantar una base de desarrollo
--  son los dos scripts de db/ (01-schema.sql + 02-seed.sql), no este archivo.
--  Esta migración existe para documentar qué cambió y para convertir alguna
--  instancia que todavía tenga datos con la forma anterior.
--
--  Estructura de partida: db/legacy/v1-schema.sql
--  Estructura de llegada: db/01-schema.sql
--
--  Los nombres en inglés que aparecen calificados con `fcqi_asesorias_v1`
--  son los del modelo viejo y se quedan como estaban: describen una base que
--  ya existe y que este script solo lee. Todo lo que se escribe usa los
--  nombres en español del modelo v2.
--
--  Estrategia: no altera la base v1 in situ. Construye la base nueva al lado
--  y copia los datos transformados, de modo que si algo sale mal la original
--  queda intacta y basta con borrar la nueva.
--
--      mysql -u root -p < db/01-schema.sql          -- crea fcqi_asesorias vacía
--      -- (renombrar antes la v1 a fcqi_asesorias_v1, ver abajo)
--      mysql -u root -p < db/migrations/001_v1_a_v2.sql
--
--  Requisitos previos:
--    · La base v1 debe llamarse `fcqi_asesorias_v1`.
--    · La base v2 (`fcqi_asesorias`) debe existir ya, vacía, creada con
--      db/01-schema.sql.
--
--  Cambios que aplica:
--    1. students + advisors + admins  ->  personas + perfiles_*
--       Deduplica por correo: una persona con dos roles deja de ser dos filas.
--    2. FullName  ->  Tratamiento + Nombres + ApellidoPaterno + ApellidoMaterno
--    3. advisors.Area (texto)  ->  programas (catálogo)
--    4. availabilities.Modality + Location  ->  lugares (catálogo)
--    5. subjects.Program ('FCQI 2026-2')  ->  ciclos_escolares
--    6. Status (texto)  ->  estados_sesion (catálogo)
--    7. ScheduledAt local  ->  ProgramadaEn en UTC
--    8. Alta de CicloId en asesores_materias, horarios y asesorias
-- =============================================================================

SET NAMES utf8mb4;
SET @SRC := 'fcqi_asesorias_v1';

USE `fcqi_asesorias`;

START TRANSACTION;

-- -----------------------------------------------------------------------------
--  1. Ciclo escolar. v1 no tenía el concepto: 'FCQI 2026-2' vivía repetido en
--     las 44 filas de subjects.Program.
--     REVISAR: fechas estimadas, ajustar al calendario oficial UABC.
-- -----------------------------------------------------------------------------
INSERT INTO `ciclos_escolares` (`Id`,`Codigo`,`Nombre`,`FechaInicio`,`FechaFin`,`EsActual`)
VALUES (1, '2026-2', 'Otoño 2026', '2026-08-10', '2026-12-11', 1);


-- -----------------------------------------------------------------------------
--  2. Programas, desde el texto libre de advisors.Area.
-- -----------------------------------------------------------------------------
INSERT INTO `programas` (`Codigo`, `Nombre`)
SELECT DISTINCT
       CASE a.`Area`
           WHEN 'Ingeniería en Electrónica'          THEN 'IE'
           WHEN 'Ingeniería Industrial'              THEN 'II'
           WHEN 'Ingeniería Química'                 THEN 'IQ'
           WHEN 'Químico Industrial'                 THEN 'QI'
           WHEN 'Química Farmacéutica Biológica'     THEN 'QFB'
           ELSE UPPER(LEFT(a.`Area`, 10))
       END,
       a.`Area`
FROM `fcqi_asesorias_v1`.`advisors` a
ORDER BY a.`Area`;


-- -----------------------------------------------------------------------------
--  3. Sedes, desde el par (Location, Modality) de availabilities.
--     En v1, 'Enlace virtual (Meet/Teams)' implicaba modalidad Virtual: la
--     dependencia transitiva que este catálogo elimina.
-- -----------------------------------------------------------------------------
INSERT INTO `lugares` (`Nombre`, `ModalidadId`)
SELECT DISTINCT v.`Location`, m.`Id`
FROM `fcqi_asesorias_v1`.`availabilities` v
JOIN `modalidades` m ON m.`Nombre` = v.`Modality`
ORDER BY v.`Location`;


-- -----------------------------------------------------------------------------
--  4. Personas. Deduplica por correo con UNION (no UNION ALL): si un mismo
--     correo aparecía como asesor y como alumno, aquí se vuelve UNA persona.
--
--     La separación del nombre usa la convención [nombres] [paterno] [materno]
--     contando desde el final. Las partículas ('de', 'la', 'del') rompen esa
--     cuenta, así que se corrigen en el paso 4b.
-- -----------------------------------------------------------------------------
CREATE TEMPORARY TABLE `tmp_nombres` (
    `Correo`         varchar(150) NOT NULL PRIMARY KEY,
    `NombreCompleto` varchar(200) NOT NULL,
    `Tratamiento`    varchar(20)  NULL,
    `Limpio`         varchar(200) NOT NULL
) ENGINE = InnoDB;

INSERT INTO `tmp_nombres` (`Correo`, `NombreCompleto`, `Tratamiento`, `Limpio`)
SELECT u.`Email`, u.`FullName`,
       CASE WHEN SUBSTRING_INDEX(u.`FullName`, ' ', 1)
                 IN ('Dra.','Dr.','Mtro.','Mtra.','Ing.','Lic.')
            THEN SUBSTRING_INDEX(u.`FullName`, ' ', 1) END,
       CASE WHEN SUBSTRING_INDEX(u.`FullName`, ' ', 1)
                 IN ('Dra.','Dr.','Mtro.','Mtra.','Ing.','Lic.')
            THEN TRIM(SUBSTRING(u.`FullName`,
                      LENGTH(SUBSTRING_INDEX(u.`FullName`, ' ', 1)) + 2))
            ELSE u.`FullName` END
FROM (
    SELECT `FullName`, LOWER(TRIM(`Email`)) AS `Email` FROM `fcqi_asesorias_v1`.`advisors`
    UNION
    SELECT `FullName`, LOWER(TRIM(`Email`))           FROM `fcqi_asesorias_v1`.`students`
    UNION
    SELECT `FullName`, LOWER(TRIM(`Email`))           FROM `fcqi_asesorias_v1`.`admins`
) u;

--  Separación del nombre, de derecha a izquierda:
--    · Si el nombre termina en partícula(s) + una palabra ('... de la Cruz'),
--      ese bloque completo es el apellido materno.
--    · Si no, el apellido materno es la última palabra.
--    · El paterno es la palabra inmediatamente anterior, y el resto son los
--      nombres de pila.
--  Así 'Carlos Enrique Martínez de la Cruz' da Martínez / de la Cruz, y
--  'Felipe de Jesús Márquez Vizcarra' da Márquez / Vizcarra sin que la
--  partícula de los nombres de pila estorbe.
INSERT INTO `personas` (`Tratamiento`, `Nombres`, `ApellidoPaterno`, `ApellidoMaterno`, `Correo`)
SELECT  x.`Tratamiento`,
        TRIM(LEFT(x.`Resto`, CHAR_LENGTH(x.`Resto`) - CHAR_LENGTH(x.`Paterno`))),
        x.`Paterno`,
        x.`Materno`,
        x.`Correo`
FROM (
    SELECT  y.`Tratamiento`, y.`Correo`, y.`NombreCompleto`, y.`Materno`, y.`Resto`,
            SUBSTRING_INDEX(y.`Resto`, ' ', -1) AS `Paterno`
    FROM (
        SELECT  t.`Tratamiento`, t.`Correo`, t.`NombreCompleto`, z.`Materno`,
                TRIM(LEFT(t.`Limpio`, CHAR_LENGTH(t.`Limpio`) - CHAR_LENGTH(z.`Materno`))) AS `Resto`
        FROM `tmp_nombres` t
        JOIN LATERAL (
            SELECT COALESCE(
                TRIM(REGEXP_SUBSTR(t.`Limpio`, '( (de|del|la|las|los|y))+ [^ ]+$')),
                SUBSTRING_INDEX(t.`Limpio`, ' ', -1)
            ) AS `Materno`
        ) z ON TRUE
    ) y
) x
ORDER BY x.`NombreCompleto`;


-- -----------------------------------------------------------------------------
--  5. Perfiles por rol.
-- -----------------------------------------------------------------------------
INSERT INTO `perfiles_asesor` (`PersonaId`, `ProgramaId`, `ModalidadPredeterminadaId`, `Activo`)
SELECT p.`Id`, pr.`Id`, m.`Id`, a.`IsActive`
FROM `fcqi_asesorias_v1`.`advisors` a
JOIN `personas`    p  ON p.`Correo` = LOWER(TRIM(a.`Email`))
JOIN `programas`   pr ON pr.`Nombre` = a.`Area`
JOIN `modalidades` m  ON m.`Nombre`  = a.`DefaultModality`;

--  ProgramaId queda NULL: v1 no registraba la carrera del alumno.
INSERT INTO `perfiles_alumno` (`PersonaId`, `Matricula`, `ProgramaId`)
SELECT p.`Id`, s.`StudentNumber`, NULL
FROM `fcqi_asesorias_v1`.`students` s
JOIN `personas` p ON p.`Correo` = LOWER(TRIM(s.`Email`));

INSERT INTO `perfiles_directivo` (`PersonaId`, `Cargo`)
SELECT p.`Id`, a.`Title`
FROM `fcqi_asesorias_v1`.`admins` a
JOIN `personas` p ON p.`Correo` = LOWER(TRIM(a.`Email`));


-- -----------------------------------------------------------------------------
--  6. Materias. Se descarta la columna Program: su contenido ('FCQI 2026-2')
--     era el ciclo, ya recogido en ciclos_escolares.
-- -----------------------------------------------------------------------------
INSERT INTO `materias` (`Codigo`, `Nombre`)
SELECT s.`Code`, s.`Name`
FROM `fcqi_asesorias_v1`.`subjects` s
ORDER BY s.`Id`;

--  PROVISIONAL: vínculo carrera-materia inferido de que un asesor adscrito a
--  una carrera imparta la materia. No es el plan de estudios oficial.
INSERT INTO `programas_materias` (`ProgramaId`, `MateriaId`)
SELECT DISTINCT ap.`ProgramaId`, ns.`Id`
FROM `fcqi_asesorias_v1`.`advisor_subjects` xs
JOIN `fcqi_asesorias_v1`.`advisors` a  ON a.`Id`  = xs.`AdvisorId`
JOIN `fcqi_asesorias_v1`.`subjects` os ON os.`Id` = xs.`SubjectId`
JOIN `personas`        p  ON p.`Correo`    = LOWER(TRIM(a.`Email`))
JOIN `perfiles_asesor` ap ON ap.`PersonaId` = p.`Id`
JOIN `materias`        ns ON ns.`Codigo`   = os.`Code`;


-- -----------------------------------------------------------------------------
--  7. Oferta, ahora acotada al ciclo.
-- -----------------------------------------------------------------------------
INSERT INTO `asesores_materias` (`CicloId`, `AsesorId`, `MateriaId`)
SELECT 1, p.`Id`, ns.`Id`
FROM `fcqi_asesorias_v1`.`advisor_subjects` xs
JOIN `fcqi_asesorias_v1`.`advisors` a  ON a.`Id`  = xs.`AdvisorId`
JOIN `fcqi_asesorias_v1`.`subjects` os ON os.`Id` = xs.`SubjectId`
JOIN `personas` p  ON p.`Correo` = LOWER(TRIM(a.`Email`))
JOIN `materias` ns ON ns.`Codigo` = os.`Code`;

--  Conserva el Id original para que las sesiones sigan apuntando al mismo bloque.
INSERT INTO `horarios`
    (`Id`, `CicloId`, `AsesorId`, `DiaSemana`, `HoraInicio`, `HoraFin`, `CupoMaximo`, `LugarId`)
SELECT v.`Id`, 1, p.`Id`, v.`DayOfWeek`, v.`StartTime`, v.`EndTime`, v.`MaxCapacity`, l.`Id`
FROM `fcqi_asesorias_v1`.`availabilities` v
JOIN `fcqi_asesorias_v1`.`advisors` a ON a.`Id` = v.`AdvisorId`
JOIN `personas` p ON p.`Correo` = LOWER(TRIM(a.`Email`))
JOIN `lugares`  l ON l.`Nombre` = v.`Location`;


-- -----------------------------------------------------------------------------
--  8. Sesiones. Dos conversiones: el estado pasa a catálogo y la hora a UTC.
--     Baja California sigue el horario de verano de EE.UU.: UTC-7 del 2.º
--     domingo de marzo al 1.er domingo de noviembre, UTC-8 el resto.
--     Se calcula el desplazamiento por fila, en vez de usar CONVERT_TZ, que
--     devuelve NULL si las tablas de zona horaria de MySQL no están cargadas.
--
--     Las sesiones sin AvailabilityId no se pueden migrar: en v2 el asesor se
--     valida contra el bloque, así que el bloque dejó de ser opcional. El
--     recuento de descartadas se imprime al final.
-- -----------------------------------------------------------------------------
INSERT INTO `asesorias`
    (`Id`, `CicloId`, `HorarioId`, `AsesorId`, `AlumnoId`, `MateriaId`,
     `ProgramadaEn`, `NumeroLugar`, `EstadoId`, `Tema`)
SELECT s.`Id`, 1, s.`AvailabilityId`, pa.`Id`, ps.`Id`, ns.`Id`,
       s.`ScheduledAt` + INTERVAL (
           CASE WHEN DATE(s.`ScheduledAt`) >=
                     (STR_TO_DATE(CONCAT(YEAR(s.`ScheduledAt`),'-03-01'),'%Y-%m-%d')
                      + INTERVAL ((8 - DAYOFWEEK(STR_TO_DATE(CONCAT(YEAR(s.`ScheduledAt`),'-03-01'),'%Y-%m-%d'))) % 7) DAY
                      + INTERVAL 7 DAY)
                 AND DATE(s.`ScheduledAt`) <
                     (STR_TO_DATE(CONCAT(YEAR(s.`ScheduledAt`),'-11-01'),'%Y-%m-%d')
                      + INTERVAL ((8 - DAYOFWEEK(STR_TO_DATE(CONCAT(YEAR(s.`ScheduledAt`),'-11-01'),'%Y-%m-%d'))) % 7) DAY)
                THEN 7 ELSE 8 END) HOUR,
       1, st.`Id`, s.`Topic`
FROM `fcqi_asesorias_v1`.`advisory_sessions` s
JOIN `fcqi_asesorias_v1`.`advisors` a  ON a.`Id`  = s.`AdvisorId`
JOIN `fcqi_asesorias_v1`.`students` u  ON u.`Id`  = s.`StudentId`
JOIN `fcqi_asesorias_v1`.`subjects` os ON os.`Id` = s.`SubjectId`
JOIN `personas`        pa ON pa.`Correo` = LOWER(TRIM(a.`Email`))
JOIN `personas`        ps ON ps.`Correo` = LOWER(TRIM(u.`Email`))
JOIN `materias`        ns ON ns.`Codigo` = os.`Code`
JOIN `estados_sesion`  st ON st.`Nombre` = s.`Status`
WHERE s.`AvailabilityId` IS NOT NULL;

COMMIT;


-- -----------------------------------------------------------------------------
--  Verificación. Todas las diferencias deben dar 0.
-- -----------------------------------------------------------------------------
SELECT 'personas (esperado: alta = suma de roles distintos)' AS control,
       (SELECT COUNT(*) FROM `personas`) AS v2,
       (SELECT COUNT(DISTINCT `Email`) FROM (
            SELECT `Email` FROM `fcqi_asesorias_v1`.`advisors`
            UNION SELECT `Email` FROM `fcqi_asesorias_v1`.`students`
            UNION SELECT `Email` FROM `fcqi_asesorias_v1`.`admins`) z) AS v1;

SELECT 'nombres mal reconstruidos' AS control, COUNT(*) AS diferencias
FROM `tmp_nombres` t JOIN `personas` p ON p.`Correo` = t.`Correo`
WHERE t.`Limpio` <> p.`NombreCompleto`;

SELECT 'asignaciones asesor-materia' AS control,
       (SELECT COUNT(*) FROM `asesores_materias`) AS v2,
       (SELECT COUNT(*) FROM `fcqi_asesorias_v1`.`advisor_subjects`) AS v1;

SELECT 'bloques de horario' AS control,
       (SELECT COUNT(*) FROM `horarios`) AS v2,
       (SELECT COUNT(*) FROM `fcqi_asesorias_v1`.`availabilities`) AS v1;

SELECT 'sesiones descartadas por no tener bloque' AS control, COUNT(*) AS filas
FROM `fcqi_asesorias_v1`.`advisory_sessions` WHERE `AvailabilityId` IS NULL;

DROP TEMPORARY TABLE IF EXISTS `tmp_nombres`;
