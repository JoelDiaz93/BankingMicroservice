#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

required=(
  BankingMicroservices.sln
  BaseDatos.sql
  docker-compose.yml
  .config/dotnet-tools.json
  postman/BankingMicroservices.postman_collection.json
  src/Services/Clients/Clients.Api/Clients.Api.csproj
  src/Services/Accounts/Accounts.Api/Accounts.Api.csproj
  tests/Clients.Domain.Tests/Clients.Domain.Tests.csproj
  tests/Clients.Application.Tests/Clients.Application.Tests.csproj
  tests/Clients.Api.IntegrationTests/Clients.Api.IntegrationTests.csproj
  tests/Accounts.Domain.Tests/Accounts.Domain.Tests.csproj
  tests/Accounts.IntegrationTests/Accounts.IntegrationTests.csproj
  tests/Banking.Messaging.IntegrationTests/Banking.Messaging.IntegrationTests.csproj
)

for file in "${required[@]}"; do
  [[ -f "$file" ]] || { echo "Falta archivo requerido: $file" >&2; exit 1; }
done

command -v dotnet >/dev/null 2>&1 || { echo "No se encontro 'dotnet' en PATH." >&2; exit 1; }
command -v docker >/dev/null 2>&1 || { echo "No se encontro 'docker' en PATH." >&2; exit 1; }

echo "==> Restaurando dependencias"
dotnet restore BankingMicroservices.sln

echo "==> Compilando en Release"
dotnet build BankingMicroservices.sln -c Release --no-restore

echo "==> Comprobando Docker"
docker info --format '{{.ServerVersion}}' >/dev/null

test_projects=(
  tests/Clients.Domain.Tests/Clients.Domain.Tests.csproj
  tests/Clients.Application.Tests/Clients.Application.Tests.csproj
  tests/Accounts.Domain.Tests/Accounts.Domain.Tests.csproj
  tests/Accounts.IntegrationTests/Accounts.IntegrationTests.csproj
  tests/Clients.Api.IntegrationTests/Clients.Api.IntegrationTests.csproj
  tests/Banking.Messaging.IntegrationTests/Banking.Messaging.IntegrationTests.csproj
)

for project in "${test_projects[@]}"; do
  echo "==> Pruebas: $project"
  dotnet test "$project" -c Release --no-build
done

echo "==> Configuración de Docker Compose"
docker compose config --quiet

echo "Proyecto verificado correctamente."
