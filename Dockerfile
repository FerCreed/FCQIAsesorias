# syntax=docker/dockerfile:1.7
# =============================================================================
#  Sistema de Asesorías FCQI — imagen "todo en uno"
#  ---------------------------------------------------------------------------
#  Destino : servidor académico con Ubuntu Server 24.04 LTS (amd64)
#  Contiene: Ubuntu 24.04 + .NET 8 (SDK para build/debug, runtime ASP.NET Core
#            para producción) + MySQL 8.4 LTS + nginx + supervisor.
#
#  Etapas (docker build --target <etapa>):
#    build    → compila API, publica el frontend Avalonia WASM y genera el
#               script SQL idempotente de las migraciones EF Core.
#    debug    → SDK + código fuente + MySQL, arranca la API con `dotnet watch`
#               y deja vsdbg listo para adjuntar un depurador.
#    runtime  → (por defecto) imagen final de ejecución/despliegue.
#
#  MULTIARQUITECTURA (amd64 + arm64):
#    La imagen se construye de forma nativa en Apple Silicon (arm64), en un PC
#    con Windows (amd64) y en el servidor académico (amd64). El único
#    componente que no es multiarquitectura es el paquete de MySQL:
#
#      amd64 → repositorio oficial de Oracle  → MySQL 8.4 LTS
#      arm64 → feed nativo de Ubuntu 24.04    → MySQL 8.0 (soporte de Canonical
#                                                hasta 2029; Oracle no publica
#                                                paquetes arm64 para "noble")
#
#    El esquema y las consultas son idénticos en ambos casos porque el proyecto
#    fija MySqlServerVersion(8,0,36) en FCQI.Infrastructure/DependencyInjection.cs,
#    de modo que EF Core genera SQL compatible con 8.0 en las dos versiones.
#
#  Uso rápido (o usa docker-compose.yml / los botones de Rider):
#    docker build -t fcqi-asesorias --build-arg API_BASE_URL=http://localhost:8080 .
#    docker run -d --name fcqi -p 8080:80 -v fcqi-mysql:/var/lib/mysql fcqi-asesorias
#    → http://localhost:8080
# =============================================================================

# --- Argumentos globales (usables en las líneas FROM) ------------------------
ARG UBUNTU_TAG=24.04
ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0-noble


# =============================================================================
#  ETAPA 0 — assets
#  Scripts y archivos de configuración en un solo lugar, para que las etapas
#  `debug` y `runtime` compartan exactamente la misma configuración.
# =============================================================================
FROM scratch AS assets

# -----------------------------------------------------------------------------
# Instalación de MySQL 8.4 LTS, nginx y supervisor sobre Ubuntu 24.04.
# -----------------------------------------------------------------------------
COPY <<'SH' /assets/install-services.sh
#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

MYSQL_APT_COMPONENT="${MYSQL_APT_COMPONENT:-mysql-8.4-lts}"
MYSQL_GPG_KEY_URL="${MYSQL_GPG_KEY_URL:-https://repo.mysql.com/RPM-GPG-KEY-mysql-2025}"

ARCH="$(dpkg --print-architecture)"

# Impide que los postinst de los .deb intenten arrancar servicios durante el build.
echo 'exit 101' > /usr/sbin/policy-rc.d
chmod +x /usr/sbin/policy-rc.d

apt-get update
apt-get install -y --no-install-recommends ca-certificates curl gnupg tzdata

# ---------------------------------------------------------------------------
# Origen de MySQL según la arquitectura del procesador.
#
#   amd64 (PC con Windows y servidor Ubuntu):
#       repositorio APT oficial de Oracle -> MySQL 8.4 LTS.
#
#   arm64 (Apple Silicon):
#       Oracle no publica paquetes .deb arm64 para Ubuntu "noble"
#       (dists/noble/InRelease declara "Architectures: i386 amd64"),
#       así que se usa MySQL 8.0 del feed nativo de Ubuntu 24.04, mantenido
#       por Canonical durante toda la vida de la LTS.
#
# En ambos casos el esquema es idéntico: el proyecto fija
# MySqlServerVersion(8,0,36), por lo que EF Core / Pomelo generan SQL
# compatible con 8.0 y por tanto válido también en 8.4.
# ---------------------------------------------------------------------------
case "$ARCH" in
    amd64)
        install -d -m 0755 /etc/apt/keyrings
        curl -fsSL "$MYSQL_GPG_KEY_URL" | gpg --dearmor -o /etc/apt/keyrings/mysql.gpg
        . /etc/os-release
        echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/mysql.gpg] http://repo.mysql.com/apt/ubuntu/ ${VERSION_CODENAME} ${MYSQL_APT_COMPONENT}" \
            > /etc/apt/sources.list.d/mysql.list
        MYSQL_PACKAGES="mysql-community-server mysql-community-client"
        ;;
    arm64)
        MYSQL_PACKAGES="mysql-server-8.0 mysql-client-8.0"
        ;;
    *)
        echo "ERROR: arquitectura no soportada: '${ARCH}'." >&2
        echo "       Esta imagen admite linux/amd64 y linux/arm64."  >&2
        exit 1
        ;;
esac
echo "Arquitectura ${ARCH}: instalando ${MYSQL_PACKAGES}"

apt-get update
# shellcheck disable=SC2086
apt-get install -y --no-install-recommends \
        $MYSQL_PACKAGES \
        nginx \
        supervisor \
        locales \
        procps iproute2 less

# Locale UTF-8: los datos del proyecto están en español (materias con acentos).
# Se deja C.UTF-8 como predeterminado por ser el más neutro para .NET; es_MX
# queda generado por si se prefiere (ENV LANG=es_MX.UTF-8).
sed -i 's/^# *es_MX.UTF-8 UTF-8/es_MX.UTF-8 UTF-8/' /etc/locale.gen
locale-gen es_MX.UTF-8 > /dev/null

# El .deb deja un data directory ya inicializado; lo vaciamos para que el
# arranque del contenedor lo inicialice (o use el volumen que monte el usuario).
rm -rf /var/lib/mysql
install -d -o mysql -g mysql -m 0750 /var/lib/mysql /var/lib/mysql-files
install -d -o mysql -g mysql -m 0755 /var/run/mysqld /var/log/mysql

# El sitio por defecto de nginx también escucha en :80 y chocaría con el nuestro.
rm -f /etc/nginx/sites-enabled/default

rm -f /usr/sbin/policy-rc.d
rm -rf /var/lib/apt/lists/*

mysqld --version
nginx -v
SH

# -----------------------------------------------------------------------------
# Configuración del servidor MySQL.
# -----------------------------------------------------------------------------
COPY <<'CNF' /assets/mysqld-fcqi.cnf
# Configuración de MySQL para el contenedor del Sistema de Asesorías FCQI.
#
# /etc/mysql/my.cnf incluye DOS directorios, en este orden:
#     !includedir /etc/mysql/conf.d/
#     !includedir /etc/mysql/mysql.conf.d/
# El paquete de Ubuntu (arm64) deja su propio mysqld.cnf en el segundo, así que
# este archivo se copia a AMBOS con un nombre que ordena al final ("zz-"),
# garantizando que gana sin importar el origen del paquete.
[mysqld]
user                    = mysql
datadir                 = /var/lib/mysql
socket                  = /var/run/mysqld/mysqld.sock
pid-file                = /var/run/mysqld/mysqld.pid
port                    = 3306

# Los errores van a stderr para que aparezcan en `docker logs`.
log-error               = stderr

# El plugin X (puerto 33060) no se usa en este proyecto.
mysqlx                  = 0
skip-name-resolve

# EF Core / Pomelo generan el esquema con utf8mb4 (ver la migración InitialCreate).
character-set-server    = utf8mb4
collation-server        = utf8mb4_0900_ai_ci

max_connections         = 100
innodb_flush_method     = O_DIRECT

[client]
socket                  = /var/run/mysqld/mysqld.sock
default-character-set   = utf8mb4
CNF

# -----------------------------------------------------------------------------
# nginx: sirve el frontend Avalonia WASM y hace proxy inverso hacia Kestrel.
# Así el frontend y la API quedan en el MISMO origen y no hay problemas de CORS.
# -----------------------------------------------------------------------------
COPY <<'NGINX' /assets/fcqi.nginx.conf
server {
    listen      80 default_server;
    listen      [::]:80 default_server;
    server_name _;

    root  /srv/fcqi/web;
    index index.html;

    charset              utf-8;
    client_max_body_size 25m;

    access_log /var/log/nginx/fcqi.access.log;
    error_log  /var/log/nginx/fcqi.error.log warn;

    gzip             on;
    gzip_min_length  1024;
    gzip_proxied     any;
    gzip_types       text/plain text/css application/javascript application/json
                     application/wasm application/octet-stream image/svg+xml;

    # --- Sonda de salud del contenedor -------------------------------------
    location = /healthz {
        access_log off;
        default_type text/plain;
        return 200 "ok\n";
    }

    # --- API REST (ASP.NET Core / Kestrel en 127.0.0.1:5016) ---------------
    location /api/ {
        proxy_pass         http://127.0.0.1:5016;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   Connection        "";
        proxy_read_timeout 120s;
    }

    # --- Swagger (solo responde si ASPNETCORE_ENVIRONMENT=Development) ------
    location /swagger {
        proxy_pass         http://127.0.0.1:5016;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   Connection        "";
    }

    # --- Runtime de .NET WebAssembly ---------------------------------------
    # Se declaran los MIME explícitamente: si el navegador no recibe
    # "application/wasm" para los .wasm, la instanciación en streaming falla.
    location /_framework/ {
        types {
            application/wasm         wasm;
            application/javascript   js mjs;
            application/json         json;
            application/octet-stream dat blat dll pdb webcil;
        }
        default_type application/octet-stream;
        add_header   Cache-Control "public, max-age=3600";
        try_files    $uri =404;
    }

    # --- Frontend Avalonia (SPA) -------------------------------------------
    location / {
        add_header Cache-Control "no-cache";
        try_files  $uri $uri/ /index.html;
    }
}
NGINX

# -----------------------------------------------------------------------------
# supervisor: orquesta los tres procesos del contenedor (mysqld, API, nginx).
# -----------------------------------------------------------------------------
COPY <<'SUP' /assets/supervisord.conf
[supervisord]
nodaemon        = true
user            = root
loglevel        = info
logfile         = /dev/null
logfile_maxbytes= 0
pidfile         = /run/supervisord.pid

[unix_http_server]
file  = /run/supervisor.sock
chmod = 0700

[rpcinterface:supervisor]
supervisor.rpcinterface_factory = supervisor.rpcinterface:make_main_rpcinterface

[supervisorctl]
serverurl = unix:///run/supervisor.sock

# --- 1) Base de datos ---------------------------------------------------------
[program:mysqld]
command          = /usr/sbin/mysqld --defaults-file=/etc/mysql/my.cnf
priority         = 10
autostart        = true
autorestart      = true
startsecs        = 5
stopsignal       = TERM
stopwaitsecs     = 60
stdout_logfile   = /dev/fd/1
stdout_logfile_maxbytes = 0
redirect_stderr  = true

# --- 2) API ASP.NET Core (espera a MySQL, migra y arranca Kestrel) -----------
[program:api]
command          = /usr/local/bin/fcqi-start-api.sh
priority         = 20
autostart        = true
autorestart      = true
startsecs        = 10
stopwaitsecs     = 30
stdout_logfile   = /dev/fd/1
stdout_logfile_maxbytes = 0
redirect_stderr  = true

# --- 3) Servidor web / proxy inverso -----------------------------------------
[program:nginx]
command          = /usr/sbin/nginx -g "daemon off;"
priority         = 30
autostart        = true
autorestart      = true
startsecs        = 3
stopsignal       = QUIT
stdout_logfile   = /dev/fd/1
stdout_logfile_maxbytes = 0
redirect_stderr  = true

# Si un proceso queda en estado FATAL, el contenedor termina en lugar de
# quedarse "arriba" pero inservible.
[eventlistener:fatal_watcher]
command = /usr/local/bin/fcqi-fatal-watcher.sh
events  = PROCESS_STATE_FATAL
SUP

COPY <<'SH' /assets/fatal-watcher.sh
#!/usr/bin/env bash
# Escucha eventos de supervisor y tumba el contenedor si un servicio muere
# definitivamente (evita contenedores "healthy" con la API caída).
printf 'READY\n'
while read -r _header; do
    printf 'RESULT 2\nOK' 
    echo "[fcqi] Un servicio entró en estado FATAL; deteniendo el contenedor." >&2
    kill -TERM 1
done
SH

# -----------------------------------------------------------------------------
# Entrypoint: prepara directorios, credenciales e inicializa el data directory
# de MySQL la primera vez; después cede el control a supervisor.
# -----------------------------------------------------------------------------
COPY <<'SH' /assets/entrypoint.sh
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
SH

# -----------------------------------------------------------------------------
# Arranque de la API: espera a MySQL, crea la BD/usuario, aplica las migraciones
# de EF Core y finalmente ejecuta Kestrel.
# -----------------------------------------------------------------------------
COPY <<'SH' /assets/start-api.sh
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
# 3) Arrancar la API.
#    El propio Program.cs aplica las migraciones de EF Core y siembra el
#    catálogo 2026-2 en cuanto arranca, así que aquí no se toca el esquema.
# ---------------------------------------------------------------------------
export ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=${DB};User ID=${DB_USER};Password=${DB_PASS};AllowPublicKeyRetrieval=True;"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:5016}"

log "Arrancando FCQI.Api en ${ASPNETCORE_URLS} (entorno ${ASPNETCORE_ENVIRONMENT:-Production})"

if [ -n "${FCQI_API_CMD:-}" ]; then
    exec bash -c "$FCQI_API_CMD"
fi

cd /srv/fcqi/api
exec dotnet /srv/fcqi/api/FCQI.Api.dll
SH

# -----------------------------------------------------------------------------
# Utilidad de la etapa `debug`: recompila el frontend WASM sin recrear la imagen.
# -----------------------------------------------------------------------------
COPY <<'SH' /assets/rebuild-web.sh
#!/usr/bin/env bash
set -euo pipefail
cd /src

WORKLOADS="$(dotnet workload list 2>/dev/null || true)"
case "$WORKLOADS" in
    *wasm-tools*) ;;
    *)
        echo "El workload 'wasm-tools' no está instalado en esta imagen."
        echo "Reconstruye la etapa debug con:  --build-arg DEBUG_WASM_TOOLS=true"
        exit 1
        ;;
esac

CONFIG="${1:-Debug}"
dotnet publish src/FCQI.Web.Browser/FCQI.Web.Browser.csproj \
    -c "$CONFIG" -p:WasmMainJSPath=wwwroot/main.js

# El AppBundle de .NET 8 se genera en bin/<Config>/net8.0-browser/browser-wasm/AppBundle
DOTNET_JS="$(find "src/FCQI.Web.Browser/bin/$CONFIG" -type f -name dotnet.js -path '*_framework*' -print -quit)"
if [ -z "$DOTNET_JS" ]; then
    echo "ERROR: no se encontró el AppBundle tras el publish." >&2
    exit 1
fi
BUNDLE="$(dirname "$(dirname "$DOTNET_JS")")"

rm -rf /srv/fcqi/web/*
cp -a "$BUNDLE/." /srv/fcqi/web/
echo "Frontend actualizado en /srv/fcqi/web (desde $BUNDLE)"
SH


# =============================================================================
#  ETAPA 1 — build
#  Se ejecuta en la arquitectura nativa del equipo que compila ($BUILDPLATFORM):
#  la salida (IL de .NET y el bundle WebAssembly) es independiente de la
#  arquitectura, así que compilar en arm64 y ejecutar en amd64 es correcto.
# =============================================================================
FROM --platform=$BUILDPLATFORM ${DOTNET_SDK_IMAGE} AS build

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    NUGET_XMLDOC_MODE=skip

# python3 lo necesita Emscripten cuando el SDK reenlaza el runtime WebAssembly.
RUN apt-get update \
 && apt-get install -y --no-install-recommends python3 \
 && rm -rf /var/lib/apt/lists/*

# FCQI.Web.Browser tiene como destino net8.0-browser / browser-wasm.
RUN dotnet workload install wasm-tools

WORKDIR /src

# --- Restore en capa aparte: solo se invalida si cambian los .csproj/.sln -----
COPY FCQI.sln ./
COPY src/FCQI.Domain/FCQI.Domain.csproj                 src/FCQI.Domain/
COPY src/FCQI.Application/FCQI.Application.csproj       src/FCQI.Application/
COPY src/FCQI.Infrastructure/FCQI.Infrastructure.csproj src/FCQI.Infrastructure/
COPY src/FCQI.Api/FCQI.Api.csproj                       src/FCQI.Api/
COPY src/FCQI.Web/FCQI.Web.csproj                       src/FCQI.Web/
COPY src/FCQI.Web.Browser/FCQI.Web.Browser.csproj       src/FCQI.Web.Browser/
COPY tests/FCQI.UnitTests/FCQI.UnitTests.csproj         tests/FCQI.UnitTests/
RUN dotnet restore FCQI.sln

# --- Código fuente -----------------------------------------------------------
COPY . .

# --- URL de la API dentro del cliente WebAssembly ----------------------------
# MainViewModel.ApiSubjectsUrl es una constante que se compila dentro del .wasm,
# así que el origen público debe fijarse en tiempo de compilación.
# Con nginx haciendo de proxy inverso, frontend y API comparten origen.
ARG API_BASE_URL=http://localhost:8080
RUN set -eux; \
    CLIENT=src/FCQI.Web/Services/FcqiApiClient.cs; \
    test -f "$CLIENT"; \
    grep -q 'http://localhost:5016' "$CLIENT"; \
    sed -i "s#\"http://localhost:5016\"#\"${API_BASE_URL%/}\"#g" "$CLIENT"; \
    grep -n 'BaseUrl' "$CLIENT"; \
    grep -q "${API_BASE_URL%/}" "$CLIENT"

# --- Pruebas unitarias (desactivables con --build-arg RUN_TESTS=false) -------
ARG RUN_TESTS=true
RUN if [ "$RUN_TESTS" = "true" ]; then \
        dotnet test tests/FCQI.UnitTests/FCQI.UnitTests.csproj \
            -c Release --no-restore --verbosity minimal; \
    else \
        echo "Pruebas omitidas (RUN_TESTS=${RUN_TESTS})."; \
    fi

# --- Inventario de dependencias NuGet (directas + transitivas) ---------------
# Queda dentro de la imagen como /srv/fcqi/dependencies.txt para auditoría.
RUN mkdir -p /out \
 && dotnet list FCQI.sln package --include-transitive > /out/dependencies.txt 2>&1 \
 && head -n 40 /out/dependencies.txt

# --- API ASP.NET Core --------------------------------------------------------
RUN dotnet publish src/FCQI.Api/FCQI.Api.csproj \
        -c Release --no-restore -p:UseAppHost=false \
        -o /out/api \
 && chmod -R a+rX /out/api

# --- Frontend Avalonia WebAssembly ------------------------------------------
# WasmMainJSPath se sobrescribe porque en el .csproj usa separador de Windows.
# La ruta exacta del AppBundle varía entre SDKs, así que se localiza por _framework.
RUN set -eux; \
    dotnet publish src/FCQI.Web.Browser/FCQI.Web.Browser.csproj \
        -c Release -p:WasmMainJSPath=wwwroot/main.js; \
    DOTNET_JS="$(find src/FCQI.Web.Browser/bin/Release -type f -name dotnet.js -path '*_framework*' -print -quit)"; \
    test -n "$DOTNET_JS"; \
    BUNDLE="$(dirname "$(dirname "$DOTNET_JS")")"; \
    echo "AppBundle localizado en: $BUNDLE"; \
    mkdir -p /out/web; \
    cp -a "$BUNDLE/." /out/web/; \
    for f in index.html app.css main.js; do \
        [ -f "/out/web/$f" ] || cp "src/FCQI.Web.Browser/wwwroot/$f" "/out/web/$f"; \
    done; \
    test -f /out/web/index.html; \
    test -d /out/web/_framework; \
    test -f /out/web/_framework/blazor.boot.json; \
    chmod -R a+rX /out/web; \
    UNREADABLE="$(find /out/web -type f ! -perm -o=r -print -quit)"; \
    test -z "$UNREADABLE"; \
    ls -la /out/web

# NOTA sobre migraciones: no se genera ningún script SQL aquí.
# src/FCQI.Api/Program.cs ya ejecuta al arrancar:
#     await db.Database.MigrateAsync();
#     await Catalog20262Seeder.SeedAsync(db);
# de modo que la aplicación crea su propio esquema y su catálogo 2026-2.
# El contenedor solo se encarga de que la base y el usuario existan antes.


# =============================================================================
#  ETAPA 2 — debug
#  SDK + código fuente + MySQL + nginx. La API corre con `dotnet watch`
#  (recarga en caliente) y Swagger queda habilitado.
#
#    docker build --target debug -t fcqi-asesorias:debug .
#    docker run --rm -it -p 8080:80 -p 5016:5016 -p 3306:3306 \
#           -v "$PWD/src:/src/src" fcqi-asesorias:debug
# =============================================================================
FROM ${DOTNET_SDK_IMAGE} AS debug

ARG MYSQL_APT_COMPONENT=mysql-8.4-lts
ARG TZ=America/Tijuana
ARG DEBUG_WASM_TOOLS=false
ARG INSTALL_VSDBG=true

ENV DEBIAN_FRONTEND=noninteractive \
    TZ=${TZ} \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_USE_POLLING_FILE_WATCHER=1 \
    ASPNETCORE_ENVIRONMENT=Development \
    FCQI_API_CMD="cd /src && dotnet watch --project src/FCQI.Api/FCQI.Api.csproj run --no-launch-profile"

COPY --from=assets --chmod=0755 /assets/install-services.sh /tmp/install-services.sh
RUN MYSQL_APT_COMPONENT="${MYSQL_APT_COMPONENT}" /tmp/install-services.sh \
 && rm -f /tmp/install-services.sh \
 && ln -snf "/usr/share/zoneinfo/${TZ}" /etc/localtime && echo "${TZ}" > /etc/timezone

# Depurador remoto para VS Code / Rider (attach al proceso de la API).
RUN if [ "$INSTALL_VSDBG" = "true" ]; then \
        apt-get update && apt-get install -y --no-install-recommends unzip procps \
     && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /usr/local/vsdbg \
     || echo "AVISO: no se pudo instalar vsdbg; la imagen sigue siendo utilizable."; \
        rm -rf /var/lib/apt/lists/*; \
    fi

RUN if [ "$DEBUG_WASM_TOOLS" = "true" ]; then dotnet workload install wasm-tools; fi

COPY --from=assets            /assets/mysqld-fcqi.cnf   /etc/fcqi/mysqld-fcqi.cnf
RUN for d in /etc/mysql/conf.d /etc/mysql/mysql.conf.d; do \
        mkdir -p "$d"; cp /etc/fcqi/mysqld-fcqi.cnf "$d/zz-fcqi.cnf"; \
    done
COPY --from=assets            /assets/fcqi.nginx.conf   /etc/nginx/conf.d/fcqi.conf
COPY --from=assets            /assets/supervisord.conf  /etc/supervisor/fcqi-supervisord.conf
COPY --from=assets --chmod=0755 /assets/entrypoint.sh     /usr/local/bin/fcqi-entrypoint.sh
COPY --from=assets --chmod=0755 /assets/start-api.sh      /usr/local/bin/fcqi-start-api.sh
COPY --from=assets --chmod=0755 /assets/fatal-watcher.sh  /usr/local/bin/fcqi-fatal-watcher.sh
COPY --from=assets --chmod=0755 /assets/rebuild-web.sh    /usr/local/bin/fcqi-rebuild-web.sh

# Código fuente (móntalo con -v "$PWD/src:/src/src" para editar desde el host).
WORKDIR /src
COPY . .
RUN dotnet restore src/FCQI.Api/FCQI.Api.csproj

# Frontend ya compilado en la etapa build (recompílalo con fcqi-rebuild-web.sh).
COPY --from=build /out/web /srv/fcqi/web

EXPOSE 80 5016 3306
VOLUME ["/var/lib/mysql"]
ENTRYPOINT ["/usr/local/bin/fcqi-entrypoint.sh"]


# =============================================================================
#  ETAPA 3 — runtime (imagen final por defecto)
#  Ubuntu 24.04 + runtime de ASP.NET Core 8 + MySQL 8.4 LTS + nginx + supervisor.
# =============================================================================
FROM ubuntu:${UBUNTU_TAG} AS runtime

ARG MYSQL_APT_COMPONENT=mysql-8.4-lts
ARG TZ=America/Tijuana

ENV DEBIAN_FRONTEND=noninteractive \
    TZ=${TZ} \
    LANG=C.UTF-8 \
    LC_ALL=C.UTF-8 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:5016 \
    MYSQL_DATABASE=fcqi_asesorias \
    MYSQL_USER=fcqi_app \
    MYSQL_BIND_ADDRESS=127.0.0.1

LABEL org.opencontainers.image.title="Sistema de Asesorías FCQI" \
      org.opencontainers.image.description="API ASP.NET Core 8 + frontend Avalonia WASM + MySQL 8.4 LTS en un solo contenedor" \
      org.opencontainers.image.vendor="Facultad de Ciencias Químicas e Ingeniería — UABC" \
      org.opencontainers.image.base.name="ubuntu:24.04"

# --- Sistema operativo y servicios ------------------------------------------
COPY --from=assets --chmod=0755 /assets/install-services.sh /tmp/install-services.sh
RUN MYSQL_APT_COMPONENT="${MYSQL_APT_COMPONENT}" /tmp/install-services.sh \
 && rm -f /tmp/install-services.sh \
 && ln -snf "/usr/share/zoneinfo/${TZ}" /etc/localtime && echo "${TZ}" > /etc/timezone

# --- Runtime de .NET 8 (feed nativo de Ubuntu 24.04, sin repos extra) --------
RUN apt-get update \
 && apt-get install -y --no-install-recommends aspnetcore-runtime-8.0 \
 && rm -rf /var/lib/apt/lists/* \
 && dotnet --list-runtimes

# --- Configuración ----------------------------------------------------------
COPY --from=assets            /assets/mysqld-fcqi.cnf   /etc/fcqi/mysqld-fcqi.cnf
RUN for d in /etc/mysql/conf.d /etc/mysql/mysql.conf.d; do \
        mkdir -p "$d"; cp /etc/fcqi/mysqld-fcqi.cnf "$d/zz-fcqi.cnf"; \
    done
COPY --from=assets            /assets/fcqi.nginx.conf   /etc/nginx/conf.d/fcqi.conf
COPY --from=assets            /assets/supervisord.conf  /etc/supervisor/fcqi-supervisord.conf
COPY --from=assets --chmod=0755 /assets/entrypoint.sh    /usr/local/bin/fcqi-entrypoint.sh
COPY --from=assets --chmod=0755 /assets/start-api.sh     /usr/local/bin/fcqi-start-api.sh
COPY --from=assets --chmod=0755 /assets/fatal-watcher.sh /usr/local/bin/fcqi-fatal-watcher.sh

# --- Aplicación -------------------------------------------------------------
COPY --from=build /out/api              /srv/fcqi/api
COPY --from=build /out/web              /srv/fcqi/web
COPY --from=build /out/dependencies.txt /srv/fcqi/dependencies.txt

# 80   → nginx (frontend + /api + /swagger)
# 5016 → Kestrel directo (opcional, para diagnóstico)
# 3306 → MySQL (solo si arrancas con MYSQL_BIND_ADDRESS=0.0.0.0)
EXPOSE 80 5016 3306

VOLUME ["/var/lib/mysql"]

HEALTHCHECK --interval=30s --timeout=10s --start-period=120s --retries=5 \
    CMD curl -fsS http://127.0.0.1/healthz > /dev/null \
     && curl -fsS http://127.0.0.1/api/subjects > /dev/null || exit 1

ENTRYPOINT ["/usr/local/bin/fcqi-entrypoint.sh"]
