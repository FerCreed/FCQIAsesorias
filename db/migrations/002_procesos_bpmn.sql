-- =============================================================================
--  Migración 002 — procesos BPMN 6, 9, 10 y 11
--  MySQL 8.0.16+ · idempotente: se puede ejecutar más de una vez.
--
--  Para bases creadas con una versión anterior de db/01-schema.sql, sin
--  perder datos. Una base nueva ya trae todo esto desde 01-schema.sql.
--
--      mysql -u root -p fcqi_asesorias < db/migrations/002_procesos_bpmn.sql
--
--  Cambios:
--    1. estados_sesion: estado 5 'Realizada' (asesoría impartida con bitácora).
--    2. asesorias.EnlaceReunion: enlace de la reunión virtual.
--    3. bitacoras_asesoria: pase de lista y notas de avance.
-- =============================================================================

USE `fcqi_asesorias`;

INSERT IGNORE INTO `estados_sesion` (`Id`, `Codigo`, `Nombre`, `Activo`)
VALUES (5, 'COMPLETED', 'Realizada', 0);

-- MySQL 8.0 no tiene ADD COLUMN IF NOT EXISTS: se consulta el catálogo.
SET @falta_enlace = (
    SELECT COUNT(*) = 0 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'asesorias' AND COLUMN_NAME = 'EnlaceReunion');
SET @sql = IF(@falta_enlace,
    'ALTER TABLE `asesorias` ADD COLUMN `EnlaceReunion` varchar(300) NULL AFTER `Tema`',
    'DO 0');
PREPARE paso FROM @sql;
EXECUTE paso;
DEALLOCATE PREPARE paso;

CREATE TABLE IF NOT EXISTS `bitacoras_asesoria` (
    `AsesoriaId`    bigint unsigned NOT NULL,
    `Asistio`       tinyint(1)    NOT NULL,
    `Notas`         varchar(2000) NULL,
    `RegistradoPor` int unsigned  NOT NULL,
    `RegistradoEn`  datetime(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`AsesoriaId`),
    KEY `IX_bitacoras_RegistradoPor` (`RegistradoPor`),
    CONSTRAINT `FK_bitacoras_asesorias`
        FOREIGN KEY (`AsesoriaId`) REFERENCES `asesorias` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_bitacoras_personas`
        FOREIGN KEY (`RegistradoPor`) REFERENCES `personas` (`Id`) ON DELETE RESTRICT
) ENGINE = InnoDB;
