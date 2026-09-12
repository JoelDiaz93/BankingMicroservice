# Datos iniciales

`BaseDatos.sql` crea `clients_db` y `accounts_db` y carga los datos necesarios para probar la aplicación desde el primer arranque.

Los identificadores técnicos (`clienteId` y `cuentaId`) son UUID generados con `gen_random_uuid()` en cada inicialización. Los identificadores de negocio permanecen estables.

| Cliente | Identificación | Cuenta | Tipo | Saldo inicial |
|---|---|---|---|---:|
| Jose Lema | `1100000001` | `478758` | Ahorros | 2000 |
| Marianela Montalvo | `1100000002` | `225487` | Corriente | 100 |
| Juan Osorio | `1100000003` | `495878` | Ahorros | 0 |
| Marianela Montalvo | `1100000002` | `496825` | Ahorros | 540 |

## Formatos

- `identificacion`: 10 dígitos (`varchar(10)` + `CHECK`).
- `numero_cuenta`: 6 dígitos (`varchar(6)` + `CHECK`).
- UUIDs: generados dinámicamente y compartidos entre la entidad de cliente y su proyección en Accounts.

Para regenerar por completo los UUID y los datos iniciales:

```powershell
docker compose down -v --remove-orphans
docker compose up -d
```
