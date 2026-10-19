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
--    3. CupoMaximo deja de ser dato muerto: el cupo se respeta.
--    4. Estados, modalidades y áreas son catálogos con integridad
--       referencial, no texto libre.
--    5. Todo cuelga de un ciclo escolar, y los cambios quedan auditados.
--
--  Excepción deliberada a "solo estructura": las filas de los catálogos fijos
--  (modalidades, estados_sesion) se insertan aquí. No son datos del dominio
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
DROP VIEW  IF EXISTS `v_asesores`, `v_alumnos`, `v_roles_persona`, `v_ocupacion_horarios`;
DROP TABLE IF EXISTS `historial_estados_sesion`, `asesorias`, `asesores_materias`,
                     `horarios`, `programas_materias`, `materias`, `perfiles_directivo`,
                     `perfiles_alumno`, `perfiles_asesor`, `personas`, `lugares`,
                     `ciclos_escolares`, `programas`, `estados_sesion`, `modalidades`,
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

CREATE TABLE `modalidades` (
    `Id`     tinyint unsigned NOT NULL,
    `Codigo` varchar(20)  NOT NULL,
    `Nombre` varchar(50)  NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_modalidades_Codigo` (`Codigo`)
) ENGINE = InnoDB;

INSERT INTO `modalidades` (`Id`, `Codigo`, `Nombre`) VALUES
    (1, 'IN_PERSON', 'Presencial'),
    (2, 'VIRTUAL',   'Virtual');


--  Activo distingue los estados que ocupan cupo (pendiente, confirmada) de
--  los que lo liberan (cancelada, rechazada). Los índices de cupo dependen
--  de esta distinción.
CREATE TABLE `estados_sesion` (
    `Id`     tinyint unsigned NOT NULL,
    `Codigo` varchar(20) NOT NULL,
    `Nombre` varchar(50) NOT NULL,
    `Activo` tinyint(1)  NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_estados_sesion_Codigo` (`Codigo`)
) ENGINE = InnoDB;

INSERT INTO `estados_sesion` (`Id`, `Codigo`, `Nombre`, `Activo`) VALUES
    (1, 'PENDING',   'Pendiente',  1),
    (2, 'CONFIRMED', 'Confirmada', 1),
    (3, 'CANCELLED', 'Cancelada',  0),
    (4, 'REJECTED',  'Rechazada',  0);


--  Programas educativos. En v1 esto era advisors.Area: texto libre repetido
--  ('Ingeniería Química' aparecía 4 veces). Un typo creaba un área fantasma.
CREATE TABLE `programas` (
    `Id`     smallint unsigned NOT NULL AUTO_INCREMENT,
    `Codigo` varchar(30)  NOT NULL,
    `Nombre` varchar(120) NOT NULL,
    `Activo` tinyint(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_programas_Codigo` (`Codigo`)
) ENGINE = InnoDB;


--  Ciclo escolar. v1 no tenía este concepto: subjects.Program valía
--  'FCQI 2026-2' en las 44 filas, mezclando carrera y periodo. Al llegar
--  2027-1 había que duplicar el catálogo o sobrescribir el historial.
CREATE TABLE `ciclos_escolares` (
    `Id`          smallint unsigned NOT NULL AUTO_INCREMENT,
    `Codigo`      varchar(10) NOT NULL COMMENT 'p. ej. 2026-2',
    `Nombre`      varchar(60) NOT NULL,
    `FechaInicio` date        NOT NULL,
    `FechaFin`    date        NOT NULL,
    `EsActual`    tinyint(1)  NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_ciclos_escolares_Codigo` (`Codigo`),
    CONSTRAINT `CK_ciclos_escolares_Rango` CHECK (`FechaFin` > `FechaInicio`)
) ENGINE = InnoDB;


--  Lugares. En v1, availabilities.Location guardaba
--  'Enlace virtual (Meet/Teams)', que no es un lugar sino una modalidad
--  disfrazada: Location determinaba Modality (dependencia transitiva).
--  Aquí el lugar declara su modalidad una sola vez.
CREATE TABLE `lugares` (
    `Id`          smallint unsigned NOT NULL AUTO_INCREMENT,
    `Nombre`      varchar(200) NOT NULL,
    `ModalidadId` tinyint unsigned NOT NULL,
    `Detalles`    varchar(500) NULL,
    `Activo`      tinyint(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_lugares_Nombre` (`Nombre`),
    KEY `IX_lugares_ModalidadId` (`ModalidadId`),
    CONSTRAINT `FK_lugares_modalidades`
        FOREIGN KEY (`ModalidadId`) REFERENCES `modalidades` (`Id`) ON DELETE RESTRICT
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

CREATE TABLE `personas` (
    `Id`              int unsigned NOT NULL AUTO_INCREMENT,
    --  Nombre separado: en v1 era un solo FullName, así que ORDER BY ordenaba
    --  por nombre de pila. Los listados oficiales van por apellido paterno.
    `Tratamiento`     varchar(20)  NULL COMMENT 'Dra., Dr., Mtro. — opcional',
    `Nombres`         varchar(100) NOT NULL,
    `ApellidoPaterno` varchar(100) NOT NULL,
    `ApellidoMaterno` varchar(100) NULL,
    --  Columna generada: evita repetir la concatenación en cada consulta y
    --  permite buscar por nombre completo con un índice.
    --  NOT NULL explícito: CONCAT_WS sobre dos columnas obligatorias nunca
    --  devuelve NULL, pero MySQL no lo deduce y la declararía nullable, lo que
    --  dejaría el modelo de EF Core y la base discrepando sin necesidad.
    `NombreCompleto`  varchar(302) AS (
        CONCAT_WS(' ', `Nombres`, `ApellidoPaterno`, `ApellidoMaterno`)
    ) STORED NOT NULL,
    `Correo`          varchar(150) NOT NULL,
    `Activo`          tinyint(1)   NOT NULL DEFAULT 1,
    `CreadoEn`        datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `ActualizadoEn`   datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
                                            ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_personas_Correo` (`Correo`),
    KEY `IX_personas_NombreCompleto` (`NombreCompleto`),
    --  La regla institucional deja de depender de una validación en C#.
    CONSTRAINT `CK_personas_CorreoInstitucional`
        CHECK (`Correo` LIKE '%@uabc.edu.mx' AND `Correo` = LOWER(`Correo`))
) ENGINE = InnoDB;


--  Perfil de alumno. PersonaId es a la vez PK y FK: relación 1:0..1 con personas.
CREATE TABLE `perfiles_alumno` (
    `PersonaId`  int unsigned NOT NULL,
    `Matricula`  varchar(20)  NOT NULL,
    --  Nullable a propósito: el modelo v1 no registraba la carrera del alumno,
    --  y ponerla obligatoria forzaría a inventar el dato. Se llena cuando el
    --  alumno la declare o se importe el padrón oficial.
    `ProgramaId` smallint unsigned NULL,
    `Activo`     tinyint(1)   NOT NULL DEFAULT 1,
    `CreadoEn`   datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`PersonaId`),
    UNIQUE KEY `UQ_perfiles_alumno_Matricula` (`Matricula`),
    KEY `IX_perfiles_alumno_ProgramaId` (`ProgramaId`),
    CONSTRAINT `FK_perfiles_alumno_personas`
        FOREIGN KEY (`PersonaId`) REFERENCES `personas` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_perfiles_alumno_programas`
        FOREIGN KEY (`ProgramaId`) REFERENCES `programas` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;


CREATE TABLE `perfiles_asesor` (
    `PersonaId`                 int unsigned NOT NULL,
    `ProgramaId`                smallint unsigned NOT NULL COMMENT 'era advisors.Area',
    `ModalidadPredeterminadaId` tinyint unsigned NOT NULL,
    `Activo`                    tinyint(1)   NOT NULL DEFAULT 1,
    `CreadoEn`                  datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`PersonaId`),
    KEY `IX_perfiles_asesor_ProgramaId` (`ProgramaId`),
    KEY `IX_perfiles_asesor_ModalidadPredeterminadaId` (`ModalidadPredeterminadaId`),
    CONSTRAINT `FK_perfiles_asesor_personas`
        FOREIGN KEY (`PersonaId`) REFERENCES `personas` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_perfiles_asesor_programas`
        FOREIGN KEY (`ProgramaId`) REFERENCES `programas` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_perfiles_asesor_modalidades`
        FOREIGN KEY (`ModalidadPredeterminadaId`) REFERENCES `modalidades` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;


CREATE TABLE `perfiles_directivo` (
    `PersonaId` int unsigned NOT NULL,
    `Cargo`     varchar(200) NOT NULL,
    `Activo`    tinyint(1)   NOT NULL DEFAULT 1,
    `CreadoEn`  datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`PersonaId`),
    CONSTRAINT `FK_perfiles_directivo_personas`
        FOREIGN KEY (`PersonaId`) REFERENCES `personas` (`Id`) ON DELETE CASCADE
) ENGINE = InnoDB;


-- =============================================================================
--  3. CATÁLOGO ACADÉMICO
-- =============================================================================

--  La materia ya no lleva columna Program: 'Cálculo Diferencial' e 'Inglés I'
--  se cursan en varias carreras. Esa relación es N:M y vive en programas_materias.
CREATE TABLE `materias` (
    `Id`     int unsigned NOT NULL AUTO_INCREMENT,
    `Codigo` varchar(120) NOT NULL,
    `Nombre` varchar(200) NOT NULL,
    `Activo` tinyint(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_materias_Codigo` (`Codigo`),
    KEY `IX_materias_Nombre` (`Nombre`)
) ENGINE = InnoDB;


CREATE TABLE `programas_materias` (
    `ProgramaId` smallint unsigned NOT NULL,
    `MateriaId`  int unsigned NOT NULL,
    `Semestre`   tinyint unsigned NULL COMMENT 'semestre sugerido del plan',
    PRIMARY KEY (`ProgramaId`, `MateriaId`),
    KEY `IX_programas_materias_MateriaId` (`MateriaId`),
    CONSTRAINT `FK_programas_materias_programas`
        FOREIGN KEY (`ProgramaId`) REFERENCES `programas` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_programas_materias_materias`
        FOREIGN KEY (`MateriaId`) REFERENCES `materias` (`Id`) ON DELETE CASCADE
) ENGINE = InnoDB;


-- =============================================================================
--  4. OFERTA DE ASESORÍAS (por ciclo)
-- =============================================================================

--  Qué materias asesora cada asesor, EN QUÉ CICLO. En v1 la asignación no
--  tenía ciclo, así que la del semestre pasado se perdía al reasignar.
CREATE TABLE `asesores_materias` (
    `CicloId`   smallint unsigned NOT NULL,
    `AsesorId`  int unsigned NOT NULL,
    `MateriaId` int unsigned NOT NULL,
    `CreadoEn`  datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`CicloId`, `AsesorId`, `MateriaId`),
    KEY `IX_asesores_materias_MateriaId` (`MateriaId`),
    KEY `IX_asesores_materias_AsesorId` (`AsesorId`),
    CONSTRAINT `FK_asesores_materias_ciclos`
        FOREIGN KEY (`CicloId`) REFERENCES `ciclos_escolares` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_asesores_materias_asesores`
        FOREIGN KEY (`AsesorId`) REFERENCES `perfiles_asesor` (`PersonaId`) ON DELETE CASCADE,
    CONSTRAINT `FK_asesores_materias_materias`
        FOREIGN KEY (`MateriaId`) REFERENCES `materias` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;


--  Horario semanal recurrente. La columna Modality de v1 desaparece: la
--  modalidad la determina LugarId.
--  DiaSemana conserva la convención de System.DayOfWeek (0=domingo .. 6=sábado)
--  para no romper el código existente.
CREATE TABLE `horarios` (
    `Id`         int unsigned NOT NULL AUTO_INCREMENT,
    `CicloId`    smallint unsigned NOT NULL,
    `AsesorId`   int unsigned NOT NULL,
    `DiaSemana`  tinyint unsigned NOT NULL,
    `HoraInicio` time(0)      NOT NULL,
    `HoraFin`    time(0)      NOT NULL,
    `CupoMaximo` smallint unsigned NOT NULL DEFAULT 1,
    `LugarId`    smallint unsigned NOT NULL,
    `Activo`     tinyint(1)   NOT NULL DEFAULT 1,
    `CreadoEn`   datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    --  Necesaria para la llave foránea compuesta desde asesorias.
    --  Es lo que hace imposible que una sesión mienta sobre su asesor.
    UNIQUE KEY `UQ_horarios_Id_AsesorId` (`Id`, `AsesorId`),
    --  Un asesor no puede declarar dos veces el mismo bloque.
    UNIQUE KEY `UQ_horarios_Bloque` (`CicloId`, `AsesorId`, `DiaSemana`, `HoraInicio`),
    KEY `IX_horarios_AsesorId` (`AsesorId`),
    KEY `IX_horarios_LugarId` (`LugarId`),
    KEY `IX_horarios_CicloId_DiaSemana` (`CicloId`, `DiaSemana`),
    CONSTRAINT `FK_horarios_ciclos`
        FOREIGN KEY (`CicloId`) REFERENCES `ciclos_escolares` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_horarios_asesores`
        FOREIGN KEY (`AsesorId`) REFERENCES `perfiles_asesor` (`PersonaId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_horarios_lugares`
        FOREIGN KEY (`LugarId`) REFERENCES `lugares` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `CK_horarios_DiaSemana` CHECK (`DiaSemana` BETWEEN 0 AND 6),
    CONSTRAINT `CK_horarios_RangoHoras` CHECK (`HoraFin` > `HoraInicio`),
    CONSTRAINT `CK_horarios_Cupo`  CHECK (`CupoMaximo` >= 1)
) ENGINE = InnoDB;


-- =============================================================================
--  5. SESIONES
--
--  El corazón del rediseño. Tres garantías que v1 dejaba en manos del código:
--
--  (a) El asesor de la sesión ES el dueño del horario.
--      FK compuesta (HorarioId, AsesorId) -> horarios(Id, AsesorId).
--      AsesorId sigue presente como columna por razones prácticas: sin ella,
--      toda consulta de "mis asesorías" exigiría un JOIN extra, y no se podría
--      declarar la garantía (b). La dependencia transitiva que esto implica
--      queda neutralizada: la FK hace imposible el valor inconsistente, así
--      que no existe anomalía de actualización.
--
--  (b) El asesor imparte esa materia en ese ciclo.
--      FK compuesta (CicloId, AsesorId, MateriaId) -> asesores_materias.
--
--  (c) El cupo del bloque se respeta y nadie se duplica.
--      Ver los índices únicos sobre la columna generada ActivaEn.
-- =============================================================================

CREATE TABLE `asesorias` (
    `Id`            bigint unsigned NOT NULL AUTO_INCREMENT,
    `CicloId`       smallint unsigned NOT NULL,
    `HorarioId`     int unsigned NOT NULL,
    `AsesorId`      int unsigned NOT NULL,
    `AlumnoId`      int unsigned NOT NULL,
    `MateriaId`     int unsigned NOT NULL,
    --  UTC. En v1 era hora local sin zona, y Ensenada observa horario de
    --  verano: las citas que cruzaban el cambio se corrían una hora.
    `ProgramadaEn`  datetime(6)  NOT NULL COMMENT 'UTC; la hora local es America/Tijuana',
    `NumeroLugar`   smallint unsigned NOT NULL DEFAULT 1,
    `EstadoId`      tinyint unsigned NOT NULL DEFAULT 1,
    `Tema`          varchar(500) NOT NULL,
    `CreadoEn`      datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `ActualizadoEn` datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
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
    --  el trigger además permite consultar estados_sesion.Activo en vez
    --  de incrustar los IDs 1 y 2, que una columna generada no podría leer.
    --  Cualquier valor que venga en el INSERT lo sobrescribe el trigger.
    `ActivaEn`      datetime(6) NULL,

    PRIMARY KEY (`Id`),

    --  (c) Un asiento de un bloque, a una hora dada, lo ocupa una sola sesión
    --  activa. Con CupoMaximo = 1 equivale a "no hay sobreventa".
    UNIQUE KEY `UQ_asesorias_Lugar` (`HorarioId`, `ActivaEn`, `NumeroLugar`),
    --  El mismo alumno no puede tener dos solicitudes activas en el mismo
    --  bloque y hora. En v1 esto era una consulta en C#.
    UNIQUE KEY `UQ_asesorias_AlumnoBloque` (`AlumnoId`, `HorarioId`, `ActivaEn`),

    KEY `IX_asesorias_AsesorId_ProgramadaEn` (`AsesorId`, `ProgramadaEn`),
    KEY `IX_asesorias_AlumnoId_ProgramadaEn` (`AlumnoId`, `ProgramadaEn`),
    KEY `IX_asesorias_EstadoId` (`EstadoId`),
    KEY `IX_asesorias_MateriaId` (`MateriaId`),
    KEY `IX_asesorias_CicloId` (`CicloId`),
    KEY `FK_asesorias_imparte` (`CicloId`, `AsesorId`, `MateriaId`),

    --  (a) el horario pertenece a ese asesor
    CONSTRAINT `FK_asesorias_horario_asesor`
        FOREIGN KEY (`HorarioId`, `AsesorId`)
        REFERENCES `horarios` (`Id`, `AsesorId`) ON DELETE RESTRICT,
    --  (b) ese asesor imparte esa materia en ese ciclo
    CONSTRAINT `FK_asesorias_imparte`
        FOREIGN KEY (`CicloId`, `AsesorId`, `MateriaId`)
        REFERENCES `asesores_materias` (`CicloId`, `AsesorId`, `MateriaId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_asesorias_alumnos`
        FOREIGN KEY (`AlumnoId`) REFERENCES `perfiles_alumno` (`PersonaId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_asesorias_estados`
        FOREIGN KEY (`EstadoId`) REFERENCES `estados_sesion` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `CK_asesorias_Lugar` CHECK (`NumeroLugar` >= 1)
) ENGINE = InnoDB;


-- =============================================================================
--  6. AUDITORÍA
--
--  v1 no registraba nada. Cuando un alumno reclamara que su asesoría fue
--  cancelada, no había forma de saber quién ni cuándo. En un sistema
--  institucional eso no es opcional.
-- =============================================================================

CREATE TABLE `historial_estados_sesion` (
    `Id`               bigint unsigned NOT NULL AUTO_INCREMENT,
    `AsesoriaId`       bigint unsigned NOT NULL,
    `EstadoAnteriorId` tinyint unsigned NULL COMMENT 'NULL = creación',
    `EstadoNuevoId`    tinyint unsigned NOT NULL,
    `CambiadoPor`      int unsigned NULL COMMENT 'NULL = proceso automático',
    `CambiadoEn`       datetime(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `Motivo`           varchar(500) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_historial_AsesoriaId_CambiadoEn` (`AsesoriaId`, `CambiadoEn`),
    KEY `IX_historial_CambiadoPor` (`CambiadoPor`),
    KEY `IX_historial_EstadoNuevoId` (`EstadoNuevoId`),
    KEY `IX_historial_EstadoAnteriorId` (`EstadoAnteriorId`),
    CONSTRAINT `FK_historial_asesorias`
        FOREIGN KEY (`AsesoriaId`) REFERENCES `asesorias` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_historial_estado_anterior`
        FOREIGN KEY (`EstadoAnteriorId`) REFERENCES `estados_sesion` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_historial_estado_nuevo`
        FOREIGN KEY (`EstadoNuevoId`) REFERENCES `estados_sesion` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_historial_personas`
        FOREIGN KEY (`CambiadoPor`) REFERENCES `personas` (`Id`) ON DELETE SET NULL
) ENGINE = InnoDB;


-- =============================================================================
--  7. VALIDACIÓN DE CUPO
--
--  Los índices únicos de asesorias impiden que dos sesiones activas
--  compartan asiento, pero no pueden comparar NumeroLugar contra el CupoMaximo
--  del bloque: un CHECK de MySQL no puede consultar otra tabla. Sin esto,
--  NumeroLugar = 99 entra en un bloque de cupo 2 y la sobreventa vuelve por
--  la puerta de atrás. De ahí los triggers.
--
--  Nota sobre lo que NO se valida aquí: que ProgramadaEn caiga realmente en el
--  día y la hora del bloque. Comprobarlo exige CONVERT_TZ entre UTC y
--  America/Tijuana, que depende de las tablas de zona horaria de MySQL
--  (mysql_tzinfo_to_sql), ausentes en muchas instalaciones. Esa validación
--  se queda en la aplicación, donde la zona horaria es explícita.
-- =============================================================================

DELIMITER $$

CREATE TRIGGER `TRG_asesorias_antes_insertar`
BEFORE INSERT ON `asesorias`
FOR EACH ROW
BEGIN
    DECLARE v_capacity  SMALLINT UNSIGNED;
    DECLARE v_slot_open TINYINT(1);
    DECLARE v_occupies  TINYINT(1);

    SELECT `CupoMaximo`, `Activo` INTO v_capacity, v_slot_open
    FROM `horarios` WHERE `Id` = NEW.`HorarioId`;

    IF NEW.`NumeroLugar` > v_capacity THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El asiento excede el cupo del bloque de horario.';
    END IF;

    IF v_slot_open = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El bloque de horario está dado de baja.';
    END IF;

    SELECT `Activo` INTO v_occupies
    FROM `estados_sesion` WHERE `Id` = NEW.`EstadoId`;

    SET NEW.`ActivaEn` = IF(v_occupies = 1, NEW.`ProgramadaEn`, NULL);
END$$

CREATE TRIGGER `TRG_asesorias_antes_actualizar`
BEFORE UPDATE ON `asesorias`
FOR EACH ROW
BEGIN
    DECLARE v_capacity SMALLINT UNSIGNED;
    DECLARE v_occupies TINYINT(1);

    IF NEW.`NumeroLugar` <> OLD.`NumeroLugar`
       OR NEW.`HorarioId` <> OLD.`HorarioId` THEN
        SELECT `CupoMaximo` INTO v_capacity
        FROM `horarios` WHERE `Id` = NEW.`HorarioId`;

        IF NEW.`NumeroLugar` > v_capacity THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'El asiento excede el cupo del bloque de horario.';
        END IF;
    END IF;

    SELECT `Activo` INTO v_occupies
    FROM `estados_sesion` WHERE `Id` = NEW.`EstadoId`;

    SET NEW.`ActivaEn` = IF(v_occupies = 1, NEW.`ProgramadaEn`, NULL);
END$$

--  Registra cada cambio de estado sin que la aplicación tenga que acordarse.
CREATE TRIGGER `TRG_asesorias_historial_insertar`
AFTER INSERT ON `asesorias`
FOR EACH ROW
BEGIN
    INSERT INTO `historial_estados_sesion` (`AsesoriaId`, `EstadoAnteriorId`, `EstadoNuevoId`)
    VALUES (NEW.`Id`, NULL, NEW.`EstadoId`);
END$$

CREATE TRIGGER `TRG_asesorias_historial_actualizar`
AFTER UPDATE ON `asesorias`
FOR EACH ROW
BEGIN
    IF NEW.`EstadoId` <> OLD.`EstadoId` THEN
        INSERT INTO `historial_estados_sesion` (`AsesoriaId`, `EstadoAnteriorId`, `EstadoNuevoId`)
        VALUES (NEW.`Id`, OLD.`EstadoId`, NEW.`EstadoId`);
    END IF;
END$$

DELIMITER ;


-- =============================================================================
--  8. VISTAS DE CONSULTA
--
--  Aplanan el modelo para las consultas de listado, que de otro modo repiten
--  los mismos JOIN entre la persona y su perfil.
-- =============================================================================

CREATE OR REPLACE VIEW `v_asesores` AS
SELECT  p.`Id`,
        p.`NombreCompleto`,
        p.`Correo`,
        pr.`Nombre` AS `Programa`,
        m.`Nombre`  AS `ModalidadPredeterminada`,
        ap.`Activo`
FROM `perfiles_asesor` ap
JOIN `personas`    p  ON p.`Id`  = ap.`PersonaId`
JOIN `programas`   pr ON pr.`Id` = ap.`ProgramaId`
JOIN `modalidades` m  ON m.`Id`  = ap.`ModalidadPredeterminadaId`;


CREATE OR REPLACE VIEW `v_alumnos` AS
SELECT  p.`Id`,
        p.`NombreCompleto`,
        p.`Correo`,
        sp.`Matricula`
FROM `perfiles_alumno` sp
JOIN `personas` p ON p.`Id` = sp.`PersonaId`;


--  Resuelve TODOS los roles de una persona en una sola consulta. Sustituye la
--  cadena de tres SELECT de ResolveProfileHandler, que devolvía solo el
--  primero y por eso rompía a los asesores pares.
CREATE OR REPLACE VIEW `v_roles_persona` AS
SELECT p.`Id` AS `PersonaId`, p.`Correo`, p.`NombreCompleto`, 'Directivo' AS `Rol`
FROM `personas` p JOIN `perfiles_directivo` x ON x.`PersonaId` = p.`Id` AND x.`Activo` = 1
UNION ALL
SELECT p.`Id`, p.`Correo`, p.`NombreCompleto`, 'Asesor'
FROM `personas` p JOIN `perfiles_asesor` x ON x.`PersonaId` = p.`Id` AND x.`Activo` = 1
UNION ALL
SELECT p.`Id`, p.`Correo`, p.`NombreCompleto`, 'Alumno'
FROM `personas` p JOIN `perfiles_alumno` x ON x.`PersonaId` = p.`Id` AND x.`Activo` = 1;


--  Ocupación de cada bloque, POR FECHA.
--
--  El cupo es por ocurrencia: un bloque semanal de CupoMaximo = 1 admite un
--  alumno CADA semana, no uno en todo el ciclo. Por eso se agrupa también por
--  ActivaEn; agrupando solo por bloque, las citas de semanas distintas se
--  sumaban contra el mismo cupo y LugaresDisponibles salía negativo.
--
--  LugaresDisponibles se calcula con CAST a SIGNED: CupoMaximo es unsigned
--  y en MySQL una resta entre unsigned que dé negativo aborta la consulta en
--  vez de devolver el número.
--
--  Un bloque sin citas aparece con OcurreEn NULL y el cupo entero libre.
CREATE OR REPLACE VIEW `v_ocupacion_horarios` AS
SELECT  a.`Id` AS `HorarioId`,
        a.`CicloId`,
        a.`AsesorId`,
        a.`DiaSemana`,
        a.`HoraInicio`,
        a.`HoraFin`,
        a.`CupoMaximo`,
        l.`Nombre` AS `Lugar`,
        m.`Nombre` AS `Modalidad`,
        s.`ActivaEn` AS `OcurreEn`,
        COUNT(s.`Id`) AS `AsesoriasActivas`,
        CAST(a.`CupoMaximo` AS SIGNED) - COUNT(s.`Id`) AS `LugaresDisponibles`
FROM `horarios` a
JOIN `lugares`     l ON l.`Id` = a.`LugarId`
JOIN `modalidades` m ON m.`Id` = l.`ModalidadId`
LEFT JOIN `asesorias` s
       ON s.`HorarioId` = a.`Id` AND s.`ActivaEn` IS NOT NULL
WHERE a.`Activo` = 1
GROUP BY a.`Id`, a.`CicloId`, a.`AsesorId`, a.`DiaSemana`, a.`HoraInicio`,
         a.`HoraFin`, a.`CupoMaximo`, l.`Nombre`, m.`Nombre`, s.`ActivaEn`;
