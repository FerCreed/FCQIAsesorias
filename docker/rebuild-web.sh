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
# Versión nueva en la URL de main.js: evita que el navegador use la app anterior.
sed -i "s/__BUILD__/$(date +%s)/g" /srv/fcqi/web/index.html
echo "Frontend actualizado en /srv/fcqi/web (desde $BUNDLE)"
