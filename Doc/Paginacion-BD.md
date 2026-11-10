# Paginación en base de datos

Los tres endpoints REST paginados usan el formato existente `data/pagination`
y la cabecera `Link` para navegar entre páginas:

| Endpoint | Roles |
| --- | --- |
| `GET /api/Funkos/paged?page=1&pageSize=10` | ADMIN, USER |
| `GET /api/Categorias/paged?page=1&pageSize=10` | ADMIN, USER |
| `GET /api/Proveedores/paged?page=1&pageSize=10` | ADMIN |

`page` se ajusta a un mínimo de 1 y `pageSize` al intervalo 1–100.
Sin parámetros se utiliza página 1 y tamaño 10. Una página posterior a la
última devuelve una colección vacía conservando el total de registros.

## Funcionamiento

- Funkos y categorías: el repositorio ejecuta `CountAsync()` para el total y
  `OrderBy` → `Skip` → `Take` → `ToListAsync()` para recuperar solo la página
  desde PostgreSQL, con `AsNoTracking()`. Los funkos conservan su categoría.
- Proveedores: el repositorio cuenta con `CountDocumentsAsync()` y recupera
  la página con `SortBy` → `Skip` → `Limit` → `ToListAsync()` desde MongoDB.
- El servicio normaliza los parámetros, convierte los elementos a DTOs y
  calcula `TotalPages`. No carga el listado completo para recortarlo.
- El desplazamiento se calcula directamente con `(page - 1) * pageSize`,
  como en los apuntes, sin variable auxiliar ni devolución anticipada.
  Este cálculo con `int` no cubre desplazamientos mayores que `Int32.MaxValue`.
  En proveedores no se establece un orden adicional para nombres repetidos.

Se mantienen los GET completos anteriores por compatibilidad. Para aprovechar
esta optimización, los clientes deben utilizar las rutas `/paged`. No se ha
cambiado GraphQL, el borrado lógico, los índices ni la caché. Estos listados
siguen incluyendo los eliminados, como antes de este cambio.

Esta ampliación añade dos operaciones: el backend pasa de 22 a 24 operaciones
REST. La auditoría anterior de endpoints describe el alcance previo.

## Material del profesor utilizado

- [UD02, 23.2.5: paginación EF Core](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/23-optimizacion.md#2325-paginación).
- [UD02, 13.3.6: consultas y paginación con el driver MongoDB](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/13-mongodb-cassandra.md#1336-consultar-documentos).
- [Repositorio del ejemplo 21-ProductosOptimization](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/21-ProductosOptimization/ProductosTest/Repositories/ProductoRepository.cs): organización del acceso a datos y carga de categoría.
- [ProductoService.GetPaged del ejemplo 21](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/21-ProductosOptimization/ProductosTest/Services/ProductoService.cs): tupla de resultados, normalización y cálculo de páginas.

Para las consultas se reproduce el procedimiento de los apartados 23.2.5 y
13.3.6: contar, aplicar `Skip/Take` o `Skip/Limit` y materializar al final.
Se conserva la conexión con las interfaces, DTOs y respuestas de FunkoApi.
No se añaden el desplazamiento auxiliar con `long`, el retorno anticipado ni
el desempate por ID de la primera implementación.

Nota sobre las referencias: el `GetPaged` del ejemplo 21 todavía pagina una
lista en memoria. Se utiliza como referencia de la tupla y los metadatos,
no de la consulta, porque la tarea solicitada es paginar en la base de datos,
tal como muestra el apartado 23.2.5.
