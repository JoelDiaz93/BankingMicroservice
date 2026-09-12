# Requisitos y trazabilidad

| ID | Requisito | Implementación |
|---|---|---|
| G1 | Clean Code / Clean Architecture / Repository | Capas por servicio + repositorios + DI |
| G2 | Entidades con Entity Framework Core | `ClientsDbContext`, `AccountsDbContext` |
| G3 | Manejo de excepciones | `ExceptionHandlingMiddleware` en ambas APIs |
| G4 | Al menos una prueba unitaria | `Clients.Domain.Tests/ClienteTests.cs` |
| G5 | Docker | Dockerfile por API + `docker-compose.yml` |
| G6 | Base relacional | PostgreSQL 16 |
| MS1 | Cliente/Persona en microservicio | `src/Services/Clients` |
| MS2 | Cuenta/Movimientos en microservicio | `src/Services/Accounts` |
| MS3 | Comunicación asíncrona | RabbitMQ `client.*.v1` |
| Persona | nombre, género, edad, identificación, dirección, teléfono, PK | `Persona.cs` + EF mapping |
| Cliente | hereda Persona, clienteId/PK, contraseña, estado | `Cliente.cs`; `Id` heredado funciona como `clienteId` |
| Cuenta | número, tipo, saldo inicial, estado, clave única | `Cuenta.cs` + unique index |
| Movimiento | fecha, tipo, valor, saldo, clave única | `Movimiento.cs` |
| F1 | CRUD Cliente | GET/POST/PUT/PATCH/DELETE |
| F1 | CRU Cuenta | GET/POST/PUT/PATCH |
| F1 | CRU Movimiento | GET/POST/PUT |
| F2 | valores positivos/negativos + saldo | `Cuenta.ApplyMovement` |
| F2 | registro de transacciones | tabla `movimientos` |
| F3 | `Saldo no disponible` | `InsufficientBalanceException` → 409 |
| F4 | reporte por fechas y cliente | `/api/reportes` |
| F4 | cuentas/saldos + detalle movimientos | DTO `ReporteEstadoCuentaResponse` |
| F5 | unit test Cliente | `Clients.Domain.Tests` + `Clients.Application.Tests` (7 pruebas) |
| F6 | integration test | `Clients.Api.IntegrationTests`, `Accounts.IntegrationTests` y `Banking.Messaging.IntegrationTests` con Testcontainers |
| F7 | contenedores | Compose con APIs, PostgreSQL y RabbitMQ |
| E1 | `BaseDatos.sql` | raíz del repositorio |
| E2 | JSON Postman | carpeta `postman` |
| E3 | README despliegue | `README.md` |
