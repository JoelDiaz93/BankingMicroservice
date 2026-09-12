$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Assert-CommandAvailable {
  param([Parameter(Mandatory = $true)][string]$Name)

  if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
    throw "No se encontro '$Name' en PATH. Instale/configure la herramienta antes de continuar."
  }
}

function Invoke-CheckedCommand {
  param(
    [Parameter(Mandatory = $true)][string]$Label,
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string[]]$Arguments
  )

  Write-Host "==> $Label"
  & $Executable @Arguments
  $exitCode = $LASTEXITCODE
  if ($exitCode -ne 0) {
    throw "$Label fallo con codigo de salida $exitCode."
  }
}

Write-Host "==> Comprobando archivos del proyecto"
$required = @(
  "BankingMicroservices.sln",
  "BaseDatos.sql",
  "docker-compose.yml",
  ".config/dotnet-tools.json",
  "postman/BankingMicroservices.postman_collection.json",
  "src/Services/Clients/Clients.Api/Clients.Api.csproj",
  "src/Services/Accounts/Accounts.Api/Accounts.Api.csproj",
  "tests/Clients.Domain.Tests/Clients.Domain.Tests.csproj",
  "tests/Clients.Application.Tests/Clients.Application.Tests.csproj",
  "tests/Clients.Api.IntegrationTests/Clients.Api.IntegrationTests.csproj",
  "tests/Accounts.Domain.Tests/Accounts.Domain.Tests.csproj",
  "tests/Accounts.IntegrationTests/Accounts.IntegrationTests.csproj",
  "tests/Banking.Messaging.IntegrationTests/Banking.Messaging.IntegrationTests.csproj"
)
foreach ($file in $required) {
  if (-not (Test-Path $file)) {
    throw "Falta archivo requerido: $file"
  }
}

Assert-CommandAvailable "dotnet"
Assert-CommandAvailable "docker"

Invoke-CheckedCommand "Restaurando dependencias" "dotnet" @("restore", "BankingMicroservices.sln")
Invoke-CheckedCommand "Compilando en Release" "dotnet" @("build", "BankingMicroservices.sln", "-c", "Release", "--no-restore")
# Los tests de integracion usan Testcontainers; Docker debe estar disponible antes de dotnet test.
Invoke-CheckedCommand "Comprobando Docker" "docker" @("info", "--format", "{{.ServerVersion}}")

$testProjects = @(
  @{ Label = "Pruebas de dominio - Clients"; Project = "tests/Clients.Domain.Tests/Clients.Domain.Tests.csproj" },
  @{ Label = "Pruebas de aplicacion - Clients"; Project = "tests/Clients.Application.Tests/Clients.Application.Tests.csproj" },
  @{ Label = "Pruebas de dominio - Accounts"; Project = "tests/Accounts.Domain.Tests/Accounts.Domain.Tests.csproj" },
  @{ Label = "Pruebas de integracion - Accounts/PostgreSQL"; Project = "tests/Accounts.IntegrationTests/Accounts.IntegrationTests.csproj" },
  @{ Label = "Pruebas de integracion - Clients API/PostgreSQL"; Project = "tests/Clients.Api.IntegrationTests/Clients.Api.IntegrationTests.csproj" },
  @{ Label = "Pruebas entre servicios - PostgreSQL/RabbitMQ"; Project = "tests/Banking.Messaging.IntegrationTests/Banking.Messaging.IntegrationTests.csproj" }
)

foreach ($test in $testProjects) {
  Invoke-CheckedCommand $test.Label "dotnet" @("test", $test.Project, "-c", "Release", "--no-build")
}

Invoke-CheckedCommand "Configuracion de Docker Compose" "docker" @("compose", "config", "--quiet")

Write-Host "Proyecto verificado correctamente."
