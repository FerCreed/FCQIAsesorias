-- =============================================================================
--  Sistema de Asesorías FCQI — DATOS INICIALES (catálogo 2026-2)
--  MySQL 8.0
--
--  Este archivo contiene ÚNICAMENTE inserciones. Requiere que 01-schema.sql
--  se haya ejecutado antes:
--
--      mysql -u root -p < db/01-schema.sql
--      mysql -u root -p < db/02-seed.sql
--
--  Es idempotente: vacía las tablas antes de insertar, así que puede volver a
--  ejecutarse sobre una base ya poblada y la deja en un estado conocido.
--  ATENCIÓN: eso BORRA las sesiones agendadas por los usuarios. No ejecutar
--  en producción sobre datos reales.
--
--  Origen: portado desde Catalog20262Seeder.cs. Los IDs son explícitos y
--  estables para que las llaves foráneas de este archivo no dependan del
--  AUTO_INCREMENT.
-- =============================================================================

SET NAMES utf8mb4;
USE `fcqi_asesorias`;

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE `advisory_sessions`;
TRUNCATE TABLE `advisor_subjects`;
TRUNCATE TABLE `availabilities`;
TRUNCATE TABLE `subjects`;
TRUNCATE TABLE `students`;
TRUNCATE TABLE `admins`;
TRUNCATE TABLE `advisors`;
SET FOREIGN_KEY_CHECKS = 1;

START TRANSACTION;


-- 44 materias del catalogo FCQI 2026-2.
INSERT INTO `subjects` (`Id`, `Code`, `Name`, `Program`) VALUES
    (1, 'INTRODUCCION-A-LAS-MATEMATICAS-UNIVERSITARIAS', 'Introducción a las Matemáticas Universitarias', 'FCQI 2026-2'),
    (2, 'CALCULO-DIFERENCIAL', 'Cálculo Diferencial', 'FCQI 2026-2'),
    (3, 'CALCULO-INTEGRAL', 'Cálculo Integral', 'FCQI 2026-2'),
    (4, 'CIRCUITOS-DE-CORRIENTE-DIRECTA', 'Circuitos de Corriente Directa', 'FCQI 2026-2'),
    (5, 'CIRCUITOS-DE-CORRIENTE-ALTERNA', 'Circuitos de Corriente Alterna', 'FCQI 2026-2'),
    (6, 'MODELADO-Y-CONTROL', 'Modelado y Control', 'FCQI 2026-2'),
    (7, 'ECUACIONES-DIFERENCIALES', 'Ecuaciones Diferenciales', 'FCQI 2026-2'),
    (8, 'INGENIERIA-ECONOMICA', 'Ingeniería Económica', 'FCQI 2026-2'),
    (9, 'CIRCUITOS-ELECTRICOS', 'Circuitos Eléctricos', 'FCQI 2026-2'),
    (10, 'INGLES-I', 'Inglés I', 'FCQI 2026-2'),
    (11, 'INGLES-II', 'Inglés II', 'FCQI 2026-2'),
    (12, 'PROBABILIDAD-Y-ESTADISTICA', 'Probabilidad y Estadística', 'FCQI 2026-2'),
    (13, 'INVESTIGACION-DE-OPERACIONES-I', 'Investigación de Operaciones I', 'FCQI 2026-2'),
    (14, 'ALGEBRA-SUPERIOR', 'Álgebra Superior', 'FCQI 2026-2'),
    (15, 'BALANCE-DE-MATERIA-Y-ENERGIA', 'Balance de Materia y Energía', 'FCQI 2026-2'),
    (16, 'CINETICA-QUIMICA-Y-CATALISIS', 'Cinética Química y Catálisis', 'FCQI 2026-2'),
    (17, 'REACTORES-HOMOGENEOS-Y-HETEROGENEOS', 'Reactores Homogéneos y Heterogéneos', 'FCQI 2026-2'),
    (18, 'QUIMICA', 'Química', 'FCQI 2026-2'),
    (19, 'ELECTRICIDAD-Y-MAGNETISMO', 'Electricidad y Magnetismo', 'FCQI 2026-2'),
    (20, 'TERMODINAMICA', 'Termodinámica', 'FCQI 2026-2'),
    (21, 'CONTROL-E-INSTRUMENTACION-DE-PROCESOS', 'Control e Instrumentación de Procesos', 'FCQI 2026-2'),
    (22, 'OPERACIONES-DE-SEPARACION', 'Operaciones de Separación', 'FCQI 2026-2'),
    (23, 'OPERACIONES-DE-TRANSFERENCIA-DE-CALOR', 'Operaciones de Transferencia de Calor', 'FCQI 2026-2'),
    (24, 'QUIMICA-ORGANICA-I', 'Química Orgánica I', 'FCQI 2026-2'),
    (25, 'QUIMICA-GENERAL', 'Química General', 'FCQI 2026-2'),
    (26, 'QUIMICA-ORGANICA-II', 'Química Orgánica II', 'FCQI 2026-2'),
    (27, 'BIOQUIMICA', 'Bioquímica', 'FCQI 2026-2'),
    (28, 'MATEMATICAS-BASICAS', 'Matemáticas Básicas', 'FCQI 2026-2'),
    (29, 'QUIMICA-ANALITICA-I', 'Química Analítica I', 'FCQI 2026-2'),
    (30, 'BIOLOGIA', 'Biología', 'FCQI 2026-2'),
    (31, 'BIOLOGIA-MOLECULAR', 'Biología Molecular', 'FCQI 2026-2'),
    (32, 'BIOQUIMICA-CLINICA', 'Bioquímica Clínica', 'FCQI 2026-2'),
    (33, 'BIOQUIMICA-ESTRUCTURAL', 'Bioquímica Estructural', 'FCQI 2026-2'),
    (34, 'BIOQUIMICA-METABOLICA', 'Bioquímica Metabólica', 'FCQI 2026-2'),
    (35, 'FARMACOCINETICA', 'Farmacocinética', 'FCQI 2026-2'),
    (36, 'FISICA', 'Física', 'FCQI 2026-2'),
    (37, 'BIOFARMACIA', 'Biofarmacia', 'FCQI 2026-2'),
    (38, 'FARMACOLOGIA', 'Farmacología', 'FCQI 2026-2'),
    (39, 'BIOLOGIA-CELULAR', 'Biología Celular', 'FCQI 2026-2'),
    (40, 'ANALISIS-INSTRUMENTAL-I', 'Análisis Instrumental I', 'FCQI 2026-2'),
    (41, 'QUIMICA-ANALITICA-II', 'Química Analítica II', 'FCQI 2026-2'),
    (42, 'INMUNOLOGIA', 'Inmunología', 'FCQI 2026-2'),
    (43, 'ANATOMIA-Y-FISIOLOGIA', 'Anatomía y Fisiología', 'FCQI 2026-2'),
    (44, 'MATEMATICAS-AVANZADAS', 'Matemáticas Avanzadas', 'FCQI 2026-2');


-- 14 asesores activos del ciclo 2026-2.
INSERT INTO `advisors` (`Id`, `FullName`, `Email`, `Area`, `DefaultModality`, `IsActive`) VALUES
    (1, 'Felipe de Jesús Márquez Vizcarra', 'felipe.marquez63@uabc.edu.mx', 'Ingeniería en Electrónica', 'Presencial', 1),
    (2, 'Edgar Kenichi Tsuchiya Godínez', 'edgar.tsuchiya@uabc.edu.mx', 'Ingeniería en Electrónica', 'Presencial', 1),
    (3, 'Eduardo Isaías Mérida Rodríguez', 'eduardo.merida@uabc.edu.mx', 'Ingeniería Industrial', 'Presencial', 1),
    (4, 'Luis Alejandro Flores Díaz', 'alejandro.flores48@uabc.edu.mx', 'Ingeniería Industrial', 'Virtual', 1),
    (5, 'Carlos Enrique Martínez de la Cruz', 'carlos.martinez86@uabc.edu.mx', 'Ingeniería Industrial', 'Presencial', 1),
    (6, 'Dylan Josué Guerrero Luque', 'dylan.guerrero@uabc.edu.mx', 'Ingeniería Química', 'Presencial', 1),
    (7, 'Luis Alberto González Rodríguez', 'luis.gonzalez.rodriguez@uabc.edu.mx', 'Ingeniería Química', 'Presencial', 1),
    (8, 'Getzamani Guadalupe Solis Gutiérrez', 'getzamani.solis@uabc.edu.mx', 'Ingeniería Química', 'Presencial', 1),
    (9, 'Mia Italia Alexandres Carrasco', 'mia.alexandres@uabc.edu.mx', 'Ingeniería Química', 'Presencial', 1),
    (10, 'Vladimir Bonifacio Ramírez Martínez', 'v1299027@uabc.edu.mx', 'Químico Industrial', 'Presencial', 1),
    (11, 'Bryan Emilio Rafael Muñoz Campos', 'bryan.munoz@uabc.edu.mx', 'Química Farmacéutica Biológica', 'Presencial', 1),
    (12, 'Jimena Beltrán Zepeda', 'j2207105@uabc.edu.mx', 'Química Farmacéutica Biológica', 'Presencial', 1),
    (13, 'Andrés Bautista Cruz', 'andres.cruz36@uabc.edu.mx', 'Química Farmacéutica Biológica', 'Presencial', 1),
    (14, 'Braulio Reynaldo Angulo Curiel', 'angulo.braulio@uabc.edu.mx', 'Química Farmacéutica Biológica', 'Presencial', 1);


-- Responsable del programa.
INSERT INTO `admins` (`Id`, `FullName`, `Email`, `Title`) VALUES
    (1, 'Dra. Lizeth Carolina Aguilar Dodier', 'progasesorias.fcqi@uabc.edu.mx', 'Responsable del Programa de Asesorías Académicas');


-- 6 alumnos dados de alta.
INSERT INTO `students` (`Id`, `FullName`, `Email`, `StudentNumber`) VALUES
    (1, 'Yesua Fernando Díaz Hernández', 'yesua.diaz@uabc.edu.mx', '2208134'),
    (2, 'Juan Carlos Laguna Hernández', 'juan.laguna@uabc.edu.mx', '2208686'),
    (3, 'Emily Zaray Romero Estrada', 'emily.romero@uabc.edu.mx', '2209578'),
    (4, 'Carlos Ariel Ureta Armenta', 'carlos.ureta@uabc.edu.mx', '2208511'),
    (5, 'Ana Sofía Navarro López', 'ana.navarro@uabc.edu.mx', '2211001'),
    (6, 'Diego Armando Pérez Ruiz', 'diego.perez@uabc.edu.mx', '2211002');


-- 92 asignaciones asesor-materia (N:M).
INSERT INTO `advisor_subjects` (`AdvisorId`, `SubjectId`) VALUES
    (1, 1),
    (1, 2),
    (1, 3),
    (1, 4),
    (1, 5),
    (1, 6),
    (2, 1),
    (2, 3),
    (2, 4),
    (2, 7),
    (3, 1),
    (3, 2),
    (3, 3),
    (3, 7),
    (3, 8),
    (3, 9),
    (4, 1),
    (4, 10),
    (4, 11),
    (4, 12),
    (4, 13),
    (5, 1),
    (5, 2),
    (5, 14),
    (6, 2),
    (6, 3),
    (6, 14),
    (6, 15),
    (6, 16),
    (6, 17),
    (7, 3),
    (7, 7),
    (7, 10),
    (7, 11),
    (7, 16),
    (7, 18),
    (7, 19),
    (7, 20),
    (7, 21),
    (7, 22),
    (8, 2),
    (8, 7),
    (8, 14),
    (8, 15),
    (8, 16),
    (9, 1),
    (9, 3),
    (9, 7),
    (9, 10),
    (9, 11),
    (9, 16),
    (9, 17),
    (9, 23),
    (10, 18),
    (10, 24),
    (10, 25),
    (10, 26),
    (10, 27),
    (11, 18),
    (11, 28),
    (11, 29),
    (11, 30),
    (11, 31),
    (11, 32),
    (11, 33),
    (11, 34),
    (11, 35),
    (12, 18),
    (12, 29),
    (12, 30),
    (12, 36),
    (12, 37),
    (12, 38),
    (12, 39),
    (12, 40),
    (13, 18),
    (13, 30),
    (13, 35),
    (13, 40),
    (13, 41),
    (13, 42),
    (13, 43),
    (14, 18),
    (14, 28),
    (14, 30),
    (14, 31),
    (14, 33),
    (14, 34),
    (14, 36),
    (14, 39),
    (14, 42),
    (14, 44);


-- 104 bloques de horario semanal. DayOfWeek: 0=domingo .. 6=sabado.
INSERT INTO `availabilities` (`Id`, `AdvisorId`, `DayOfWeek`, `StartTime`, `EndTime`, `MaxCapacity`, `Modality`, `Location`) VALUES
    (1, 1, 1, '12:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (2, 1, 3, '12:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (3, 1, 4, '12:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (4, 1, 2, '13:00:00.000000', '17:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (5, 1, 5, '10:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (6, 1, 5, '15:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (7, 2, 1, '08:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (8, 2, 2, '08:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (9, 2, 1, '13:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (10, 2, 2, '13:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (11, 2, 3, '11:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (12, 2, 4, '08:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (13, 2, 4, '14:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (14, 2, 5, '14:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (15, 3, 1, '09:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (16, 3, 2, '09:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (17, 3, 3, '09:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (18, 3, 4, '09:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (19, 3, 5, '12:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (20, 3, 5, '16:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (21, 4, 1, '17:00:00.000000', '21:00:00.000000', 1, 'Virtual', 'Enlace virtual (Meet/Teams)'),
    (22, 4, 2, '17:00:00.000000', '21:00:00.000000', 1, 'Virtual', 'Enlace virtual (Meet/Teams)'),
    (23, 4, 3, '17:00:00.000000', '21:00:00.000000', 1, 'Virtual', 'Enlace virtual (Meet/Teams)'),
    (24, 4, 4, '17:00:00.000000', '21:00:00.000000', 1, 'Virtual', 'Enlace virtual (Meet/Teams)'),
    (25, 4, 5, '17:00:00.000000', '21:00:00.000000', 1, 'Virtual', 'Enlace virtual (Meet/Teams)'),
    (26, 5, 1, '12:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (27, 5, 1, '15:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (28, 5, 2, '10:00:00.000000', '11:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (29, 5, 2, '15:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (30, 5, 3, '14:00:00.000000', '17:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (31, 5, 4, '10:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (32, 5, 4, '16:00:00.000000', '17:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (33, 5, 5, '11:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (34, 6, 1, '11:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (35, 6, 1, '17:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (36, 6, 2, '11:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (37, 6, 4, '11:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (38, 6, 3, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (39, 6, 3, '11:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (40, 6, 3, '15:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (41, 6, 5, '10:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (42, 7, 1, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (43, 7, 2, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (44, 7, 1, '13:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (45, 7, 2, '13:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (46, 7, 1, '17:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (47, 7, 2, '17:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (48, 7, 3, '08:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (49, 7, 4, '08:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (50, 7, 3, '14:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (51, 7, 4, '14:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (52, 7, 5, '14:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (53, 8, 1, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (54, 8, 3, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (55, 8, 1, '15:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (56, 8, 3, '15:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (57, 8, 2, '07:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (58, 8, 4, '14:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (59, 8, 5, '08:00:00.000000', '11:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (60, 8, 5, '14:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (61, 9, 1, '11:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (62, 9, 2, '10:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (63, 9, 4, '10:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (64, 9, 3, '10:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (65, 9, 5, '15:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (66, 10, 1, '08:00:00.000000', '12:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (67, 10, 2, '08:00:00.000000', '12:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (68, 10, 3, '08:00:00.000000', '12:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (69, 10, 4, '09:00:00.000000', '12:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (70, 10, 4, '13:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (71, 10, 5, '07:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (72, 11, 1, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (73, 11, 1, '12:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (74, 11, 1, '14:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (75, 11, 2, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (76, 11, 2, '14:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (77, 11, 2, '16:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (78, 11, 3, '14:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (79, 11, 3, '16:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (80, 11, 4, '14:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (81, 11, 5, '13:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (82, 12, 1, '08:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (83, 12, 2, '08:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (84, 12, 3, '12:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (85, 12, 4, '14:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (86, 12, 5, '11:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (87, 13, 1, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (88, 13, 1, '11:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (89, 13, 1, '15:00:00.000000', '17:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (90, 13, 2, '10:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (91, 13, 3, '12:00:00.000000', '15:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (92, 13, 3, '16:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (93, 13, 5, '10:00:00.000000', '12:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (94, 13, 5, '13:00:00.000000', '16:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (95, 14, 1, '09:00:00.000000', '10:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (96, 14, 1, '12:00:00.000000', '13:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (97, 14, 1, '16:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (98, 14, 2, '09:00:00.000000', '11:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (99, 14, 2, '13:00:00.000000', '14:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (100, 14, 2, '16:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (101, 14, 3, '07:00:00.000000', '08:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (102, 14, 3, '16:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (103, 14, 4, '15:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI'),
    (104, 14, 5, '13:00:00.000000', '18:00:00.000000', 1, 'Presencial', 'Cubículo FCQI');


-- Sesiones de ejemplo.
--
-- El seeder original calculaba estas fechas con DateTime.Today, de modo que
-- siempre caían en la próxima semana. Se reproduce ese comportamiento: la
-- expresión devuelve el primer día de la semana indicado que sea POSTERIOR a
-- hoy (1 a 7 días por delante).
--
--   DAYOFWEEK() de MySQL: 1=domingo .. 7=sábado
--   El literal que se le resta usa la convención de System.DayOfWeek:
--   1=lunes, 3=miércoles, 4=jueves.
INSERT INTO `advisory_sessions`
    (`Id`, `StudentId`, `AdvisorId`, `SubjectId`, `AvailabilityId`, `ScheduledAt`, `Topic`, `Status`) VALUES
    (1, 1, 1,  2,  1,
     TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((1 - DAYOFWEEK(CURDATE()) + 7) % 7) + 1 DAY), '12:00:00'),
     'Límites y continuidad — dudas del parcial 1', 'Pendiente'),
    (2, 3, 1,  2,  1,
     TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((1 - DAYOFWEEK(CURDATE()) + 7) % 7) + 8 DAY), '12:00:00'),
     'Derivadas de funciones trigonométricas', 'Pendiente'),
    (3, 1, 4, 10, 21,
     TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((3 - DAYOFWEEK(CURDATE()) + 7) % 7) + 1 DAY), '17:00:00'),
     'Presente perfecto', 'Confirmada'),
    (4, 3, 4, 10, 21,
     TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((4 - DAYOFWEEK(CURDATE()) + 7) % 7) + 1 DAY), '18:00:00'),
     'Ensayo cancelado', 'Cancelada');

COMMIT;
