# Consultas y borrado lógico

## Regla de lectura

Las lecturas normales de Funkos, categorías y proveedores devuelven solo
registros con `IsDeleted = false`. Esto incluye listados completos, páginas,
sus totales, búsquedas por ID y nombre y comprobaciones de ID para asociar
categorías y proveedores a un Funko.

- EF Core: `HasQueryFilter(f => !f.IsDeleted)` y
  `HasQueryFilter(c => !c.IsDeleted)` en `AppDbContext`.
- MongoDB: `!p.IsDeleted` explícito en `Find` y `CountDocumentsAsync`.
- Las consultas GraphQL que usan esos repositorios y el DataLoader de
  categorías quedan sujetas a los mismos filtros de EF Core.
- Un segundo borrado lógico de un registro ya eliminado no lo modifica.
  En MongoDB se actualiza también `UpdatedAt`, como en PostgreSQL.
- `FunkoRepository.Update` consulta con `FirstOrDefaultAsync` para aplicar
  el filtro en BD también después de `SoftDelete` en el mismo contexto.
  La prueba `Update_TrasSoftDeleteEnMismoContexto_NoActualizaEliminado`
  cubre este caso sin limpiar previamente el seguimiento.

El borrado lógico conserva el registro. Para inspeccionar físicamente los
datos de PostgreSQL en pruebas se usa `IgnoreQueryFilters()`; en MongoDB se
consulta la colección directamente.

## Excepciones internas deliberadas

No se cambia el diseño de índices ni las reglas existentes de integridad:

- `ExitsName` y `ExistsByName` ignoran el filtro porque el índice único de
  nombre sigue incluyendo eliminados. Un nombre eliminado continúa reservado.
- Las comprobaciones de funkos asociados a categoría o proveedor usan
  `IgnoreQueryFilters()`: también cuentan funkos eliminados lógicamente,
  que siguen existiendo y conservando sus referencias.
- Los métodos de borrado físico del repositorio pueden recuperar registros
  sin el filtro. No se crean endpoints nuevos de papelera o restauración,
  ni se amplía el alcance de los métodos de los servicios.

## Caché

Al borrar un Funko se invalidan tanto su clave por ID como su clave por nombre.
Así una búsqueda por nombre cacheada antes del borrado no sigue mostrándolo.
Se conserva el resto del diseño de caché. No se vacía Redis al desplegar este
cambio: entradas antiguas creadas por la versión previa pueden persistir hasta
su expiración.

## Referencias del profesor

- [AppDbContext, ejercicio 21](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/21-ProductosOptimization/ProductosTest/Entity/AppDbContext.cs): `HasQueryFilter`.
- [ProductoRepository, ejercicio 21](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/21-ProductosOptimization/ProductosTest/Repositories/ProductoRepository.cs): lecturas normales y `IgnoreQueryFilters` para acceso explícito a eliminados.
- [ProductoNativeRepository, ejercicio 12](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/12-ProductosMongo/ProductosMongo/Repositories/Native/ProductoNativeRepository.cs): filtros explícitos de `IsDeleted` y actualización de la fecha de borrado.
- [UD02, 23.3.5](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/23-optimizacion.md#2335-invalidación-de-caché): invalidación de entradas tras modificar datos.

Los filtros se aplican a las tres entidades de FunkoApi. Las excepciones de
unicidad y relaciones conservan las restricciones de este proyecto; no son
funcionalidades adicionales copiadas del ejemplo del profesor.
