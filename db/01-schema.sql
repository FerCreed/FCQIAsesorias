-- =============================================================================
--  Sistema de Asesorías FCQI — ESTRUCTURA DE LA BASE DE DATOS
--  MySQL 8.0.16+ · InnoDB · utf8mb4
--
--  Script 1 de 2. Contiene ÚNICAMENTE la estructura: tablas, llaves, índices,
--  restricciones, triggers y vistas. Los datos van en 02-seed.sql.
--
--      mysql -u root -p < db/01-schema.sql     (estructura)
--      mysql -u root -p < db/02-seed.sql       (datos de prueba)
--
--  Los contenedores de desarrollo se levantan y se destruyen en distintas
--  máquinas: estos dos scripts son la definición reproducible de la base,
--  no el volumen de Docker. Ejecutar 01 borra y recrea la base entera.
--
--  El modelo sigue un esquema entidad-relación normalizado hasta 3FN.
--  Diagrama: db/diagrama-er.md (lo genera tools/generar-er.py leyendo
--  la base en ejecución, así que nunca contradice a este archivo).
--
--  Decisiones estructurales, frente al modelo anterior (db/legacy/):
--
--    1. Una persona, varios roles. Antes había tres tablas de identidad
--       (students, advisors, admins) y el rol dependía del orden de consulta,
--       lo que impedía que un alumno fuera también asesor.
--    2. El asesor de una sesión no puede contradecir al dueño del horario.
--       Antes solo lo vigilaba código C#; ahora lo impone una llave foránea.
--    3. MaxCapacity deja de ser dato muerto: el cupo se respeta.
--    4. Estados, modalidades y áreas son catálogos con integridad
--       referencial, no texto libre.
--    5. Todo cuelga de un ciclo escolar, y los cambios quedan auditados.
--
--  Excepción deliberada a "solo estructura": las filas de los catálogos fijos
--  (modalities, session_statuses) se insertan aquí. No son datos del dominio
--  sino parte del contrato del esquema: los triggers y las llaves foráneas
--  dependen de que existan.
-- =============================================================================

SET NAMES utf8mb4;

CREATE DATABASE IF NOT EXISTS `fcqi_asesorias`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE `fcqi_asesorias`;

-- Se descartan las tablas en vez de la base entera: así los permisos del
-- usuario de aplicación sobreviven a una reinstalación del esquema.
-- El orden no importa con las comprobaciones desactivadas.
SET FOREIGN_KEY_CHECKS = 0;
DROP VIEW  IF EXISTS `v_advisors`, `v_students`, `v_person_roles`, `v_availability_load`;
DROP TABLE IF EXISTS `session_status_history`, `advisory_sessions`, `advisor_subjects`,
                     `availabilities`, `program_subjects`, `subjects`, `admin_profiles`,
                     `student_profiles`, `advisor_profiles`, `people`, `locations`,
                     `academic_terms`, `programs`, `session_statuses`, `modalities`,
                     `__EFMigrationsHistory`;
SET FOREIGN_KEY_CHECKS = 1;


-- =============================================================================
--  1. CATÁLOGOS
--
--  Sustituyen a las columnas varchar libres de v1. En v1 la base aceptaba
--  'Pendinte' sin protestar; aquí una llave foránea lo rechaza.
--  Se prefieren tablas a ENUM: agregar un valor es un INSERT, no un ALTER
--  TABLE que bloquea la tabla completa.
-- =============================================================================

CREATE TABLE `modalities` (
    `Id`   tinyint unsigned NOT NULL,
    `Code` varchar(20)  NOT NULL,
    `Name` varchar(50)  NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_modalities_Code` (`Code`)
) ENGINE = InnoDB;

INSERT INTO `modalities` (`Id`, `Code`, `Name`) VALUES
    (1, 'IN_PERSON', 'Presencial'),
    (2, 'VIRTUAL',   'Virtual');


--  IsActive distingue los estados que ocupan cupo (pendiente, confirmada) de
--  los que lo liberan (cancelada, rechazada). Los índices de cupo dependen
--  de esta distinción.
CREATE TABLE `session_statuses` (
    `Id`       tinyint unsigned NOT NULL,
    `Code`     varchar(20) NOT NULL,
    `Name`     varchar(50) NOT NULL,
    `IsActive` tinyint(1)  NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_session_statuses_Code` (`Code`)
) ENGINE = InnoDB;

INSERT INTO `session_statuses` (`Id`, `Code`, `Name`, `IsActive`) VALUES
    (1, 'PENDING',   'Pendiente',  1),
    (2, 'CONFIRMED', 'Confirmada', 1),
    (3, 'CANCELLED', 'Cancelada',  0),
    (4, 'REJECTED',  'Rechazada',  0);


--  Programas educativos. En v1 esto era advisors.Area: texto libre repetido
--  ('Ingeniería Química' aparecía 4 veces). Un typo creaba un área fantasma.
CREATE TABLE `programs` (
    `Id`        smallint unsigned NOT NULL AUTO_INCREMENT,
    `Code`      varchar(30)  NOT NULL,
    `Name`      varchar(120) NOT NULL,
    `IsActive`  tinyint(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_programs_Code` (`Code`)
) ENGINE = InnoDB;


--  Ciclo escolar. v1 no tenía este concepto: subjects.Program valía
--  'FCQI 2026-2' en las 44 filas, mezclando carrera y periodo. Al llegar
--  2027-1 había que duplicar el catálogo o sobrescribir el historial.
CREATE TABLE `academic_terms` (
    `Id`        smallint unsigned NOT NULL AUTO_INCREMENT,
    `Code`      varchar(10) NOT NULL COMMENT 'p. ej. 2026-2',
    `Name`      varchar(60) NOT NULL,
    `StartsOn`  date        NOT NULL,
    `EndsOn`    date        NOT NULL,
    `IsCurrent` tinyint(1)  NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_academic_terms_Code` (`Code`),
    CONSTRAINT `CK_academic_terms_Range` CHECK (`EndsOn` > `StartsOn`)
) ENGINE = InnoDB;


--  Lugares. En v1, availabilities.Location guardaba
--  'Enlace virtual (Meet/Teams)', que no es un lugar sino una modalidad
--  disfrazada: Location determinaba Modality (dependencia transitiva).
--  Aquí el lugar declara su modalidad una sola vez.
CREATE TABLE `locations` (
    `Id`         smallint unsigned NOT NULL AUTO_INCREMENT,
    `Name`       varchar(200) NOT NULL,
    `ModalityId` tinyint unsigned NOT NULL,
    `Details`    varchar(500) NULL,
    `IsActive`   tinyint(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_locations_Name` (`Name`),
    KEY `IX_locations_ModalityId` (`ModalityId`),
    CONSTRAINT `FK_locations_modalities`
        FOREIGN KEY (`ModalityId`) REFERENCES `modalities` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;


-- =============================================================================
--  2. IDENTIDAD
--
--  El cambio de fondo frente a v1. Antes había tres tablas de identidad
--  (students, advisors, admins) y el rol se decidía por el orden en que
--  ResolveProfileHandler las consultaba. Eso rompía a los asesores pares:
--  v1299027@uabc.edu.mx y j2207105@uabc.edu.mx son alumnos que asesoran, y
--  con el modelo v1 nunca habrían podido agendar una asesoría como alumnos.
--
--  Aquí la persona es una sola fila, y cada rol es un perfil opcional que
--  cuelga de ella. Una persona puede tener los tres a la vez.
-- =============================================================================

CREATE TABLE `people` (
    `Id`               int unsigned NOT NULL AUTO_INCREMENT,
    --  Nombre separado: en v1 era un solo FullName, así que ORDER BY ordenaba
    --  por nombre de pila. Los listados oficiales van por apellido paterno.
    `Honorific`        varchar(20)  NULL COMMENT 'Dra., Dr., Mtro. — opcional',
    `FirstName`        varchar(100) NOT NULL,
    `LastNamePaternal` varchar(100) NOT NULL,
    `LastNameMaternal` varchar(100) NULL,
    --  Columna generada: evita repetir la concatenación en cada consulta y
    --  permite buscar por nombre completo con un índice.
    --  NOT NULL explícito: CONCAT_WS sobre dos columnas obligatorias nunca
    --  devuelve NULL, pero MySQL no lo deduce y la declararía nullable, lo que
    --  dejaría el modelo de EF Core y la base discrepando sin necesidad.
    `DisplayName`      varchar(302) AS (
        CONCAT_WS(' ', `FirstName`, `LastNamePaternal`, `LastNameMaternal`)
    ) STORED NOT NULL,
    `Email`            varchar(150) NOT NULL,
    `IsActive`         tinyint(1)   NOT NULL DEFAULT 1,
    `CreatedAt`        datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt`        datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
                                    ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_people_Email` (`Email`),
    KEY `IX_people_DisplayName` (`DisplayName`),
    --  La regla institucional deja de depender de una validación en C#.
    CONSTRAINT `CK_people_InstitutionalEmail`
        CHECK (`Email` LIKE '%@uabc.edu.mx' AND `Email` = LOWER(`Email`))
) ENGINE = InnoDB;


--  Perfil de alumno. PersonId es a la vez PK y FK: relación 1:0..1 con people.
CREATE TABLE `student_profiles` (
    `PersonId`      int unsigned NOT NULL,
    `StudentNumber` varchar(20)  NOT NULL,
    --  Nullable a propósito: el modelo v1 no registraba la carrera del alumno,
    --  y ponerla obligatoria forzaría a inventar el dato. Se llena cuando el
    --  alumno la declare o se importe el padrón oficial.
    `ProgramId`     smallint unsigned NULL,
    `IsActive`      tinyint(1)   NOT NULL DEFAULT 1,
    `CreatedAt`     datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`PersonId`),
    UNIQUE KEY `UQ_student_profiles_StudentNumber` (`StudentNumber`),
    KEY `IX_student_profiles_ProgramId` (`ProgramId`),
    CONSTRAINT `FK_student_profiles_people`
        FOREIGN KEY (`PersonId`) REFERENCES `people` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_student_profiles_programs`
        FOREIGN KEY (`ProgramId`) REFERENCES `programs` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;


CREATE TABLE `advisor_profiles` (
    `PersonId`          int unsigned NOT NULL,
    `ProgramId`         smallint unsigned NOT NULL COMMENT 'era advisors.Area',
    `DefaultModalityId` tinyint unsigned NOT NULL,
    `IsActive`          tinyint(1)   NOT NULL DEFAULT 1,
    `CreatedAt`         datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`PersonId`),
    KEY `IX_advisor_profiles_ProgramId` (`ProgramId`),
    KEY `IX_advisor_profiles_DefaultModalityId` (`DefaultModalityId`),
    CONSTRAINT `FK_advisor_profiles_people`
        FOREIGN KEY (`PersonId`) REFERENCES `people` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_advisor_profiles_programs`
        FOREIGN KEY (`ProgramId`) REFERENCES `programs` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_advisor_profiles_modalities`
        FOREIGN KEY (`DefaultModalityId`) REFERENCES `modalities` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;


CREATE TABLE `admin_profiles` (
    `PersonId`  int unsigned NOT NULL,
    `Title`     varchar(200) NOT NULL,
    `IsActive`  tinyint(1)   NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`PersonId`),
    CONSTRAINT `FK_admin_profiles_people`
        FOREIGN KEY (`PersonId`) REFERENCES `people` (`Id`) ON DELETE CASCADE
) ENGINE = InnoDB;


-- =============================================================================
--  3. CATÁLOGO ACADÉMICO
-- =============================================================================

--  La materia ya no lleva columna Program: 'Cálculo Diferencial' e 'Inglés I'
--  se cursan en varias carreras. Esa relación es N:M y vive en program_subjects.
CREATE TABLE `subjects` (
    `Id`       int unsigned NOT NULL AUTO_INCREMENT,
    `Code`     varchar(120) NOT NULL,
    `Name`     varchar(200) NOT NULL,
    `IsActive` tinyint(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_subjects_Code` (`Code`),
    KEY `IX_subjects_Name` (`Name`)
) ENGINE = InnoDB;


CREATE TABLE `program_subjects` (
    `ProgramId` smallint unsigned NOT NULL,
    `SubjectId` int unsigned NOT NULL,
    `Semester`  tinyint unsigned NULL COMMENT 'semestre sugerido del plan',
    PRIMARY KEY (`ProgramId`, `SubjectId`),
    KEY `IX_program_subjects_SubjectId` (`SubjectId`),
    CONSTRAINT `FK_program_subjects_programs`
        FOREIGN KEY (`ProgramId`) REFERENCES `programs` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_program_subjects_subjects`
        FOREIGN KEY (`SubjectId`) REFERENCES `subjects` (`Id`) ON DELETE CASCADE
) ENGINE = InnoDB;


-- =============================================================================
--  4. OFERTA DE ASESORÍAS (por ciclo)
-- =============================================================================

--  Qué materias asesora cada asesor, EN QUÉ CICLO. En v1 la asignación no
--  tenía ciclo, así que la del semestre pasado se perdía al reasignar.
CREATE TABLE `advisor_subjects` (
    `TermId`    smallint unsigned NOT NULL,
    `AdvisorId` int unsigned NOT NULL,
    `SubjectId` int unsigned NOT NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`TermId`, `AdvisorId`, `SubjectId`),
    KEY `IX_advisor_subjects_SubjectId` (`SubjectId`),
    KEY `IX_advisor_subjects_AdvisorId` (`AdvisorId`),
    CONSTRAINT `FK_advisor_subjects_terms`
        FOREIGN KEY (`TermId`) REFERENCES `academic_terms` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_advisor_subjects_advisors`
        FOREIGN KEY (`AdvisorId`) REFERENCES `advisor_profiles` (`PersonId`) ON DELETE CASCADE,
    CONSTRAINT `FK_advisor_subjects_subjects`
        FOREIGN KEY (`SubjectId`) REFERENCES `subjects` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;


--  Horario semanal recurrente. Modality desaparece: la determina LocationId.
--  DayOfWeek conserva la convención de System.DayOfWeek (0=domingo .. 6=sábado)
--  para no romper el código existente.
CREATE TABLE `availabilities` (
    `Id`          int unsigned NOT NULL AUTO_INCREMENT,
    `TermId`      smallint unsigned NOT NULL,
    `AdvisorId`   int unsigned NOT NULL,
    `DayOfWeek`   tinyint unsigned NOT NULL,
    `StartTime`   time(0)      NOT NULL,
    `EndTime`     time(0)      NOT NULL,
    `MaxCapacity` smallint unsigned NOT NULL DEFAULT 1,
    `LocationId`  smallint unsigned NOT NULL,
    `IsActive`    tinyint(1)   NOT NULL DEFAULT 1,
    `CreatedAt`   datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    --  Necesaria para la llave foránea compuesta desde advisory_sessions.
    --  Es lo que hace imposible que una sesión mienta sobre su asesor.
    UNIQUE KEY `UQ_availabilities_Id_AdvisorId` (`Id`, `AdvisorId`),
    --  Un asesor no puede declarar dos veces el mismo bloque.
    UNIQUE KEY `UQ_availabilities_Slot` (`TermId`, `AdvisorId`, `DayOfWeek`, `StartTime`),
    KEY `IX_availabilities_AdvisorId` (`AdvisorId`),
    KEY `IX_availabilities_LocationId` (`LocationId`),
    KEY `IX_availabilities_TermId_DayOfWeek` (`TermId`, `DayOfWeek`),
    CONSTRAINT `FK_availabilities_terms`
        FOREIGN KEY (`TermId`) REFERENCES `academic_terms` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_availabilities_advisors`
        FOREIGN KEY (`AdvisorId`) REFERENCES `advisor_profiles` (`PersonId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_availabilities_locations`
        FOREIGN KEY (`LocationId`) REFERENCES `locations` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `CK_availabilities_DayOfWeek` CHECK (`DayOfWeek` BETWEEN 0 AND 6),
    CONSTRAINT `CK_availabilities_TimeRange` CHECK (`EndTime` > `StartTime`),
    CONSTRAINT `CK_availabilities_Capacity`  CHECK (`MaxCapacity` >= 1)
) ENGINE = InnoDB;


-- =============================================================================
--  5. SESIONES
--
--  El corazón del rediseño. Tres garantías que v1 dejaba en manos del código:
--
--  (a) El asesor de la sesión ES el dueño del horario.
--      FK compuesta (AvailabilityId, AdvisorId) -> availabilities(Id, AdvisorId).
--      AdvisorId sigue presente como columna por razones prácticas: sin ella,
--      toda consulta de "mis asesorías" exigiría un JOIN extra, y no se podría
--      declarar la garantía (b). La dependencia transitiva que esto implica
--      queda neutralizada: la FK hace imposible el valor inconsistente, así
--      que no existe anomalía de actualización.
--
--  (b) El asesor imparte esa materia en ese ciclo.
--      FK compuesta (TermId, AdvisorId, SubjectId) -> advisor_subjects.
--
--  (c) El cupo del bloque se respeta y nadie se duplica.
--      Ver los índices únicos sobre la columna generada ActiveAt.
-- =============================================================================

CREATE TABLE `advisory_sessions` (
    `Id`             bigint unsigned NOT NULL AUTO_INCREMENT,
    `TermId`         smallint unsigned NOT NULL,
    `AvailabilityId` int unsigned NOT NULL,
    `AdvisorId`      int unsigned NOT NULL,
    `StudentId`      int unsigned NOT NULL,
    `SubjectId`      int unsigned NOT NULL,
    --  UTC. En v1 era hora local sin zona, y Ensenada observa horario de
    --  verano: las citas que cruzaban el cambio se corrían una hora.
    `ScheduledAt`    datetime(6)  NOT NULL COMMENT 'UTC; la hora local es America/Tijuana',
    `SeatNumber`     smallint unsigned NOT NULL DEFAULT 1,
    `StatusId`       tinyint unsigned NOT NULL DEFAULT 1,
    `Topic`          varchar(500) NOT NULL,
    `CreatedAt`      datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt`      datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
                                  ON UPDATE CURRENT_TIMESTAMP(6),

    --  Vale la fecha SOLO mientras la sesión ocupa cupo (pendiente o
    --  confirmada) y NULL cuando se cancela o rechaza. MySQL no tiene
    --  índices parciales, pero en un índice único los NULL no colisionan
    --  entre sí: ese es el mecanismo que libera el lugar al cancelar, sin
    --  borrar la fila ni perder el historial.
    --
    --  La mantienen los triggers de la sección 7, no es columna generada.
    --  Una generada daría el mismo resultado sobre el papel, pero MySQL la
    --  evalúa mal ('0000-00-00') cuando un trigger BEFORE INSERT de la misma
    --  tabla lee NEW, y eso hace fallar todo INSERT válido. Mantenerla desde
    --  el trigger además permite consultar session_statuses.IsActive en vez
    --  de incrustar los IDs 1 y 2, que una columna generada no podría leer.
    --  Cualquier valor que venga en el INSERT lo sobrescribe el trigger.
    `ActiveAt` datetime(6) NULL,

    PRIMARY KEY (`Id`),

    --  (c) Un asiento de un bloque, a una hora dada, lo ocupa una sola sesión
    --  activa. Con MaxCapacity = 1 equivale a "no hay sobreventa".
    UNIQUE KEY `UQ_sessions_Seat` (`AvailabilityId`, `ActiveAt`, `SeatNumber`),
    --  El mismo alumno no puede tener dos solicitudes activas en el mismo
    --  bloque y hora. En v1 esto era una consulta en C#.
    UNIQUE KEY `UQ_sessions_StudentSlot` (`StudentId`, `AvailabilityId`, `ActiveAt`),

    KEY `IX_sessions_AdvisorId_ScheduledAt` (`AdvisorId`, `ScheduledAt`),
    KEY `IX_sessions_StudentId_ScheduledAt` (`StudentId`, `ScheduledAt`),
    KEY `IX_sessions_StatusId` (`StatusId`),
    KEY `IX_sessions_SubjectId` (`SubjectId`),
    KEY `IX_sessions_TermId` (`TermId`),
    KEY `FK_sessions_teaches` (`TermId`, `AdvisorId`, `SubjectId`),

    --  (a) el horario pertenece a ese asesor
    CONSTRAINT `FK_sessions_availability_advisor`
        FOREIGN KEY (`AvailabilityId`, `AdvisorId`)
        REFERENCES `availabilities` (`Id`, `AdvisorId`) ON DELETE RESTRICT,
    --  (b) ese asesor imparte esa materia en ese ciclo
    CONSTRAINT `FK_sessions_teaches`
        FOREIGN KEY (`TermId`, `AdvisorId`, `SubjectId`)
        REFERENCES `advisor_subjects` (`TermId`, `AdvisorId`, `SubjectId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_sessions_students`
        FOREIGN KEY (`StudentId`) REFERENCES `student_profiles` (`PersonId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_sessions_statuses`
        FOREIGN KEY (`StatusId`) REFERENCES `session_statuses` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `CK_sessions_Seat` CHECK (`SeatNumber` >= 1)
) ENGINE = InnoDB;


-- =============================================================================
--  6. AUDITORÍA
--
--  v1 no registraba nada. Cuando un alumno reclamara que su asesoría fue
--  cancelada, no había forma de saber quién ni cuándo. En un sistema
--  institucional eso no es opcional.
-- =============================================================================

CREATE TABLE `session_status_history` (
    `Id`           bigint unsigned NOT NULL AUTO_INCREMENT,
    `SessionId`    bigint unsigned NOT NULL,
    `FromStatusId` tinyint unsigned NULL COMMENT 'NULL = creación',
    `ToStatusId`   tinyint unsigned NOT NULL,
    `ChangedBy`    int unsigned NULL COMMENT 'NULL = proceso automático',
    `ChangedAt`    datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `Reason`       varchar(500) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_history_SessionId_ChangedAt` (`SessionId`, `ChangedAt`),
    KEY `IX_history_ChangedBy` (`ChangedBy`),
    KEY `IX_history_ToStatusId` (`ToStatusId`),
    KEY `IX_history_FromStatusId` (`FromStatusId`),
    CONSTRAINT `FK_history_sessions`
        FOREIGN KEY (`SessionId`) REFERENCES `advisory_sessions` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_history_from_status`
        FOREIGN KEY (`FromStatusId`) REFERENCES `session_statuses` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_history_to_status`
        FOREIGN KEY (`ToStatusId`) REFERENCES `session_statuses` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_history_people`
        FOREIGN KEY (`ChangedBy`) REFERENCES `people` (`Id`) ON DELETE SET NULL
) ENGINE = InnoDB;


-- =============================================================================
--  7. VALIDACIÓN DE CUPO
--
--  Los índices únicos de advisory_sessions impiden que dos sesiones activas
--  compartan asiento, pero no pueden comparar SeatNumber contra el MaxCapacity
--  del bloque: un CHECK de MySQL no puede consultar otra tabla. Sin esto,
--  SeatNumber = 99 entra en un bloque de cupo 2 y la sobreventa vuelve por
--  la puerta de atrás. De ahí los triggers.
--
--  Nota sobre lo que NO se valida aquí: que ScheduledAt caiga realmente en el
--  día y la hora del bloque. Comprobarlo exige CONVERT_TZ entre UTC y
--  America/Tijuana, que depende de las tablas de zona horaria de MySQL
--  (mysql_tzinfo_to_sql), ausentes en muchas instalaciones. Esa validación
--  se queda en la aplicación, donde la zona horaria es explícita.
-- =============================================================================

DELIMITER $$

CREATE TRIGGER `TRG_sessions_before_insert`
BEFORE INSERT ON `advisory_sessions`
FOR EACH ROW
BEGIN
    DECLARE v_capacity  SMALLINT UNSIGNED;
    DECLARE v_slot_open TINYINT(1);
    DECLARE v_occupies  TINYINT(1);

    SELECT `MaxCapacity`, `IsActive` INTO v_capacity, v_slot_open
    FROM `availabilities` WHERE `Id` = NEW.`AvailabilityId`;

    IF NEW.`SeatNumber` > v_capacity THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El asiento excede el cupo del bloque de horario.';
    END IF;

    IF v_slot_open = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El bloque de horario está dado de baja.';
    END IF;

    SELECT `IsActive` INTO v_occupies
    FROM `session_statuses` WHERE `Id` = NEW.`StatusId`;

    SET NEW.`ActiveAt` = IF(v_occupies = 1, NEW.`ScheduledAt`, NULL);
END$$

CREATE TRIGGER `TRG_sessions_before_update`
BEFORE UPDATE ON `advisory_sessions`
FOR EACH ROW
BEGIN
    DECLARE v_capacity SMALLINT UNSIGNED;
    DECLARE v_occupies TINYINT(1);

    IF NEW.`SeatNumber` <> OLD.`SeatNumber`
       OR NEW.`AvailabilityId` <> OLD.`AvailabilityId` THEN
        SELECT `MaxCapacity` INTO v_capacity
        FROM `availabilities` WHERE `Id` = NEW.`AvailabilityId`;

        IF NEW.`SeatNumber` > v_capacity THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'El asiento excede el cupo del bloque de horario.';
        END IF;
    END IF;

    SELECT `IsActive` INTO v_occupies
    FROM `session_statuses` WHERE `Id` = NEW.`StatusId`;

    SET NEW.`ActiveAt` = IF(v_occupies = 1, NEW.`ScheduledAt`, NULL);
END$$

--  Registra cada cambio de estado sin que la aplicación tenga que acordarse.
CREATE TRIGGER `TRG_sessions_history_insert`
AFTER INSERT ON `advisory_sessions`
FOR EACH ROW
BEGIN
    INSERT INTO `session_status_history` (`SessionId`, `FromStatusId`, `ToStatusId`)
    VALUES (NEW.`Id`, NULL, NEW.`StatusId`);
END$$

CREATE TRIGGER `TRG_sessions_history_update`
AFTER UPDATE ON `advisory_sessions`
FOR EACH ROW
BEGIN
    IF NEW.`StatusId` <> OLD.`StatusId` THEN
        INSERT INTO `session_status_history` (`SessionId`, `FromStatusId`, `ToStatusId`)
        VALUES (NEW.`Id`, OLD.`StatusId`, NEW.`StatusId`);
    END IF;
END$$

DELIMITER ;


-- =============================================================================
--  8. VISTAS DE COMPATIBILIDAD
--
--  Presentan el modelo v2 con la forma que espera el código actual, para
--  poder migrar la aplicación por partes en lugar de todo de golpe.
-- =============================================================================

CREATE OR REPLACE VIEW `v_advisors` AS
SELECT  p.`Id`,
        p.`DisplayName`    AS `FullName`,
        p.`Email`,
        pr.`Name`          AS `Area`,
        m.`Name`           AS `DefaultModality`,
        ap.`IsActive`
FROM `advisor_profiles` ap
JOIN `people`     p  ON p.`Id`  = ap.`PersonId`
JOIN `programs`   pr ON pr.`Id` = ap.`ProgramId`
JOIN `modalities` m  ON m.`Id`  = ap.`DefaultModalityId`;


CREATE OR REPLACE VIEW `v_students` AS
SELECT  p.`Id`,
        p.`DisplayName` AS `FullName`,
        p.`Email`,
        sp.`StudentNumber`
FROM `student_profiles` sp
JOIN `people` p ON p.`Id` = sp.`PersonId`;


--  Resuelve TODOS los roles de una persona en una sola consulta. Sustituye la
--  cadena de tres SELECT de ResolveProfileHandler, que devolvía solo el
--  primero y por eso rompía a los asesores pares.
CREATE OR REPLACE VIEW `v_person_roles` AS
SELECT p.`Id` AS `PersonId`, p.`Email`, p.`DisplayName`, 'Directivo' AS `Role`
FROM `people` p JOIN `admin_profiles`   x ON x.`PersonId` = p.`Id` AND x.`IsActive` = 1
UNION ALL
SELECT p.`Id`, p.`Email`, p.`DisplayName`, 'Asesor'
FROM `people` p JOIN `advisor_profiles` x ON x.`PersonId` = p.`Id` AND x.`IsActive` = 1
UNION ALL
SELECT p.`Id`, p.`Email`, p.`DisplayName`, 'Alumno'
FROM `people` p JOIN `student_profiles` x ON x.`PersonId` = p.`Id` AND x.`IsActive` = 1;


--  Ocupación de cada bloque, POR FECHA.
--
--  El cupo es por ocurrencia: un bloque semanal de MaxCapacity = 1 admite un
--  alumno CADA semana, no uno en todo el ciclo. Por eso se agrupa también por
--  ActiveAt; agrupando solo por bloque, las citas de semanas distintas se
--  sumaban contra el mismo cupo y SeatsLeft salía negativo.
--
--  SeatsLeft se calcula con CAST a SIGNED: MaxCapacity es unsigned y en MySQL
--  una resta entre unsigned que dé negativo aborta la consulta en vez de
--  devolver el número.
--
--  Un bloque sin citas aparece con OccursAt NULL y el cupo entero libre.
CREATE OR REPLACE VIEW `v_availability_load` AS
SELECT  a.`Id` AS `AvailabilityId`,
        a.`TermId`,
        a.`AdvisorId`,
        a.`DayOfWeek`,
        a.`StartTime`,
        a.`EndTime`,
        a.`MaxCapacity`,
        l.`Name` AS `Location`,
        m.`Name` AS `Modality`,
        s.`ActiveAt` AS `OccursAt`,
        COUNT(s.`Id`) AS `ActiveSessions`,
        CAST(a.`MaxCapacity` AS SIGNED) - COUNT(s.`Id`) AS `SeatsLeft`
FROM `availabilities` a
JOIN `locations`  l ON l.`Id` = a.`LocationId`
JOIN `modalities` m ON m.`Id` = l.`ModalityId`
LEFT JOIN `advisory_sessions` s
       ON s.`AvailabilityId` = a.`Id` AND s.`ActiveAt` IS NOT NULL
WHERE a.`IsActive` = 1
GROUP BY a.`Id`, a.`TermId`, a.`AdvisorId`, a.`DayOfWeek`, a.`StartTime`,
         a.`EndTime`, a.`MaxCapacity`, l.`Name`, m.`Name`, s.`ActiveAt`;
