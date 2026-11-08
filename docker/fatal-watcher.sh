#!/usr/bin/env bash
# Escucha eventos de supervisor y tumba el contenedor si un servicio muere
# definitivamente (evita contenedores "healthy" con la API caída).
printf 'READY\n'
while read -r _header; do
    printf 'RESULT 2\nOK' 
    echo "[fcqi] Un servicio entró en estado FATAL; deteniendo el contenedor." >&2
    kill -TERM 1
done
