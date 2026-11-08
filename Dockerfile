# syntax=docker/dockerfile:1.7
# =============================================================================
#  Sistema de Asesorías FCQI — imagen "todo en uno"
#  ---------------------------------------------------------------------------
#  Destino : servidor académico con Ubuntu Server 24.04 LTS (amd64)
#  Contiene: Ubuntu 24.04 + .NET 8 (SDK para build/debug, runtime ASP.NET Core
#            para producción) + MySQL 8.4 LTS + nginx + supervisor.
#
#  Etapas (docker build --target <etapa>):
#    build    → compila y prueba la solución, publica la API y el frontend
#               Avalonia WASM (el cliente de escritorio no entra en la imagen).
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
#    docker build -t fcqi-asesorias .
#    docker run -d --name fcqi -p 8080:80 -v fcqi-mysql:/var/lib/mysql fcqi-asesorias
#    → http://localhost:8080
# =============================================================================

# --- Argumentos globales (usables en las líneas FROM) ------------------------
ARG UBUNTU_TAG=24.04
ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0-noble

# --- Scripts y configuración del contenedor: carpeta docker/ ----------------
#   install-services.sh  MySQL (según arquitectura), nginx y supervisor
#   mysqld-fcqi.cnf      configuración de MySQL
#   fcqi.nginx.conf      sirve el frontend WASM y hace proxy de /api a Kestrel
#                        (mismo origen: el cliente no necesita CORS)
#   supervisord.conf     orquesta mysqld, la API y nginx
#   entrypoint.sh        prepara el data directory de MySQL y arranca supervisor
#   start-api.sh         espera a MySQL, crea base/usuario, instala el esquema
#                        (db/*.sql), aplica migraciones idempotentes y arranca la API
#   fatal-watcher.sh     detiene el contenedor si un proceso muere sin remedio
#   rebuild-web.sh       (debug) recompila el frontend sin recrear la imagen

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
COPY src/FCQI.Desktop/FCQI.Desktop.csproj               src/FCQI.Desktop/
COPY tests/FCQI.UnitTests/FCQI.UnitTests.csproj         tests/FCQI.UnitTests/
RUN dotnet restore FCQI.sln

# --- Código fuente -----------------------------------------------------------
COPY . .

# --- URL de la API ----------------------------------------------------------
# No se compila dentro del cliente: en el navegador es el mismo origen de la
# página (nginx sirve /api) y en escritorio se lee de FCQI_API_URL.

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

# NOTA sobre el esquema: lo definen db/01-schema.sql y db/02-seed.sql, que se
# copian a /srv/fcqi/db en las etapas debug y runtime. start-api.sh los ejecuta
# contra MySQL antes de arrancar la API. La aplicación ya no crea ni siembra
# nada: no hay migraciones de EF Core ni seeder en C#.


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

COPY --chmod=0755 docker/install-services.sh /tmp/install-services.sh
# CRLF (checkout en Windows) rompe el shebang: /usr/bin/env: 'bash\r'
RUN sed -i 's/\r$//' /tmp/install-services.sh \
 && MYSQL_APT_COMPONENT="${MYSQL_APT_COMPONENT}" bash /tmp/install-services.sh \
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

COPY docker/mysqld-fcqi.cnf /etc/fcqi/mysqld-fcqi.cnf
RUN for d in /etc/mysql/conf.d /etc/mysql/mysql.conf.d; do \
        mkdir -p "$d"; cp /etc/fcqi/mysqld-fcqi.cnf "$d/zz-fcqi.cnf"; \
    done
COPY docker/fcqi.nginx.conf /etc/nginx/conf.d/fcqi.conf
COPY docker/supervisord.conf /etc/supervisor/fcqi-supervisord.conf
COPY --chmod=0755 docker/entrypoint.sh /usr/local/bin/fcqi-entrypoint.sh
COPY --chmod=0755 docker/start-api.sh /usr/local/bin/fcqi-start-api.sh
COPY --chmod=0755 docker/fatal-watcher.sh /usr/local/bin/fcqi-fatal-watcher.sh
COPY --chmod=0755 docker/rebuild-web.sh /usr/local/bin/fcqi-rebuild-web.sh
RUN sed -i 's/\r$//' /usr/local/bin/fcqi-*.sh /etc/fcqi/mysqld-fcqi.cnf /etc/nginx/conf.d/fcqi.conf /etc/supervisor/fcqi-supervisord.conf \
 && for d in /etc/mysql/conf.d /etc/mysql/mysql.conf.d; do cp /etc/fcqi/mysqld-fcqi.cnf "$d/zz-fcqi.cnf"; done

# Código fuente (móntalo con -v "$PWD/src:/src/src" para editar desde el host).
WORKDIR /src
COPY . .
RUN dotnet restore src/FCQI.Api/FCQI.Api.csproj

# Frontend ya compilado en la etapa build (recompílalo con fcqi-rebuild-web.sh).
COPY --from=build /out/web /srv/fcqi/web

# Definición de la base: estructura y datos de prueba. start-api.sh los ejecuta
# contra MySQL antes de arrancar la API. Sustituyen al seeder que antes vivía
# compilado dentro de FCQI.Infrastructure.
COPY db/01-schema.sql db/02-seed.sql /srv/fcqi/db/
COPY db/migrations/ /srv/fcqi/db/migrations/

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
COPY --chmod=0755 docker/install-services.sh /tmp/install-services.sh
RUN sed -i 's/\r$//' /tmp/install-services.sh \
 && MYSQL_APT_COMPONENT="${MYSQL_APT_COMPONENT}" bash /tmp/install-services.sh \
 && rm -f /tmp/install-services.sh \
 && ln -snf "/usr/share/zoneinfo/${TZ}" /etc/localtime && echo "${TZ}" > /etc/timezone

# --- Runtime de .NET 8 (feed nativo de Ubuntu 24.04, sin repos extra) --------
RUN apt-get update \
 && apt-get install -y --no-install-recommends aspnetcore-runtime-8.0 \
 && rm -rf /var/lib/apt/lists/* \
 && dotnet --list-runtimes

# --- Configuración ----------------------------------------------------------
COPY docker/mysqld-fcqi.cnf /etc/fcqi/mysqld-fcqi.cnf
RUN for d in /etc/mysql/conf.d /etc/mysql/mysql.conf.d; do \
        mkdir -p "$d"; cp /etc/fcqi/mysqld-fcqi.cnf "$d/zz-fcqi.cnf"; \
    done
COPY docker/fcqi.nginx.conf /etc/nginx/conf.d/fcqi.conf
COPY docker/supervisord.conf /etc/supervisor/fcqi-supervisord.conf
COPY --chmod=0755 docker/entrypoint.sh /usr/local/bin/fcqi-entrypoint.sh
COPY --chmod=0755 docker/start-api.sh /usr/local/bin/fcqi-start-api.sh
COPY --chmod=0755 docker/fatal-watcher.sh /usr/local/bin/fcqi-fatal-watcher.sh
RUN sed -i 's/\r$//' /usr/local/bin/fcqi-*.sh /etc/fcqi/mysqld-fcqi.cnf /etc/nginx/conf.d/fcqi.conf /etc/supervisor/fcqi-supervisord.conf \
 && for d in /etc/mysql/conf.d /etc/mysql/mysql.conf.d; do cp /etc/fcqi/mysqld-fcqi.cnf "$d/zz-fcqi.cnf"; done

# --- Aplicación -------------------------------------------------------------
COPY --from=build /out/api              /srv/fcqi/api
COPY --from=build /out/web              /srv/fcqi/web
COPY --from=build /out/dependencies.txt /srv/fcqi/dependencies.txt

# Definición de la base: estructura y datos de prueba. start-api.sh los ejecuta
# contra MySQL antes de arrancar la API. Sustituyen al seeder que antes vivía
# compilado dentro de FCQI.Infrastructure.
COPY db/01-schema.sql db/02-seed.sql /srv/fcqi/db/
COPY db/migrations/ /srv/fcqi/db/migrations/

# 80   → nginx (frontend + /api + /swagger)
# 5016 → Kestrel directo (opcional, para diagnóstico)
# 3306 → MySQL (solo si arrancas con MYSQL_BIND_ADDRESS=0.0.0.0)
EXPOSE 80 5016 3306

VOLUME ["/var/lib/mysql"]

HEALTHCHECK --interval=30s --timeout=10s --start-period=120s --retries=5 \
    CMD curl -fsS http://127.0.0.1/healthz > /dev/null \
     && curl -fsS http://127.0.0.1/api/subjects > /dev/null || exit 1

ENTRYPOINT ["/usr/local/bin/fcqi-entrypoint.sh"]
