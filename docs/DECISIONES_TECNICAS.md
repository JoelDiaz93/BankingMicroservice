# Decisiones técnicas

Este documento resume las decisiones de diseño más relevantes de la solución y el motivo detrás de cada una.

## Separación de servicios y datos

El dominio se divide en dos servicios: `Clients` administra Persona/Cliente y `Accounts` administra Cuenta/Movimiento. Cada servicio controla su modelo y su base lógica. `Accounts` no consulta `clients_db`; recibe cambios de cliente mediante eventos y mantiene una proyección local en `client_snapshots`.

## Comunicación asíncrona

`Clients` publica `client.created.v1`, `client.updated.v1` y `client.deleted.v1` en RabbitMQ. `Accounts` consume esos eventos y actualiza su proyección local. Esto evita una dependencia HTTP síncrona entre servicios para operaciones sobre cuentas.

## Outbox e Inbox

El cambio de un cliente y su evento se guardan en la misma transacción. Un `BackgroundService` publica posteriormente los mensajes pendientes del Outbox. En `Accounts`, el Inbox registra cada `EventId` procesado para evitar duplicados ante redelivery del broker.

## Protección del saldo

`Cuenta` es responsable de su saldo. `ApplyMovement` rechaza cualquier operación cuyo resultado sea negativo. Las operaciones de movimiento se ejecutan dentro de una transacción `Serializable`, de manera que dos retiros concurrentes no puedan consumir el mismo saldo disponible.

## Saldo histórico en movimientos

Cada movimiento guarda el saldo resultante de la cuenta. Esto facilita la generación del estado de cuenta y permite recalcular los saldos posteriores cuando se modifica un movimiento existente.

## Herencia Cliente / Persona

`Cliente` hereda de `Persona`, conforme al modelo solicitado. EF Core usa TPT: los datos comunes se almacenan en `personas` y los específicos del cliente en `clientes`, enlazados por la misma clave primaria.

## Errores HTTP

Ambas APIs usan `ProblemDetails` para respuestas de error. Los casos principales son:

- `400 Bad Request` para parámetros inválidos;
- `404 Not Found` para recursos inexistentes;
- `409 Conflict` para saldo insuficiente o conflictos de persistencia;
- `422 Unprocessable Entity` para reglas de dominio;
- `500 Internal Server Error` para errores no controlados.

Las respuestas incluyen `traceId` para facilitar diagnóstico.

## Seguridad de contraseñas

Las contraseñas no se devuelven por API. Los clientes creados en runtime almacenan un hash PBKDF2-SHA256 con salt aleatorio.

## Rendimiento y escalabilidad

Las consultas de lectura usan `AsNoTracking`, existen índices para búsquedas frecuentes y las operaciones I/O son asíncronas. Los servicios son stateless y pueden escalar horizontalmente. Para escenarios de mayor volumen, el reporte podría migrar a una proyección de lectura dedicada.

## Consideraciones para producción

Una evolución hacia producción debería incorporar autenticación OIDC/JWT, secretos administrados, TLS, observabilidad, DLQ y políticas de retry, migraciones versionadas, rate limiting, infraestructura administrada/HA y pruebas de carga y seguridad.
