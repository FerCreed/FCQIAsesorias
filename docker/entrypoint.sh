#!/usr/bin/env bash
set -euo pipefail

log() { printf '[fcqi] %s\n' "$*"; }

DATA_DIR=/var/lib/mysql
SECRETS_FILE="$DATA_DIR/.fcqi-credentials"

install -d -o mysql -g mysql -m 0755 /var/run/mysqld /var/log/mysql
install -d -o mysql -g mysql -m 0750 "$DATA_DIR" /var/lib/mysql-files
install -d -m 0755 /run

# ---------------------------------------------------------------------------
# 1) Credenciales.
#    Prioridad: variable de entorno > credencial guardada en el volumen >
#    contraseña aleatoria nueva.
# ---------------------------------------------------------------------------
# Nota: no se usa `| head -c`, porque head cierra la tubería y el productor
# muere con SIGPIPE, que `set -o pipefail` convierte en fallo del script.
# `od -N` lee una cantidad fija de bytes y termina por sí solo.
gen_pw() { od -An -tx1 -N 18 /dev/urandom | tr -d ' \n'; }

SAVED_ROOT=""
SAVED_APP=""
SAVED_JWT=""
if [ -f "$SECRETS_FILE" ]; then
    # shellcheck disable=SC1090
    . "$SECRETS_FILE"
fi

: "${MYSQL_DATABASE:=fcqi_asesorias}"
: "${MYSQL_USER:=fcqi_app}"

GENERATED=0
if [ -z "${MYSQL_ROOT_PASSWORD:-}" ]; then
    if [ -n "$SAVED_ROOT" ]; then MYSQL_ROOT_PASSWORD="$SAVED_ROOT"
    else MYSQL_ROOT_PASSWORD="$(gen_pw)"; GENERATED=1; fi
fi
if [ -z "${MYSQL_PASSWORD:-}" ]; then
    if [ -n "$SAVED_APP" ]; then MYSQL_PASSWORD="$SAVED_APP"
    else MYSQL_PASSWORD="$(gen_pw)"; GENERATED=1; fi
fi

# Las contraseñas se insertan literalmente en sentencias SQL: se restringe el
# juego de caracteres en lugar de intentar escaparlas.
for value in "$MYSQL_ROOT_PASSWORD" "$MYSQL_PASSWORD"; do
    case "$value" in
        *[!A-Za-z0-9_.:@#%+-]*)
            log "ERROR: las contraseñas de MySQL solo admiten A-Z a-z 0-9 y _.:@#%+-"
            exit 1
            ;;
    esac
done

# ---------------------------------------------------------------------------
# 1b) Clave de firma de los JWT.
#     src/FCQI.Api/appsettings.json trae una clave de ejemplo
#     ("FCQI-dev-jwt-key-change-me-32chars!") que está en el repositorio y, por
#     tanto, no sirve en un servidor real: cualquiera podría firmar tokens.
#     Si no se define Authentication__Jwt__Key, se genera una y se guarda en el
#     volumen para que las sesiones sobrevivan a los reinicios.
# ---------------------------------------------------------------------------
if [ -z "${Authentication__Jwt__Key:-}" ]; then
    if [ -n "$SAVED_JWT" ]; then
        Authentication__Jwt__Key="$SAVED_JWT"
    else
        Authentication__Jwt__Key="$(gen_pw)$(gen_pw)"
        GENERATED=1
    fi
fi

export MYSQL_DATABASE MYSQL_USER MYSQL_PASSWORD MYSQL_ROOT_PASSWORD
export Authentication__Jwt__Key

# ---------------------------------------------------------------------------
# 2) Ajustes de MySQL que dependen del arranque (no del build).
# ---------------------------------------------------------------------------
for dir in /etc/mysql/conf.d /etc/mysql/mysql.conf.d; do
    mkdir -p "$dir"
    cat > "$dir/zzz-fcqi-runtime.cnf" <<CNF
[mysqld]
bind-address            = ${MYSQL_BIND_ADDRESS:-127.0.0.1}
innodb_buffer_pool_size = ${MYSQL_INNODB_BUFFER_POOL_SIZE:-256M}
CNF
done

# ---------------------------------------------------------------------------
# 3) Inicialización del data directory (solo la primera vez / volumen vacío).
# ---------------------------------------------------------------------------
if [ ! -d "$DATA_DIR/mysql" ]; then
    log "Volumen de datos vacío: inicializando MySQL en $DATA_DIR ..."
    mysqld --defaults-file=/etc/mysql/my.cnf \
           --initialize-insecure --user=mysql --datadir="$DATA_DIR"
    log "Data directory inicializado."
fi

umask 077
cat > "$SECRETS_FILE" <<CRED
SAVED_ROOT='${MYSQL_ROOT_PASSWORD}'
SAVED_APP='${MYSQL_PASSWORD}'
SAVED_JWT='${Authentication__Jwt__Key}'
CRED
chown mysql:mysql "$SECRETS_FILE"
umask 022

echo
log "==================== Sistema de Asesorías FCQI ===================="
log " Base de datos : ${MYSQL_DATABASE}"
log " Usuario app   : ${MYSQL_USER}"
log " Entorno .NET  : ${ASPNETCORE_ENVIRONMENT:-Production}"
if [ -n "${Authentication__Google__ClientId:-}" ]; then
    log " Google OAuth  : configurado (solo correos @uabc.edu.mx)"
else
    log " Google OAuth  : sin configurar → login de demostración por perfil"
    log "                 (defínelo con -e Authentication__Google__ClientId=...)"
fi
if [ "$GENERATED" = 1 ]; then
    log " AVISO: se generaron contraseñas aleatorias para este volumen."
    log "   root       : ${MYSQL_ROOT_PASSWORD}"
    log "   ${MYSQL_USER} : ${MYSQL_PASSWORD}"
    log "   Clave JWT generada automáticamente (no se muestra)."
    log "   Se guardaron en ${SECRETS_FILE} (dentro del volumen de datos)."
    log "   Para fijarlas tú: -e MYSQL_ROOT_PASSWORD=... -e MYSQL_PASSWORD=..."
    log "                     -e Authentication__Jwt__Key=..."
fi
log "=================================================================="
echo

exec /usr/bin/supervisord -c /etc/supervisor/fcqi-supervisord.conf
