# Pruebas

La solución contiene **34 pruebas automatizadas** orientadas a reglas de dominio, servicios de aplicación, persistencia real y comunicación entre microservicios.

| Proyecto | Pruebas | Alcance |
|---|---:|---|
| `Clients.Domain.Tests` | 6 | creación, estado e identificación de 10 dígitos |
| `Clients.Application.Tests` | 4 | hashing, duplicados, actualización y eventos |
| `Clients.Api.IntegrationTests` | 6 | HTTP, PostgreSQL, Outbox y validación del payload |
| `Accounts.Domain.Tests` | 11 | cuenta, número de 6 dígitos, saldo y movimientos |
| `Accounts.IntegrationTests` | 5 | persistencia, transacciones, reportes y concurrencia |
| `Banking.Messaging.IntegrationTests` | 2 | RabbitMQ, Inbox/Outbox y flujo entre servicios |
| **Total** | **34** | |

## Ejecutar todas las verificaciones

```powershell
.\scripts\verify.ps1
```

## Ejecutar una suite

```powershell
dotnet test .\tests\Clients.Domain.Tests\Clients.Domain.Tests.csproj -c Release
dotnet test .\tests\Accounts.Domain.Tests\Accounts.Domain.Tests.csproj -c Release
```

Las pruebas de integración utilizan Testcontainers, por lo que Docker debe estar iniciado.

## Cobertura

```powershell
.\scripts\coverage.ps1
Start-Process .\coverage-report\index.html
```

El reporte HTML y `Summary.txt` se generan en `coverage-report/`. Los porcentajes se calculan en cada ejecución para reflejar el estado real del código.
