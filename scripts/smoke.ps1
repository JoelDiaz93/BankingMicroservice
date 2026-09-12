$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Assert-CommandAvailable {
  param([Parameter(Mandatory = $true)][string]$Name)
  if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
    throw "No se encontro '$Name' en PATH."
  }
}

function Wait-JsonEndpoint {
  param(
    [Parameter(Mandatory = $true)][string]$Name,
    [Parameter(Mandatory = $true)][string]$Url,
    [int]$Attempts = 30,
    [int]$DelaySeconds = 2
  )

  for ($i = 1; $i -le $Attempts; $i++) {
    try {
      $result = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 5
      Write-Host "OK  $Name -> $Url"
      return $result
    }
    catch {
      if ($i -eq $Attempts) {
        throw "$Name no respondio correctamente tras $Attempts intentos. URL: $Url. Error: $($_.Exception.Message)"
      }
      Start-Sleep -Seconds $DelaySeconds
    }
  }
}

Assert-CommandAvailable "docker"

Write-Host "==> Servicios Docker"
& docker compose ps
if ($LASTEXITCODE -ne 0) { throw "docker compose ps fallo." }

Write-Host "==> Estado de las APIs"
$clientsReady = Wait-JsonEndpoint "Clients API" "http://localhost:8081/health/ready"
$accountsReady = Wait-JsonEndpoint "Accounts API" "http://localhost:8082/health/ready"

if ($clientsReady.status -ne "ready") { throw "Clients no reporta status=ready." }
if ($accountsReady.status -ne "ready") { throw "Accounts no reporta status=ready." }

Write-Host "==> Consultas basicas"
$clients = Wait-JsonEndpoint "GET clientes" "http://localhost:8081/api/clientes"
$accounts = Wait-JsonEndpoint "GET cuentas" "http://localhost:8082/api/cuentas"

Write-Host "Clients devueltos: $(@($clients).Count)"
Write-Host "Cuentas devueltas: $(@($accounts).Count)"
Write-Host "Servicios verificados correctamente."
