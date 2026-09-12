# Colección Postman

La colección `postman/BankingMicroservices.postman_collection.json` permite recorrer las APIs de Clients y Accounts sin configurar un Environment adicional.

## Ejecución completa

La ejecución integral está diseñada para realizarse en orden, sobre una base recién inicializada:

```powershell
docker compose down -v --remove-orphans
docker compose up -d
.\scripts\smoke.ps1
```

Después importe **Banking Microservices API** y use **Run collection** desde el primer request.

La secuencia:

1. comprueba readiness y descubre los UUID dinámicos de los datos iniciales;
2. crea y modifica un cliente temporal;
3. crea y modifica cuentas;
4. registra depósitos y retiros y comprueba las reglas de saldo;
5. consulta el estado de cuenta;
6. elimina el cliente temporal y comprueba los saldos resultantes.

## Datos dinámicos

La colección nunca depende de UUID fijos. Los clientes del seed se localizan por sus identificaciones (`1100000001`, `1100000002`, `1100000003`) y las cuentas por sus números (`478758`, `225487`, `495878`, `496825`).

Los datos temporales respetan las restricciones del dominio:

- identificación: exactamente 10 dígitos;
- número de cuenta: exactamente 6 dígitos;
- UUID: obtenido de las respuestas de la API.

`Crear cliente` regenera su identificación antes de cada envío y puede ejecutarse de forma individual. `Crear cuenta de prueba` hace lo mismo con el número de cuenta y resuelve automáticamente el UUID de Jose Lema desde Clients API.

Los requests dependientes indican en su descripción qué creación debe haberse ejecutado antes cuando se prueban de forma individual.

## Casos negativos

Las pruebas de validación alteran únicamente el dato que se desea comprobar. Por ejemplo, una identificación corta mantiene nombre, dirección, teléfono y contraseña válidos; una cuenta con saldo negativo mantiene un cliente válido, un número de seis dígitos y un tipo de cuenta admitido. Esto permite atribuir el `400` a la regla bajo prueba y evita falsos positivos.
