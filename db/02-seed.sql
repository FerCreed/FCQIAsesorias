-- =============================================================================
--  Sistema de Asesorías FCQI — DATOS DE PRUEBA
--  MySQL 8.0
--
--  Script 2 de 2. Contiene ÚNICAMENTE inserciones. Requiere que 01-schema.sql
--  se haya ejecutado antes:
--
--      mysql -u root -p < db/01-schema.sql     (estructura)
--      mysql -u root -p < db/02-seed.sql       (este archivo)
--
--  Estos son los datos que antes vivían compilados dentro de la aplicación,
--  en Catalog20262Seeder.cs. Al existir aquí, ese código puede eliminarse:
--  un contenedor de desarrollo levantado en cualquier máquina obtiene la
--  misma base ejecutando los dos scripts, sin recompilar nada.
--
--  DATOS DE PRUEBA, NO DE PRODUCCIÓN. El archivo vacía las tablas antes de
--  insertar, así que es idempotente y puede volver a ejecutarse sobre una
--  base ya poblada. Por lo mismo, NO debe correrse sobre datos reales.
--
--  Los IDs son explícitos para que las llaves foráneas de este archivo no
--  dependan del AUTO_INCREMENT y el resultado sea idéntico en toda máquina.
--
--  Tres puntos marcados REVISAR/PROVISIONAL más abajo requieren confirmación
--  de la coordinación: no había forma de derivarlos del modelo anterior.
-- =============================================================================

SET NAMES utf8mb4;
USE `fcqi_asesorias`;

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE `session_status_history`;
TRUNCATE TABLE `advisory_sessions`;
TRUNCATE TABLE `advisor_subjects`;
TRUNCATE TABLE `availabilities`;
TRUNCATE TABLE `program_subjects`;
TRUNCATE TABLE `subjects`;
TRUNCATE TABLE `admin_profiles`;
TRUNCATE TABLE `student_profiles`;
TRUNCATE TABLE `advisor_profiles`;
TRUNCATE TABLE `people`;
TRUNCATE TABLE `locations`;
TRUNCATE TABLE `academic_terms`;
TRUNCATE TABLE `programs`;
SET FOREIGN_KEY_CHECKS = 1;

START TRANSACTION;

-- 5 programas educativos. En el modelo anterior esto era advisors.Area, texto libre repetido.
INSERT INTO `programs` (`Id`, `Code`, `Name`) VALUES
    (1, 'II', 'Ingeniería Industrial'),
    (2, 'IQ', 'Ingeniería Química'),
    (3, 'IE', 'Ingeniería en Electrónica'),
    (4, 'QFB', 'Química Farmacéutica Biológica'),
    (5, 'QI', 'Químico Industrial');

-- Ciclo escolar. Concepto ausente en el modelo anterior, donde 'FCQI 2026-2'
-- vivia repetido en las 44 filas de subjects.Program.
-- REVISAR: las fechas de inicio y fin son estimadas; ajustar al calendario oficial UABC.
INSERT INTO `academic_terms` (`Id`, `Code`, `Name`, `StartsOn`, `EndsOn`, `IsCurrent`) VALUES
    (1, '2026-2', 'Otoño 2026', '2026-08-10', '2026-12-11', 1);

-- La sede declara su modalidad una sola vez. Antes availabilities guardaba
-- ambas por separado y 'Enlace virtual' implicaba modalidad Virtual.
INSERT INTO `locations` (`Id`, `Name`, `ModalityId`) VALUES
    (1, 'Cubículo FCQI', 1),
    (2, 'Enlace virtual (Meet/Teams)', 2);

-- 21 personas. Identidad unica por correo institucional; los roles
-- cuelgan de aqui. Los nombres se separaron desde el campo FullName del
-- modelo anterior segun la convencion [nombres] [paterno] [materno].
-- REVISAR: la separacion es automatica y algunos casos son ambiguos.
INSERT INTO `people` (`Id`, `Honorific`, `FirstName`, `LastNamePaternal`, `LastNameMaternal`, `Email`) VALUES
    (1, NULL, 'Felipe de Jesús', 'Márquez', 'Vizcarra', 'felipe.marquez63@uabc.edu.mx'),
    (2, NULL, 'Edgar Kenichi', 'Tsuchiya', 'Godínez', 'edgar.tsuchiya@uabc.edu.mx'),
    (3, NULL, 'Eduardo Isaías', 'Mérida', 'Rodríguez', 'eduardo.merida@uabc.edu.mx'),
    (4, NULL, 'Luis Alejandro', 'Flores', 'Díaz', 'alejandro.flores48@uabc.edu.mx'),
    (5, NULL, 'Carlos Enrique', 'Martínez', 'de la Cruz', 'carlos.martinez86@uabc.edu.mx'),
    (6, NULL, 'Dylan Josué', 'Guerrero', 'Luque', 'dylan.guerrero@uabc.edu.mx'),
    (7, NULL, 'Luis Alberto', 'González', 'Rodríguez', 'luis.gonzalez.rodriguez@uabc.edu.mx'),
    (8, NULL, 'Getzamani Guadalupe', 'Solis', 'Gutiérrez', 'getzamani.solis@uabc.edu.mx'),
    (9, NULL, 'Mia Italia', 'Alexandres', 'Carrasco', 'mia.alexandres@uabc.edu.mx'),
    (10, NULL, 'Vladimir Bonifacio', 'Ramírez', 'Martínez', 'v1299027@uabc.edu.mx'),
    (11, NULL, 'Bryan Emilio Rafael', 'Muñoz', 'Campos', 'bryan.munoz@uabc.edu.mx'),
    (12, NULL, 'Jimena', 'Beltrán', 'Zepeda', 'j2207105@uabc.edu.mx'),
    (13, NULL, 'Andrés', 'Bautista', 'Cruz', 'andres.cruz36@uabc.edu.mx'),
    (14, NULL, 'Braulio Reynaldo', 'Angulo', 'Curiel', 'angulo.braulio@uabc.edu.mx'),
    (15, NULL, 'Yesua Fernando', 'Díaz', 'Hernández', 'yesua.diaz@uabc.edu.mx'),
    (16, NULL, 'Juan Carlos', 'Laguna', 'Hernández', 'juan.laguna@uabc.edu.mx'),
    (17, NULL, 'Emily Zaray', 'Romero', 'Estrada', 'emily.romero@uabc.edu.mx'),
    (18, NULL, 'Carlos Ariel', 'Ureta', 'Armenta', 'carlos.ureta@uabc.edu.mx'),
    (19, NULL, 'Ana Sofía', 'Navarro', 'López', 'ana.navarro@uabc.edu.mx'),
    (20, NULL, 'Diego Armando', 'Pérez', 'Ruiz', 'diego.perez@uabc.edu.mx'),
    (21, 'Dra.', 'Lizeth Carolina', 'Aguilar', 'Dodier', 'progasesorias.fcqi@uabc.edu.mx');

-- 14 asesores.
INSERT INTO `advisor_profiles` (`PersonId`, `ProgramId`, `DefaultModalityId`) VALUES
    (1, 3, 1),
    (2, 3, 1),
    (3, 1, 1),
    (4, 1, 2),
    (5, 1, 1),
    (6, 2, 1),
    (7, 2, 1),
    (8, 2, 1),
    (9, 2, 1),
    (10, 5, 1),
    (11, 4, 1),
    (12, 4, 1),
    (13, 4, 1),
    (14, 4, 1);

-- 8 perfiles de alumno, de los cuales DOS son asesores pares: Vladimir
-- (v1299027) y Jimena (j2207105) ya tienen perfil de asesor arriba, y su
-- matricula viene en el propio correo institucional. Son el caso que el
-- modelo anterior no podia representar y la razon de ser de este rediseno:
-- sin estas dos filas, ninguna persona de la base tiene mas de un rol y
-- todo el mecanismo de roles multiples queda sin datos que lo ejerciten.
-- ProgramId va NULL en los demas: el modelo anterior no registraba la
-- carrera del alumno y no se inventa el dato.
INSERT INTO `student_profiles` (`PersonId`, `StudentNumber`, `ProgramId`) VALUES
    (10, '1299027', 5),     -- asesor par: Quimico Industrial
    (12, '2207105', 4),     -- asesora par: Quimica Farmaceutica Biologica
    (15, '2208134', NULL),
    (16, '2208686', NULL),
    (17, '2209578', NULL),
    (18, '2208511', NULL),
    (19, '2211001', NULL),
    (20, '2211002', NULL);

-- Responsable del programa.
INSERT INTO `admin_profiles` (`PersonId`, `Title`) VALUES
    (21, 'Responsable del Programa de Asesorías Académicas');

-- 44 materias. Sin columna Program: una materia como 'Calculo
-- Diferencial' se cursa en varias carreras, y esa relacion N:M vive en program_subjects.
INSERT INTO `subjects` (`Id`, `Code`, `Name`) VALUES
    (1, 'INTRODUCCION-A-LAS-MATEMATICAS-UNIVERSITARIAS', 'Introducción a las Matemáticas Universitarias'),
    (2, 'CALCULO-DIFERENCIAL', 'Cálculo Diferencial'),
    (3, 'CALCULO-INTEGRAL', 'Cálculo Integral'),
    (4, 'CIRCUITOS-DE-CORRIENTE-DIRECTA', 'Circuitos de Corriente Directa'),
    (5, 'CIRCUITOS-DE-CORRIENTE-ALTERNA', 'Circuitos de Corriente Alterna'),
    (6, 'MODELADO-Y-CONTROL', 'Modelado y Control'),
    (7, 'ECUACIONES-DIFERENCIALES', 'Ecuaciones Diferenciales'),
    (8, 'INGENIERIA-ECONOMICA', 'Ingeniería Económica'),
    (9, 'CIRCUITOS-ELECTRICOS', 'Circuitos Eléctricos'),
    (10, 'INGLES-I', 'Inglés I'),
    (11, 'INGLES-II', 'Inglés II'),
    (12, 'PROBABILIDAD-Y-ESTADISTICA', 'Probabilidad y Estadística'),
    (13, 'INVESTIGACION-DE-OPERACIONES-I', 'Investigación de Operaciones I'),
    (14, 'ALGEBRA-SUPERIOR', 'Álgebra Superior'),
    (15, 'BALANCE-DE-MATERIA-Y-ENERGIA', 'Balance de Materia y Energía'),
    (16, 'CINETICA-QUIMICA-Y-CATALISIS', 'Cinética Química y Catálisis'),
    (17, 'REACTORES-HOMOGENEOS-Y-HETEROGENEOS', 'Reactores Homogéneos y Heterogéneos'),
    (18, 'QUIMICA', 'Química'),
    (19, 'ELECTRICIDAD-Y-MAGNETISMO', 'Electricidad y Magnetismo'),
    (20, 'TERMODINAMICA', 'Termodinámica'),
    (21, 'CONTROL-E-INSTRUMENTACION-DE-PROCESOS', 'Control e Instrumentación de Procesos'),
    (22, 'OPERACIONES-DE-SEPARACION', 'Operaciones de Separación'),
    (23, 'OPERACIONES-DE-TRANSFERENCIA-DE-CALOR', 'Operaciones de Transferencia de Calor'),
    (24, 'QUIMICA-ORGANICA-I', 'Química Orgánica I'),
    (25, 'QUIMICA-GENERAL', 'Química General'),
    (26, 'QUIMICA-ORGANICA-II', 'Química Orgánica II'),
    (27, 'BIOQUIMICA', 'Bioquímica'),
    (28, 'MATEMATICAS-BASICAS', 'Matemáticas Básicas'),
    (29, 'QUIMICA-ANALITICA-I', 'Química Analítica I'),
    (30, 'BIOLOGIA', 'Biología'),
    (31, 'BIOLOGIA-MOLECULAR', 'Biología Molecular'),
    (32, 'BIOQUIMICA-CLINICA', 'Bioquímica Clínica'),
    (33, 'BIOQUIMICA-ESTRUCTURAL', 'Bioquímica Estructural'),
    (34, 'BIOQUIMICA-METABOLICA', 'Bioquímica Metabólica'),
    (35, 'FARMACOCINETICA', 'Farmacocinética'),
    (36, 'FISICA', 'Física'),
    (37, 'BIOFARMACIA', 'Biofarmacia'),
    (38, 'FARMACOLOGIA', 'Farmacología'),
    (39, 'BIOLOGIA-CELULAR', 'Biología Celular'),
    (40, 'ANALISIS-INSTRUMENTAL-I', 'Análisis Instrumental I'),
    (41, 'QUIMICA-ANALITICA-II', 'Química Analítica II'),
    (42, 'INMUNOLOGIA', 'Inmunología'),
    (43, 'ANATOMIA-Y-FISIOLOGIA', 'Anatomía y Fisiología'),
    (44, 'MATEMATICAS-AVANZADAS', 'Matemáticas Avanzadas');

-- 57 vinculos carrera-materia.
-- PROVISIONAL: derivados de que un asesor adscrito a una carrera imparta la
-- materia. Es una inferencia razonable para datos de prueba, NO el plan de
-- estudios oficial. Debe reemplazarlo la coordinacion.
INSERT INTO `program_subjects` (`ProgramId`, `SubjectId`) VALUES
    (1, 1),
    (1, 2),
    (1, 3),
    (1, 7),
    (1, 8),
    (1, 9),
    (1, 10),
    (1, 11),
    (1, 12),
    (1, 13),
    (1, 14),
    (2, 1),
    (2, 2),
    (2, 3),
    (2, 7),
    (2, 10),
    (2, 11),
    (2, 14),
    (2, 15),
    (2, 16),
    (2, 17),
    (2, 18),
    (2, 19),
    (2, 20),
    (2, 21),
    (2, 22),
    (2, 23),
    (3, 1),
    (3, 2),
    (3, 3),
    (3, 4),
    (3, 5),
    (3, 6),
    (3, 7),
    (4, 18),
    (4, 28),
    (4, 29),
    (4, 30),
    (4, 31),
    (4, 32),
    (4, 33),
    (4, 34),
    (4, 35),
    (4, 36),
    (4, 37),
    (4, 38),
    (4, 39),
    (4, 40),
    (4, 41),
    (4, 42),
    (4, 43),
    (4, 44),
    (5, 18),
    (5, 24),
    (5, 25),
    (5, 26),
    (5, 27);

-- 92 asignaciones asesor-materia, ahora acotadas al ciclo.
INSERT INTO `advisor_subjects` (`TermId`, `AdvisorId`, `SubjectId`) VALUES
    (1, 1, 1),
    (1, 1, 2),
    (1, 1, 3),
    (1, 1, 4),
    (1, 1, 5),
    (1, 1, 6),
    (1, 2, 1),
    (1, 2, 3),
    (1, 2, 4),
    (1, 2, 7),
    (1, 3, 1),
    (1, 3, 2),
    (1, 3, 3),
    (1, 3, 7),
    (1, 3, 8),
    (1, 3, 9),
    (1, 4, 1),
    (1, 4, 10),
    (1, 4, 11),
    (1, 4, 12),
    (1, 4, 13),
    (1, 5, 1),
    (1, 5, 2),
    (1, 5, 14),
    (1, 6, 2),
    (1, 6, 3),
    (1, 6, 14),
    (1, 6, 15),
    (1, 6, 16),
    (1, 6, 17),
    (1, 7, 3),
    (1, 7, 7),
    (1, 7, 10),
    (1, 7, 11),
    (1, 7, 16),
    (1, 7, 18),
    (1, 7, 19),
    (1, 7, 20),
    (1, 7, 21),
    (1, 7, 22),
    (1, 8, 2),
    (1, 8, 7),
    (1, 8, 14),
    (1, 8, 15),
    (1, 8, 16),
    (1, 9, 1),
    (1, 9, 3),
    (1, 9, 7),
    (1, 9, 10),
    (1, 9, 11),
    (1, 9, 16),
    (1, 9, 17),
    (1, 9, 23),
    (1, 10, 18),
    (1, 10, 24),
    (1, 10, 25),
    (1, 10, 26),
    (1, 10, 27),
    (1, 11, 18),
    (1, 11, 28),
    (1, 11, 29),
    (1, 11, 30),
    (1, 11, 31),
    (1, 11, 32),
    (1, 11, 33),
    (1, 11, 34),
    (1, 11, 35),
    (1, 12, 18),
    (1, 12, 29),
    (1, 12, 30),
    (1, 12, 36),
    (1, 12, 37),
    (1, 12, 38),
    (1, 12, 39),
    (1, 12, 40),
    (1, 13, 18),
    (1, 13, 30),
    (1, 13, 35),
    (1, 13, 40),
    (1, 13, 41),
    (1, 13, 42),
    (1, 13, 43),
    (1, 14, 18),
    (1, 14, 28),
    (1, 14, 30),
    (1, 14, 31),
    (1, 14, 33),
    (1, 14, 34),
    (1, 14, 36),
    (1, 14, 39),
    (1, 14, 42),
    (1, 14, 44);

-- 104 bloques de horario. DayOfWeek: 0=domingo .. 6=sabado.
-- Modality desaparece: la determina LocationId.
INSERT INTO `availabilities` (`Id`, `TermId`, `AdvisorId`, `DayOfWeek`, `StartTime`, `EndTime`, `MaxCapacity`, `LocationId`) VALUES
    (1, 1, 1, 1, '12:00:00', '16:00:00', 1, 1),
    (2, 1, 1, 3, '12:00:00', '16:00:00', 1, 1),
    (3, 1, 1, 4, '12:00:00', '16:00:00', 1, 1),
    (4, 1, 1, 2, '13:00:00', '17:00:00', 1, 1),
    (5, 1, 1, 5, '10:00:00', '13:00:00', 1, 1),
    (6, 1, 1, 5, '15:00:00', '16:00:00', 1, 1),
    (7, 1, 2, 1, '08:00:00', '10:00:00', 1, 1),
    (8, 1, 2, 2, '08:00:00', '10:00:00', 1, 1),
    (9, 1, 2, 1, '13:00:00', '15:00:00', 1, 1),
    (10, 1, 2, 2, '13:00:00', '15:00:00', 1, 1),
    (11, 1, 2, 3, '11:00:00', '15:00:00', 1, 1),
    (12, 1, 2, 4, '08:00:00', '10:00:00', 1, 1),
    (13, 1, 2, 4, '14:00:00', '16:00:00', 1, 1),
    (14, 1, 2, 5, '14:00:00', '18:00:00', 1, 1),
    (15, 1, 3, 1, '09:00:00', '13:00:00', 1, 1),
    (16, 1, 3, 2, '09:00:00', '13:00:00', 1, 1),
    (17, 1, 3, 3, '09:00:00', '13:00:00', 1, 1),
    (18, 1, 3, 4, '09:00:00', '13:00:00', 1, 1),
    (19, 1, 3, 5, '12:00:00', '14:00:00', 1, 1),
    (20, 1, 3, 5, '16:00:00', '18:00:00', 1, 1),
    (21, 1, 4, 1, '17:00:00', '21:00:00', 1, 2),
    (22, 1, 4, 2, '17:00:00', '21:00:00', 1, 2),
    (23, 1, 4, 3, '17:00:00', '21:00:00', 1, 2),
    (24, 1, 4, 4, '17:00:00', '21:00:00', 1, 2),
    (25, 1, 4, 5, '17:00:00', '21:00:00', 1, 2),
    (26, 1, 5, 1, '12:00:00', '14:00:00', 1, 1),
    (27, 1, 5, 1, '15:00:00', '16:00:00', 1, 1),
    (28, 1, 5, 2, '10:00:00', '11:00:00', 1, 1),
    (29, 1, 5, 2, '15:00:00', '16:00:00', 1, 1),
    (30, 1, 5, 3, '14:00:00', '17:00:00', 1, 1),
    (31, 1, 5, 4, '10:00:00', '14:00:00', 1, 1),
    (32, 1, 5, 4, '16:00:00', '17:00:00', 1, 1),
    (33, 1, 5, 5, '11:00:00', '16:00:00', 1, 1),
    (34, 1, 6, 1, '11:00:00', '14:00:00', 1, 1),
    (35, 1, 6, 1, '17:00:00', '18:00:00', 1, 1),
    (36, 1, 6, 2, '11:00:00', '14:00:00', 1, 1),
    (37, 1, 6, 4, '11:00:00', '14:00:00', 1, 1),
    (38, 1, 6, 3, '09:00:00', '10:00:00', 1, 1),
    (39, 1, 6, 3, '11:00:00', '14:00:00', 1, 1),
    (40, 1, 6, 3, '15:00:00', '16:00:00', 1, 1),
    (41, 1, 6, 5, '10:00:00', '15:00:00', 1, 1),
    (42, 1, 7, 1, '09:00:00', '10:00:00', 1, 1),
    (43, 1, 7, 2, '09:00:00', '10:00:00', 1, 1),
    (44, 1, 7, 1, '13:00:00', '15:00:00', 1, 1),
    (45, 1, 7, 2, '13:00:00', '15:00:00', 1, 1),
    (46, 1, 7, 1, '17:00:00', '18:00:00', 1, 1),
    (47, 1, 7, 2, '17:00:00', '18:00:00', 1, 1),
    (48, 1, 7, 3, '08:00:00', '10:00:00', 1, 1),
    (49, 1, 7, 4, '08:00:00', '10:00:00', 1, 1),
    (50, 1, 7, 3, '14:00:00', '16:00:00', 1, 1),
    (51, 1, 7, 4, '14:00:00', '16:00:00', 1, 1),
    (52, 1, 7, 5, '14:00:00', '18:00:00', 1, 1),
    (53, 1, 8, 1, '09:00:00', '10:00:00', 1, 1),
    (54, 1, 8, 3, '09:00:00', '10:00:00', 1, 1),
    (55, 1, 8, 1, '15:00:00', '18:00:00', 1, 1),
    (56, 1, 8, 3, '15:00:00', '18:00:00', 1, 1),
    (57, 1, 8, 2, '07:00:00', '10:00:00', 1, 1),
    (58, 1, 8, 4, '14:00:00', '18:00:00', 1, 1),
    (59, 1, 8, 5, '08:00:00', '11:00:00', 1, 1),
    (60, 1, 8, 5, '14:00:00', '16:00:00', 1, 1),
    (61, 1, 9, 1, '11:00:00', '14:00:00', 1, 1),
    (62, 1, 9, 2, '10:00:00', '15:00:00', 1, 1),
    (63, 1, 9, 4, '10:00:00', '15:00:00', 1, 1),
    (64, 1, 9, 3, '10:00:00', '14:00:00', 1, 1),
    (65, 1, 9, 5, '15:00:00', '18:00:00', 1, 1),
    (66, 1, 10, 1, '08:00:00', '12:00:00', 1, 1),
    (67, 1, 10, 2, '08:00:00', '12:00:00', 1, 1),
    (68, 1, 10, 3, '08:00:00', '12:00:00', 1, 1),
    (69, 1, 10, 4, '09:00:00', '12:00:00', 1, 1),
    (70, 1, 10, 4, '13:00:00', '15:00:00', 1, 1),
    (71, 1, 10, 5, '07:00:00', '10:00:00', 1, 1),
    (72, 1, 11, 1, '09:00:00', '10:00:00', 1, 1),
    (73, 1, 11, 1, '12:00:00', '13:00:00', 1, 1),
    (74, 1, 11, 1, '14:00:00', '16:00:00', 1, 1),
    (75, 1, 11, 2, '09:00:00', '10:00:00', 1, 1),
    (76, 1, 11, 2, '14:00:00', '15:00:00', 1, 1),
    (77, 1, 11, 2, '16:00:00', '18:00:00', 1, 1),
    (78, 1, 11, 3, '14:00:00', '15:00:00', 1, 1),
    (79, 1, 11, 3, '16:00:00', '18:00:00', 1, 1),
    (80, 1, 11, 4, '14:00:00', '18:00:00', 1, 1),
    (81, 1, 11, 5, '13:00:00', '18:00:00', 1, 1),
    (82, 1, 12, 1, '08:00:00', '13:00:00', 1, 1),
    (83, 1, 12, 2, '08:00:00', '13:00:00', 1, 1),
    (84, 1, 12, 3, '12:00:00', '16:00:00', 1, 1),
    (85, 1, 12, 4, '14:00:00', '15:00:00', 1, 1),
    (86, 1, 12, 5, '11:00:00', '16:00:00', 1, 1),
    (87, 1, 13, 1, '09:00:00', '10:00:00', 1, 1),
    (88, 1, 13, 1, '11:00:00', '13:00:00', 1, 1),
    (89, 1, 13, 1, '15:00:00', '17:00:00', 1, 1),
    (90, 1, 13, 2, '10:00:00', '15:00:00', 1, 1),
    (91, 1, 13, 3, '12:00:00', '15:00:00', 1, 1),
    (92, 1, 13, 3, '16:00:00', '18:00:00', 1, 1),
    (93, 1, 13, 5, '10:00:00', '12:00:00', 1, 1),
    (94, 1, 13, 5, '13:00:00', '16:00:00', 1, 1),
    (95, 1, 14, 1, '09:00:00', '10:00:00', 1, 1),
    (96, 1, 14, 1, '12:00:00', '13:00:00', 1, 1),
    (97, 1, 14, 1, '16:00:00', '18:00:00', 1, 1),
    (98, 1, 14, 2, '09:00:00', '11:00:00', 1, 1),
    (99, 1, 14, 2, '13:00:00', '14:00:00', 1, 1),
    (100, 1, 14, 2, '16:00:00', '18:00:00', 1, 1),
    (101, 1, 14, 3, '07:00:00', '08:00:00', 1, 1),
    (102, 1, 14, 3, '16:00:00', '18:00:00', 1, 1),
    (103, 1, 14, 4, '15:00:00', '18:00:00', 1, 1),
    (104, 1, 14, 5, '13:00:00', '18:00:00', 1, 1);

-- Sesiones de ejemplo.
--
-- Dos conversiones respecto al modelo anterior:
--
-- 1. Las fechas se calculaban en C# con DateTime.Today, de modo que siempre
--    caían en la próxima semana. Se reproduce aquí: la expresión devuelve el
--    primer día de la semana indicado POSTERIOR a hoy.
--      DAYOFWEEK() de MySQL: 1=domingo .. 7=sábado
--      El literal que se resta usa System.DayOfWeek: 1=lunes, 3=miércoles, 4=jueves.
--
-- 2. ScheduledAt ahora es UTC; antes era hora local sin zona. Baja California
--    sigue el horario de verano de EE.UU.: UTC-7 del 2.º domingo de marzo al
--    1.er domingo de noviembre, UTC-8 el resto del año. Se calcula abajo en
--    lugar de usar CONVERT_TZ, que devuelve NULL cuando las tablas de zona
--    horaria de MySQL no están cargadas — que es el caso en este contenedor.
SET @y          := YEAR(CURDATE());
SET @mar1       := STR_TO_DATE(CONCAT(@y, '-03-01'), '%Y-%m-%d');
SET @nov1       := STR_TO_DATE(CONCAT(@y, '-11-01'), '%Y-%m-%d');
SET @dst_start  := @mar1 + INTERVAL ((8 - DAYOFWEEK(@mar1)) % 7) DAY + INTERVAL 7 DAY;
SET @dst_end    := @nov1 + INTERVAL ((8 - DAYOFWEEK(@nov1)) % 7) DAY;
SET @utc_offset := IF(CURDATE() >= @dst_start AND CURDATE() < @dst_end, 7, 8);

INSERT INTO `advisory_sessions` (`Id`, `TermId`, `AvailabilityId`, `AdvisorId`, `StudentId`, `SubjectId`, `ScheduledAt`, `SeatNumber`, `StatusId`, `Topic`) VALUES
    (1, 1, 1, 1, 15, 2, TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((1 - DAYOFWEEK(CURDATE()) + 7) % 7) + 1 DAY), '12:00:00') + INTERVAL @utc_offset HOUR, 1, 1, 'Límites y continuidad — dudas del parcial 1'),
    (2, 1, 1, 1, 17, 2, TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((1 - DAYOFWEEK(CURDATE()) + 7) % 7) + 8 DAY), '12:00:00') + INTERVAL @utc_offset HOUR, 1, 1, 'Derivadas de funciones trigonométricas'),
    (3, 1, 21, 4, 15, 10, TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((3 - DAYOFWEEK(CURDATE()) + 7) % 7) + 1 DAY), '17:00:00') + INTERVAL @utc_offset HOUR, 1, 2, 'Presente perfecto'),
    (4, 1, 21, 4, 17, 10, TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((4 - DAYOFWEEK(CURDATE()) + 7) % 7) + 1 DAY), '18:00:00') + INTERVAL @utc_offset HOUR, 1, 3, 'Ensayo cancelado'),
    -- Jimena (12) es asesora, y aquí aparece como ALUMNA pidiendo asesoría a
    -- otro tutor. Es la fila que demuestra que los dos roles conviven.
    (5, 1, 2, 1, 12, 2, TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((3 - DAYOFWEEK(CURDATE()) + 7) % 7) + 1 DAY), '12:00:00') + INTERVAL @utc_offset HOUR, 1, 1, 'Regla de la cadena — voy como alumna'),
    -- Y aquí la MISMA Jimena del otro lado del mostrador: alguien le pide
    -- asesoría a ella. Con las dos filas, al cambiar de rol en la interfaz
    -- las dos bandejas tienen contenido y el cambio se ve de inmediato.
    (6, 1, 85, 12, 16, 30, TIMESTAMP(DATE_ADD(CURDATE(), INTERVAL ((4 - DAYOFWEEK(CURDATE()) + 7) % 7) + 1 DAY), '14:00:00') + INTERVAL @utc_offset HOUR, 1, 1, 'Mitosis y meiosis — le toca atender');

COMMIT;

-- El historial de estados (session_status_history) NO se inserta aquí: los
-- triggers de la sección 7 del esquema lo escriben solos al crear cada sesión.
