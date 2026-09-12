-- BaseDatos.sql
-- Script reproducible para PostgreSQL. Docker lo ejecuta automáticamente en el primer arranque.

CREATE DATABASE clients_db;
CREATE DATABASE accounts_db;

\connect clients_db

CREATE TABLE personas (
    id uuid PRIMARY KEY,
    nombre varchar(150) NOT NULL,
    genero varchar(30) NOT NULL,
    edad integer NOT NULL CHECK (edad BETWEEN 0 AND 130),
    identificacion varchar(10) NOT NULL CHECK (identificacion ~ '^[0-9]{10}$'),
    direccion varchar(250) NOT NULL,
    telefono varchar(30) NOT NULL
);
CREATE UNIQUE INDEX ux_personas_identificacion ON personas(identificacion);

CREATE TABLE clientes (
    id uuid PRIMARY KEY REFERENCES personas(id) ON DELETE CASCADE,
    password_hash varchar(512) NOT NULL,
    estado boolean NOT NULL,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL
);

CREATE TABLE outbox_messages (
    id uuid PRIMARY KEY,
    occurred_on_utc timestamptz NOT NULL,
    event_type varchar(200) NOT NULL,
    routing_key varchar(100) NOT NULL,
    payload jsonb NOT NULL,
    processed_on_utc timestamptz NULL,
    attempts integer NOT NULL DEFAULT 0,
    last_error varchar(2000) NULL
);
CREATE INDEX ix_outbox_pending ON outbox_messages(processed_on_utc, occurred_on_utc);

-- Los identificadores técnicos se generan en cada inicialización, igual que los nuevos agregados de dominio.
-- \gset guarda los UUID como variables de psql y permite reutilizarlos después de cambiar de base.
SELECT
    gen_random_uuid() AS jose_lema_id,
    gen_random_uuid() AS marianela_montalvo_id,
    gen_random_uuid() AS juan_osorio_id
\gset

-- Datos iniciales. Identificación, género y edad completan la entidad Persona.
INSERT INTO personas(id,nombre,genero,edad,identificacion,direccion,telefono) VALUES
(:'jose_lema_id','Jose Lema','Masculino',35,'1100000001','Otavalo sn y principal','098254785'),
(:'marianela_montalvo_id','Marianela Montalvo','Femenino',33,'1100000002','Amazonas y NNUU','097548965'),
(:'juan_osorio_id','Juan Osorio','Masculino',31,'1100000003','13 junio y Equinoccial','098874587');

-- La API no expone la contraseña. Los clientes creados desde la aplicación usan PBKDF2-SHA256.
INSERT INTO clientes(id,password_hash,estado,created_at_utc,updated_at_utc) VALUES
(:'jose_lema_id','seed-demo:1234',true,now(),now()),
(:'marianela_montalvo_id','seed-demo:5678',true,now(),now()),
(:'juan_osorio_id','seed-demo:1245',true,now(),now());

\connect accounts_db

CREATE TABLE client_snapshots (
    cliente_id uuid PRIMARY KEY,
    nombre varchar(150) NOT NULL,
    estado boolean NOT NULL,
    updated_at_utc timestamptz NOT NULL
);
CREATE INDEX ix_client_snapshots_nombre ON client_snapshots(nombre);

CREATE TABLE cuentas (
    id uuid PRIMARY KEY,
    numero_cuenta varchar(6) NOT NULL CHECK (numero_cuenta ~ '^[0-9]{6}$'),
    tipo_cuenta varchar(20) NOT NULL CHECK (tipo_cuenta IN ('Ahorros','Corriente')),
    saldo_inicial numeric(18,2) NOT NULL CHECK (saldo_inicial >= 0),
    saldo_disponible numeric(18,2) NOT NULL CHECK (saldo_disponible >= 0),
    estado boolean NOT NULL,
    cliente_id uuid NOT NULL,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL
);
CREATE UNIQUE INDEX ux_cuentas_numero ON cuentas(numero_cuenta);
CREATE INDEX ix_cuentas_cliente_id ON cuentas(cliente_id);

CREATE TABLE movimientos (
    id uuid PRIMARY KEY,
    cuenta_id uuid NOT NULL REFERENCES cuentas(id) ON DELETE RESTRICT,
    fecha timestamptz NOT NULL,
    tipo_movimiento varchar(20) NOT NULL CHECK (tipo_movimiento IN ('Deposito','Retiro')),
    valor numeric(18,2) NOT NULL CHECK (valor <> 0),
    saldo numeric(18,2) NOT NULL CHECK (saldo >= 0)
);
CREATE INDEX ix_movimientos_cuenta_fecha ON movimientos(cuenta_id, fecha);

CREATE TABLE inbox_messages (
    event_id uuid PRIMARY KEY,
    received_at_utc timestamptz NOT NULL
);

-- Los UUID de las cuentas se generan en cada inicialización.
SELECT
    gen_random_uuid() AS account_478758_id,
    gen_random_uuid() AS account_225487_id,
    gen_random_uuid() AS account_495878_id,
    gen_random_uuid() AS account_496825_id
\gset

-- Proyección inicial de clientes. Luego se sincroniza asíncronamente mediante RabbitMQ.
-- Se reutilizan los mismos cliente_id generados para clients_db para conservar la identidad entre microservicios.
INSERT INTO client_snapshots(cliente_id,nombre,estado,updated_at_utc) VALUES
(:'jose_lema_id','Jose Lema',true,now()),
(:'marianela_montalvo_id','Marianela Montalvo',true,now()),
(:'juan_osorio_id','Juan Osorio',true,now());

-- Estado inicial de las cuentas. Los movimientos se registran desde la API o Postman.
INSERT INTO cuentas(id,numero_cuenta,tipo_cuenta,saldo_inicial,saldo_disponible,estado,cliente_id,created_at_utc,updated_at_utc) VALUES
(:'account_478758_id','478758','Ahorros',2000,2000,true,:'jose_lema_id',now(),now()),
(:'account_225487_id','225487','Corriente',100,100,true,:'marianela_montalvo_id',now(),now()),
(:'account_495878_id','495878','Ahorros',0,0,true,:'juan_osorio_id',now(),now()),
(:'account_496825_id','496825','Ahorros',540,540,true,:'marianela_montalvo_id',now(),now());

\echo Identificadores generados para los datos iniciales:
\echo '  Jose Lema clienteId=' :jose_lema_id
\echo '  Marianela Montalvo clienteId=' :marianela_montalvo_id
\echo '  Juan Osorio clienteId=' :juan_osorio_id
\echo '  478758 cuentaId=' :account_478758_id
\echo '  225487 cuentaId=' :account_225487_id
\echo '  495878 cuentaId=' :account_495878_id
\echo '  496825 cuentaId=' :account_496825_id
