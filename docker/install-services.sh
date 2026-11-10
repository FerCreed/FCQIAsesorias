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
