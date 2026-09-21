-- =============================================================================
--  Sistema de Asesorías FCQI — ESQUEMA DE BASE DE DATOS
--  MySQL 8.0 · InnoDB · utf8mb4
--
--  Este archivo contiene ÚNICAMENTE la estructura (DDL). No inserta datos:
--  de eso se encarga 02-seed.sql, que debe ejecutarse después.
--
--      mysql -u root -p < db/01-schema.sql
--      mysql -u root -p < db/02-seed.sql
--
--  Refleja el esquema generado por las migraciones de EF Core:
--    20260830235519_InitialCreate  +  20260909210044_Catalog20262
--  Cualquier cambio aquí debe ir acompañado de su migración equivalente,
--  o el modelo de EF Core y la base de datos quedarán desincronizados.
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE DATABASE IF NOT EXISTS `fcqi_asesorias`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE `fcqi_asesorias`;


-- -----------------------------------------------------------------------------
--  Tablas de identidad. Una por rol: el correo institucional es la identidad
--  real y la tabla en la que aparece determina el rol (ver ResolveProfileHandler).
--  No se almacenan contraseñas: la autenticación es Google OAuth + JWT.
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `admins`;
CREATE TABLE `admins` (
    `Id`       int          NOT NULL AUTO_INCREMENT,
    `FullName` varchar(200) NOT NULL,
    `Email`    varchar(150) NOT NULL,
    `Title`    varchar(200) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_admins_Email` (`Email`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;


DROP TABLE IF EXISTS `advisors`;
CREATE TABLE `advisors` (
    `Id`              int          NOT NULL AUTO_INCREMENT,
    `FullName`        varchar(200) NOT NULL,
    `Email`           varchar(150) NOT NULL,
    `Area`            varchar(120) NOT NULL DEFAULT '',
    `DefaultModality` varchar(50)  NOT NULL DEFAULT '',
    `IsActive`        tinyint(1)   NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_advisors_Email` (`Email`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;


DROP TABLE IF EXISTS `students`;
CREATE TABLE `students` (
    `Id`            int          NOT NULL AUTO_INCREMENT,
    `FullName`      varchar(200) NOT NULL,
    `Email`         varchar(150) NOT NULL,
    `StudentNumber` varchar(20)  NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_students_Email`         (`Email`),
    UNIQUE KEY `IX_students_StudentNumber` (`StudentNumber`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  Catálogo académico.
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `subjects`;
CREATE TABLE `subjects` (
    `Id`      int          NOT NULL AUTO_INCREMENT,
    `Code`    varchar(120) NOT NULL,
    `Name`    varchar(200) NOT NULL,
    `Program` varchar(100) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_subjects_Code` (`Code`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  Relaciones.
-- -----------------------------------------------------------------------------

--  N:M entre asesores y las materias que imparten. Clave primaria compuesta:
--  la pareja (asesor, materia) no puede repetirse.
DROP TABLE IF EXISTS `advisor_subjects`;
CREATE TABLE `advisor_subjects` (
    `AdvisorId` int NOT NULL,
    `SubjectId` int NOT NULL,
    PRIMARY KEY (`AdvisorId`, `SubjectId`),
    KEY `IX_advisor_subjects_SubjectId` (`SubjectId`),
    CONSTRAINT `FK_advisor_subjects_advisors_AdvisorId`
        FOREIGN KEY (`AdvisorId`) REFERENCES `advisors` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_advisor_subjects_subjects_SubjectId`
        FOREIGN KEY (`SubjectId`) REFERENCES `subjects` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;


--  Horario semanal recurrente de cada asesor. DayOfWeek sigue la convención de
--  System.DayOfWeek: 0 = domingo … 6 = sábado.
DROP TABLE IF EXISTS `availabilities`;
CREATE TABLE `availabilities` (
    `Id`          int          NOT NULL AUTO_INCREMENT,
    `AdvisorId`   int          NOT NULL,
    `DayOfWeek`   int          NOT NULL,
    `StartTime`   time(6)      NOT NULL,
    `EndTime`     time(6)      NOT NULL,
    `MaxCapacity` int          NOT NULL,
    `Modality`    varchar(50)  NOT NULL,
    `Location`    varchar(200) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_availabilities_AdvisorId` (`AdvisorId`),
    CONSTRAINT `FK_availabilities_advisors_AdvisorId`
        FOREIGN KEY (`AdvisorId`) REFERENCES `advisors` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;


--  Cita concreta entre un alumno y un asesor. AvailabilityId es opcional:
--  si el bloque de horario se elimina, la sesión sobrevive con NULL.
DROP TABLE IF EXISTS `advisory_sessions`;
CREATE TABLE `advisory_sessions` (
    `Id`             int          NOT NULL AUTO_INCREMENT,
    `StudentId`      int          NOT NULL,
    `AdvisorId`      int          NOT NULL,
    `SubjectId`      int          NOT NULL,
    `AvailabilityId` int          NULL DEFAULT NULL,
    `ScheduledAt`    datetime(6)  NOT NULL,
    `Topic`          varchar(500) NOT NULL,
    `Status`         varchar(50)  NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_advisory_sessions_AdvisorId`      (`AdvisorId`),
    KEY `IX_advisory_sessions_AvailabilityId` (`AvailabilityId`),
    KEY `IX_advisory_sessions_StudentId`      (`StudentId`),
    KEY `IX_advisory_sessions_SubjectId`      (`SubjectId`),
    CONSTRAINT `FK_advisory_sessions_advisors_AdvisorId`
        FOREIGN KEY (`AdvisorId`) REFERENCES `advisors` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_advisory_sessions_availabilities_AvailabilityId`
        FOREIGN KEY (`AvailabilityId`) REFERENCES `availabilities` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_advisory_sessions_students_StudentId`
        FOREIGN KEY (`StudentId`) REFERENCES `students` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_advisory_sessions_subjects_SubjectId`
        FOREIGN KEY (`SubjectId`) REFERENCES `subjects` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------------------------------
--  Control de migraciones de EF Core.
--
--  La API ejecuta db.Database.MigrateAsync() al arrancar. Si esta tabla está
--  vacía, EF Core intentará crear desde cero un esquema que ya existe y fallará.
--  Estas filas le indican que ambas migraciones ya fueron aplicadas.
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `__EFMigrationsHistory`;
CREATE TABLE `__EFMigrationsHistory` (
    `MigrationId`    varchar(150) NOT NULL,
    `ProductVersion` varchar(32)  NOT NULL,
    PRIMARY KEY (`MigrationId`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES
    ('20260830235519_InitialCreate', '8.0.13'),
    ('20260909210044_Catalog20262',  '8.0.13');

SET FOREIGN_KEY_CHECKS = 1;
