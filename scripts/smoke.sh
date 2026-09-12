#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

command -v docker >/dev/null 2>&1 || { echo "No se encontro 'docker' en PATH." >&2; exit 1; }
command -v curl >/dev/null 2>&1 || { echo "No se encontro 'curl' en PATH." >&2; exit 1; }

wait_endpoint() {
  local name="$1"
  local url="$2"
  local attempts="${3:-30}"
  local delay="${4:-2}"
  local i
  for ((i=1; i<=attempts; i++)); do
    if curl --fail --silent --show-error --max-time 5 "$url" >/dev/null; then
      echo "OK  $name -> $url"
      return 0
    fi
    sleep "$delay"
  done
  echo "$name no respondio correctamente tras $attempts intentos: $url" >&2
  return 1
}

echo "==> Servicios Docker"
docker compose ps

echo "==> Estado de las APIs"
wait_endpoint "Clients API" "http://localhost:8081/health/ready"
wait_endpoint "Accounts API" "http://localhost:8082/health/ready"

echo "==> Consultas básicas"
wait_endpoint "GET clientes" "http://localhost:8081/api/clientes"
wait_endpoint "GET cuentas" "http://localhost:8082/api/cuentas"

echo "Servicios verificados correctamente."
