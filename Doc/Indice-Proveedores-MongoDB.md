# Índice de proveedores en MongoDB

`ProveedorMongoRepository` crea un índice ascendente **no único** sobre
`Proveedor.Nombre`, almacenado como `nombre` en BSON.

- Nombre del índice: `idx_proveedor_nombre`.
- Consultas que lo justifican: `GetAll()` y `GetPaged()`, que ordenan por nombre.
- Se siguen permitiendo proveedores con nombres repetidos.
- No se han añadido índices de otros campos ni se ha modificado PostgreSQL.

## Procedimiento del profesor

- [TiendaDawApi, apartado 9.6](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/doc/09-mongodb-cassandra.md#96-repositorios-con-mongodb-driver): método `CreateIndexes()`, llamado desde el constructor, con `IndexKeys`, `CreateIndexModel` y nombre del índice.
- [UD02, apartado 13.3.10](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/13-mongodb-cassandra.md#13310-índices): índice simple ascendente y creación síncrona con `CreateOne()`.

Se combinan esos dos fragmentos: la organización del primero y la llamada
síncrona del segundo, sin dejar una tarea de creación sin esperar. Se cambia
la entidad y el campo por los de proveedores y no se establece `Unique`.

La creación se ejecuta al construir el repositorio, tanto en desarrollo como
en producción. Como el repositorio está registrado como Scoped, se solicita
también en cada nueva instancia. La API necesita acceso a MongoDB y permiso
para crear índices; un fallo se propaga, no se oculta.

La implementación no borra ni recrea la base de datos. No obstante, el arranque
de Development ya existente sí la elimina: no es necesario arrancarlo solo
para aplicar este índice a una base de datos con datos que se quieran conservar.

Las pruebas comprueban la definición del índice, su creación real en un
MongoDB de Testcontainers, la repetición de la creación sin duplicarlo y que
se pueden insertar nombres repetidos. No se ha medido una mejora de tiempos.
