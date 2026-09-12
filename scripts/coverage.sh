#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

command -v dotnet >/dev/null 2>&1 || { echo "No se encontro 'dotnet' en PATH." >&2; exit 1; }
command -v docker >/dev/null 2>&1 || { echo "No se encontro 'docker' en PATH." >&2; exit 1; }

echo "==> Comprobando Docker"
docker info --format '{{.ServerVersion}}' >/dev/null

echo "==> Restaurando dependencias local tools"
dotnet tool restore

rm -rf TestResults coverage-report

echo "==> Compilando en Release"
dotnet build BankingMicroservices.sln -c Release

test_projects=(
  tests/Clients.Domain.Tests/Clients.Domain.Tests.csproj
  tests/Clients.Application.Tests/Clients.Application.Tests.csproj
  tests/Accounts.Domain.Tests/Accounts.Domain.Tests.csproj
  tests/Accounts.IntegrationTests/Accounts.IntegrationTests.csproj
  tests/Clients.Api.IntegrationTests/Clients.Api.IntegrationTests.csproj
  tests/Banking.Messaging.IntegrationTests/Banking.Messaging.IntegrationTests.csproj
)

for project in "${test_projects[@]}"; do
  safe_name="$(basename "$project" .csproj | tr '.' '-')"
  echo "==> Cobertura: $project"
  dotnet test "$project" -c Release --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory "./TestResults/$safe_name"
done

echo "==> Generando reporte de cobertura"
dotnet tool run reportgenerator \
  '-reports:./TestResults/**/coverage.cobertura.xml' \
  '-targetdir:./coverage-report' \
  '-reporttypes:Html;TextSummary;Cobertura' \
  '-assemblyfilters:+Clients.*;+Accounts.*;-*.Tests'

cat ./coverage-report/Summary.txt
echo "Reporte HTML: $(pwd)/coverage-report/index.html"
