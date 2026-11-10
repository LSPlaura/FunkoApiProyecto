# Ampliación de pruebas y cobertura

## Referencias del profesor

- [Tema 28: testing](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/28-testing.md):
  AAA (28.5), NUnit (28.6), FluentAssertions (28.7), Moq y Verify/Times (28.8),
  cobertura y evitar pruebas triviales (28.13–28.15).
- [10-ProductosTest: ProductoServiceTests](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/10-ProductosTest/ProductosTest.Test/Services/ProductoServiceTests.cs):
  preparación de mocks, éxito/error con Result y comprobación de llamadas.
- [ProductosControllerTests de TiendaDawApi](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests/Unit/Controllers/ProductosControllerTests.cs):
  casos de error, imagen, paginación y sección Authorization Attribute Tests.
- [IdentityAuthServiceTests](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests/Unit/Services/Auth/IdentityAuthServiceTests.cs):
  Identity real con EF InMemory y contraseñas válidas/inválidas, sin simular SignInManager.
- [Tema 24: documentación](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/24-documentacion.md):
  contratos de entrada/salida, JWT y formatos de error.
- [22-ProductosComplete: ProductoRepositoryTests](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/22-ProductosComplete/ProductosTest.Test/Services/ProductoRepositoryTests.cs):
  persistencia, lecturas y relaciones desde la base de datos, con preparación y limpieza por test.

**Adaptación:** se conservan las carpetas y el SetUp del proyecto, sus DTO, sus errores y
sus respuestas HTTP reales. No se copia la arquitectura completa de TiendaDawApi.

## Qué se ha ampliado

Se pasa de **417 a 476 casos ejecutados sin Docker**: 59 casos adicionales.

| Archivo en FunkoApi.Tests | Comprobaciones añadidas |
| --- | --- |
| Controllers/AuthorizationAttributesTests.cs | Permisos de las 22 operaciones: roles de categorías, Funkos y proveedores; login/signup públicos; perfil autenticado sin rol concreto. |
| Controllers/AuthControllerTests.cs | Rechazo de Identity traducido a 422 y conservación del mensaje. |
| Controllers/CategoriasControllerTests.cs | Duplicado al actualizar y categoría inexistente al borrar. |
| Controllers/ProveedoresControllerTests.cs | Borrado inexistente (404) y resultado false (200 con booleano). |
| Controllers/FunkosControllerTests.cs | Errores de relaciones e imágenes, 400/409/422 al actualizar, rechazo de archivo sin actualizar la entidad, y paginación con datos y enlaces. |
| Services/Funko/FunkoServicesTests.cs | Lecturas desde caché sin consultar repositorio, creación con archivo vacío e imagen predeterminada, y actualización con mismo nombre sin archivo o con archivo vacío. |
| Services/Auth/AuthServiceTests.cs | Contraseñas que incumplen cada requisito de Identity y perfil de un dato existente sin email. |
| Storage/FileSystemStorageServiceTests.cs | Tamaño máximo configurable: por debajo, igual y por encima del límite, comprobando los archivos escritos. |
| Infrastructure/SwaggerDocumentationTests.cs | Campos exactos de los DTO de salida, sin contraseñas; perfil sin restricción de rol; 415 y ProblemDetails en entradas JSON. |

Los casos nuevos con dependencias simuladas verifican las llamadas relevantes con Verify/Times,
incluidas las operaciones que no deben ejecutarse. Las pruebas de almacenamiento crean y limpian
su propio directorio temporal. No se ha modificado código de producción ni añadido paquetes.

## Ejecución inicial sin Docker y resultado

```powershell
dotnet test FunkoApi.Tests/FunkoApi.Tests.csproj --filter "TestCategory!=Integration" --collect:"XPlat Code Coverage" --results-directory TestResults/CoberturaAmpliada
```

Resultado comprobado en el PC de casa:

- Compilación correcta y **476 pruebas correctas, 0 fallidas y 0 omitidas**.
- Los tests con categoría Integration se excluyen del comando, no se cuentan como omitidos.
- **Cobertura global sin exclusiones nuevas:** 58,15 % de líneas (1773/3049)
  y 55,87 % de ramas (352/630).
- Antes: las mismas 1773 líneas y 350 ramas. Los nuevos casos amplían los escenarios
  comprobados aunque muchos recorran líneas que otros tests ya ejecutaban.
- Los cuatro controladores mantienen **100 % de líneas y ramas** en esta medición.
- Informe: `TestResults/CoberturaAmpliada/eb7d9d6f-29ce-44d2-880c-daa50e06e5c1/coverage.cobertura.xml`.
  La carpeta identificadora cambia en cada ejecución.
- Siguen los avisos de NuGet, incluido SSH.NET 2024.2.0 con vulnerabilidades de gravedad alta.
  No se han cambiado sus dependencias.

## Límites de la medición inicial

**No se ha alcanzado ni certificado el objetivo global >80 % del tema 28.** El informe incluye
código generado de OpenAPI y código de inicio, además del código que necesita integración.
No se han añadido exclusiones para aparentar una cobertura mayor.

En la ampliación inicial se dejó Docker pendiente por decisión de la responsable. Posteriormente
se ha autorizado la ejecución de integración aislada, descrita en el apartado siguiente.
No se sustituyen las transacciones reales por EF InMemory.

Las llamadas directas a controladores no ejecutan el pipeline HTTP: no prueban el binding,
la validación automática de ApiController, la negociación de formatos ni la aceptación real
de tokens. Los tests de atributos verifican permisos declarados, no la respuesta 401/403 por HTTP.
Las comprobaciones de Swagger verifican documentación, no que todos sus códigos se produzcan
correctamente por HTTP. Esa integración sigue pendiente.

El 100 % del controlador **no corrige ni descarta** los hallazgos funcionales de
[la auditoría](Auditoria-Endpoints-OpenAPI.md), especialmente el PATCH de imagen.
No se ha arrancado la API ni enviado correo ni usado las bases de datos del proyecto.

## Revisión de las ocho clases con integración

La responsable ha autorizado comprobar Docker y ejecutar la suite completa con Testcontainers.
Los tests crean y eliminan bases de datos y contenedores temporales propios; no utilizan las
conexiones de las bases del proyecto. Docker respondió con la versión 29.9.2.

La primera medición completa (`TestResults/RevisionIntegracion01`) pasó **557 pruebas** y
mostró que las ocho clases señaladas ya tenían **100 % de líneas** al ejecutar los tests
de integración existentes. No era necesario duplicar sus pruebas ni crear tests triviales
del constructor de Funko: las consultas de EF Core ya lo ejecutan.

En esa medición, siete clases tenían también 100 % de ramas:
EfcSeeder, DatabaseInitializationExtensions, Funko, CategoriaRepository, FunkoRepository,
FunkoServices y FunkoTransactionsService. ProveedorMongoRepository tenía 100 % de líneas
y 50 % de ramas: no se ejecutó el caso `IsAcknowledged == false` de Update (línea 44).
No se certifica esa rama como probada por haber pasado los CRUD con MongoDB real.

### Casos adicionales de esta revisión

- `Data/Seeders/EfcSeederTests.cs`: comprueba que tras sembrar se puede crear otro Funko
  sin colisión de ID y que una categoría inicial eliminada (Marvel, Star Wars o Anime)
  no se duplica ni permite sembrar Funkos sin esa categoría activa.
- `Repositories/Proveedor/ProveedorMongoRepositoryTests.cs`: amplía la comprobación de
  identificadores mal formados a ExistsById y SoftDelete, además de GetById.
- `Repositories/Funko/FunkoRepositoryTests.cs`: refuerza GetById para comparar todos los
  datos recuperados y comprobar que se materializa una instancia distinta de la guardada.
- `README.md`: corrige los comandos para seleccionar tests por TestCategory; los namespaces
  actuales no contienen necesariamente Unit/Integration y no hay un mecanismo de omisión
  basado en la variable SKIP_INTEGRATION_TESTS.

Estas pruebas conservan el estilo AAA y la infraestructura de Testcontainers existente.
No se ha cambiado código de producción ni añadido dependencias.

### Resultado final comprobado

- Suite completa tras los cambios: **565 pruebas correctas, 0 fallidas, 0 omitidas**.
  Son ocho casos adicionales respecto a las 557 pruebas de la primera ejecución completa.
- Las ocho clases conservan **100 % de líneas**. Las siete clases anteriores mantienen
  también 100 % de ramas; ProveedorMongoRepository mantiene pendiente la rama indicada.
- Cobertura global, sin exclusiones nuevas: **69,36 % de líneas** (2115/3049)
  y **63,17 % de ramas** (398/630). No es el porcentaje individual de estas ocho clases.
- Informe final de esta revisión:
  `TestResults/RevisionIntegracion02/38809fe3-86a0-4177-8d5f-1ffb09fc7884/coverage.cobertura.xml`,
  relativo a la raíz de la solución.
- Para abrir ese informe concreto desde FunkoApi.Tests, sin volver a ejecutar los tests:

```powershell
reportgenerator -reports:"../TestResults/RevisionIntegracion02/**/coverage.cobertura.xml" -targetdir:"coveragereport/Integracion02" -reporttypes:Html
start coveragereport/Integracion02/index.html
```

Para generar una medición completa nueva **desde FunkoApi.Tests**, con Docker disponible:

```powershell
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults/IntegracionNueva01
reportgenerator -reports:"TestResults/IntegracionNueva01/**/coverage.cobertura.xml" -targetdir:"coveragereport/IntegracionNueva01" -reporttypes:Html
start coveragereport/IntegracionNueva01/index.html
```

Ejecutar ReportGenerator solo si terminan correctamente los tests. Usar otra carpeta nueva
en cada medición. **No añadir `TestCategory!=Integration`** si se quiere medir estas ocho
clases con sus pruebas de bases de datos y transacciones.
