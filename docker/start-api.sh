#!/usr/bin/env bash
set -euo pipefail

log() { printf '[fcqi/api] %s\n' "$*"; }

SOCK=/var/run/mysqld/mysqld.sock
DB="${MYSQL_DATABASE:-fcqi_asesorias}"
DB_USER="${MYSQL_USER:-fcqi_app}"
DB_PASS="${MYSQL_PASSWORD:?MYSQL_PASSWORD no definido}"
ROOT_PASS="${MYSQL_ROOT_PASSWORD:?MYSQL_ROOT_PASSWORD no definido}"

# ---------------------------------------------------------------------------
# 1) Esperar a que mysqld acepte conexiones por socket.
#    En el primer arranque root no tiene contraseña (--initialize-insecure);
#    en los siguientes ya usa la contraseña definitiva.
# ---------------------------------------------------------------------------
ROOT_PW=""
READY=0
log "Esperando a MySQL ..."
for _ in $(seq 1 180); do
    if [ -S "$SOCK" ]; then
        if MYSQL_PWD="$ROOT_PASS" mysql --protocol=socket --socket="$SOCK" -uroot \
             -e 'SELECT 1' >/dev/null 2>&1; then
            ROOT_PW="$ROOT_PASS"; READY=1; break
        fi
        if MYSQL_PWD="" mysql --protocol=socket --socket="$SOCK" -uroot \
             -e 'SELECT 1' >/dev/null 2>&1; then
            ROOT_PW=""; READY=1; break
        fi
    fi
    sleep 1
done

if [ "$READY" != 1 ]; then
    log "ERROR: MySQL no respondió tras 180 s. Revisa los logs de mysqld."
    exit 1
fi
log "MySQL disponible."

myroot() { MYSQL_PWD="$ROOT_PW" mysql --protocol=socket --socket="$SOCK" -uroot "$@"; }

# ---------------------------------------------------------------------------
# 2) Base de datos, usuario de aplicación y permisos (idempotente).
# ---------------------------------------------------------------------------
log "Configurando la base de datos '${DB}' y el usuario '${DB_USER}' ..."
myroot <<SQL
ALTER USER 'root'@'localhost' IDENTIFIED BY '${ROOT_PASS}';

CREATE DATABASE IF NOT EXISTS \`${DB}\`
    CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

CREATE USER IF NOT EXISTS '${DB_USER}'@'localhost' IDENTIFIED BY '${DB_PASS}';
CREATE USER IF NOT EXISTS '${DB_USER}'@'127.0.0.1' IDENTIFIED BY '${DB_PASS}';
CREATE USER IF NOT EXISTS '${DB_USER}'@'%'         IDENTIFIED BY '${DB_PASS}';
ALTER  USER '${DB_USER}'@'localhost' IDENTIFIED BY '${DB_PASS}';
ALTER  USER '${DB_USER}'@'127.0.0.1' IDENTIFIED BY '${DB_PASS}';
ALTER  USER '${DB_USER}'@'%'         IDENTIFIED BY '${DB_PASS}';

GRANT ALL PRIVILEGES ON \`${DB}\`.* TO '${DB_USER}'@'localhost';
GRANT ALL PRIVILEGES ON \`${DB}\`.* TO '${DB_USER}'@'127.0.0.1';
GRANT ALL PRIVILEGES ON \`${DB}\`.* TO '${DB_USER}'@'%';
FLUSH PRIVILEGES;
SQL
ROOT_PW="$ROOT_PASS"

# ---------------------------------------------------------------------------
# 3) Tablas de zona horaria de MySQL.
#    Sin ellas CONVERT_TZ() devuelve NULL en silencio. La API convierte entre
#    UTC y America/Tijuana por su cuenta, pero cargarlas deja la base en
#    condiciones de hacer las mismas cuentas desde SQL, en informes o consultas
#    manuales. Es idempotente: solo se cargan la primera vez.
# ---------------------------------------------------------------------------
TZ_COUNT="$(myroot -N -B -e 'SELECT COUNT(*) FROM mysql.time_zone_name' 2>/dev/null || echo 0)"
if [ "${TZ_COUNT:-0}" -lt 100 ]; then
    log "Cargando tablas de zona horaria de MySQL ..."
    if mysql_tzinfo_to_sql /usr/share/zoneinfo 2>/dev/null | myroot mysql 2>/dev/null; then
        log "Zonas horarias cargadas: $(myroot -N -B -e 'SELECT COUNT(*) FROM mysql.time_zone_name')"
    else
        log "AVISO: no se pudieron cargar las zonas horarias; CONVERT_TZ() no funcionará."
    fi
fi

# ---------------------------------------------------------------------------
# 4) Esquema y datos de prueba.
#
#    La aplicación ya NO crea la base: lo hacen db/01-schema.sql (estructura) y
#    db/02-seed.sql (datos), que se copian a /srv/fcqi/db en la imagen. Así la
#    definición de la base es reproducible en cualquier máquina sin recompilar,
#    y un DBA puede editarla sin tocar C#.
#
#    Solo se ejecutan si la base está vacía. Para rehacerla desde cero:
#        docker compose run -e FCQI_DB_RESET=1 ...
#    o simplemente destruir el volumen.
# ---------------------------------------------------------------------------
#    En la etapa `debug` el código fuente se monta desde el host, así que si
#    /src/db existe se usa ese: editar un .sql en el editor y reiniciar el
#    contenedor basta, sin reconstruir la imagen.
DB_DIR=/srv/fcqi/db
if [ -f /src/db/01-schema.sql ] && [ -f /src/db/02-seed.sql ]; then
    DB_DIR=/src/db
fi
SCHEMA_SQL="$DB_DIR/01-schema.sql"
SEED_SQL="$DB_DIR/02-seed.sql"

db_is_ready() {
    myroot -N -B -e \
        "SELECT COUNT(*) FROM information_schema.tables
          WHERE table_schema='${DB}' AND table_name='ciclos_escolares'" 2>/dev/null \
        | grep -q '^1$'
}

if [ "${FCQI_DB_RESET:-0}" = "1" ]; then
    log "FCQI_DB_RESET=1: se reinstalarán el esquema y los datos de prueba."
    NEED_INIT=1
elif db_is_ready; then
    NEED_INIT=0
else
    NEED_INIT=1
fi

if [ "$NEED_INIT" = "1" ]; then
    if [ ! -f "$SCHEMA_SQL" ] || [ ! -f "$SEED_SQL" ]; then
        log "ERROR: faltan $SCHEMA_SQL o $SEED_SQL en la imagen."
        exit 1
    fi

    log "Instalando el esquema desde ${DB_DIR} ..."
    myroot < "$SCHEMA_SQL"

    log "Cargando los datos de prueba (02-seed.sql) ..."
    myroot < "$SEED_SQL"

    log "Base lista: $(myroot -N -B -e "SELECT CONCAT(
            (SELECT COUNT(*) FROM \`${DB}\`.personas), ' personas, ',
            (SELECT COUNT(*) FROM \`${DB}\`.materias), ' materias, ',
            (SELECT COUNT(*) FROM \`${DB}\`.horarios), ' bloques')")"
else
    log "La base ya está inicializada; se conservan sus datos."
fi

# ---------------------------------------------------------------------------
# 4b) Migraciones idempotentes para bases creadas con un esquema anterior
#     (p. ej. un volumen de MySQL que ya existía). Se pueden ejecutar siempre:
#     cada una comprueba qué falta antes de cambiarlo. 001 no está aquí porque
#     convierte el modelo v1 y no es repetible.
# ---------------------------------------------------------------------------
for migration in 002_procesos_bpmn.sql; do
    if [ -f "$DB_DIR/migrations/$migration" ]; then
        log "Aplicando migración $migration ..."
        myroot "$DB" < "$DB_DIR/migrations/$migration"
    fi
done

# ---------------------------------------------------------------------------
# 5) Arrancar la API.
# ---------------------------------------------------------------------------
export ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=${DB};User ID=${DB_USER};Password=${DB_PASS};AllowPublicKeyRetrieval=True;"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:5016}"

log "Arrancando FCQI.Api en ${ASPNETCORE_URLS} (entorno ${ASPNETCORE_ENVIRONMENT:-Production})"

if [ -n "${FCQI_API_CMD:-}" ]; then
    exec bash -c "$FCQI_API_CMD"
fi

cd /srv/fcqi/api
exec dotnet /srv/fcqi/api/FCQI.Api.dll
