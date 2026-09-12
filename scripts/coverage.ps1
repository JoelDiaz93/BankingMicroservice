$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

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

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "No se encontro 'dotnet' en PATH." }
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw "No se encontro 'docker' en PATH." }

Invoke-CheckedCommand "Comprobando Docker" "docker" @("info", "--format", "{{.ServerVersion}}")

# Los archivos extraidos de un ZIP descargado pueden heredar Mark-of-the-Web en Windows.
# dotnet tool restore rechaza un manifiesto bloqueado, por lo que desbloqueamos solo ese archivo.
$toolManifest = Join-Path $root ".config\dotnet-tools.json"
if (Test-Path $toolManifest) {
  Unblock-File -Path $toolManifest -ErrorAction SilentlyContinue
}

Invoke-CheckedCommand "Restaurando herramientas locales" "dotnet" @("tool", "restore")

Remove-Item .\TestResults -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\coverage-report -Recurse -Force -ErrorAction SilentlyContinue

Invoke-CheckedCommand "Compilando en Release" "dotnet" @("build", "BankingMicroservices.sln", "-c", "Release")

$testProjects = @(
  "tests/Clients.Domain.Tests/Clients.Domain.Tests.csproj",
  "tests/Clients.Application.Tests/Clients.Application.Tests.csproj",
  "tests/Accounts.Domain.Tests/Accounts.Domain.Tests.csproj",
  "tests/Accounts.IntegrationTests/Accounts.IntegrationTests.csproj",
  "tests/Clients.Api.IntegrationTests/Clients.Api.IntegrationTests.csproj",
  "tests/Banking.Messaging.IntegrationTests/Banking.Messaging.IntegrationTests.csproj"
)

foreach ($project in $testProjects) {
  $safeName = ([IO.Path]::GetFileNameWithoutExtension($project)).Replace(".", "-")
  Invoke-CheckedCommand "Cobertura: $project" "dotnet" @(
    "test", $project,
    "-c", "Release",
    "--no-build",
    "--collect:XPlat Code Coverage",
    "--results-directory", ".\TestResults\$safeName"
  )
}

Invoke-CheckedCommand "Generando reporte de cobertura" "dotnet" @(
  "tool", "run", "reportgenerator",
  "-reports:.\TestResults\**\coverage.cobertura.xml",
  "-targetdir:.\coverage-report",
  "-reporttypes:Html;TextSummary;Cobertura",
  "-assemblyfilters:+Clients.*;+Accounts.*;-*.Tests"
)

Write-Host ""
Write-Host "==> Resumen de cobertura"
Get-Content .\coverage-report\Summary.txt
Write-Host ""
Write-Host "Reporte HTML: $root\coverage-report\index.html"
