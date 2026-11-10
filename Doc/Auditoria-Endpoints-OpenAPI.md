# Auditoría de controladores y contrato HTTP

## Alcance y criterio

Revisión del código de los **4 controladores y sus 22 operaciones REST**, los DTO de entrada/salida,
servicios, repositorios, almacenamiento, autorización y traducción de errores.
La auditoría inicial indica como base el estado local derivado de `c1159e8`.
Al retomar el trabajo en el PC de casa se han revisado los controladores, el filtro y las pruebas
documentales presentes en esta copia; las comprobaciones de esta continuación figuran al final.

Se documenta **lo que hace el código actual**, no un contrato ideal. La auditoría inicial no modificó rutas,
permisos, validaciones, retornos, servicios ni acceso a datos. En el cierre se ha separado la ruta
de búsqueda de Funkos por nombre a petición de Lucia; se detalla al final. No se han enviado correos ni
ejecutado peticiones que creen, actualicen o borren datos de la API desplegada.

### Referencias del profesor consultadas

- [UD02, 24-documentacion.md](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/24-documentacion.md):
  24.2.4 (XML y configuración), 24.3 (operaciones/respuestas), 24.4 (DTO), 24.5 (JWT),
  24.6 (errores) y 24.9 (filtros).
- [Ejemplo Program.cs de 22-ProductosComplete](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/22-ProductosComplete/ProductosTest/Program.cs):
  SwaggerDoc, JWT, lectura protegida del XML y Swagger solo en desarrollo.
- [Ejemplo ProductosController.cs](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/22-ProductosComplete/ProductosTest/Controllers/ProductosController.cs):
  ActionResult, CreatedAtAction, formularios, PATCH de imagen y paginación.
- [UD02, 04-controladores-mvc.md](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/04-controladores-mvc.md):
  fuentes de binding y validación de ApiController.

**Adaptación al proyecto:** el profesor prioriza ProblemDetails, pero esta API no lo utiliza
para todos sus errores. No se han cambiado las respuestas para hacerlas coincidir con el ejemplo:
Swagger describe los formatos existentes mediante esquemas documentales y un filtro.
La sintaxis de Microsoft.OpenApi se adapta a la versión **2.7.5** del proyecto;
no se copia el namespace antiguo `Microsoft.OpenApi.Models` de los ejemplos.

## Cómo consultar la documentación

Con la API **compilada con estos cambios** y ejecutándose en Development:

- UI interactiva: `/swagger/index.html`.
- Documento generado: `/swagger/v1/swagger.json`.
- En Docker con el puerto del compose actual: `http://localhost:5000/swagger/index.html`.

El contenedor ya iniciado no incorpora automáticamente estos cambios del código fuente.
No se ha reconstruido ni reiniciado aquí: el arranque de esta aplicación ejecuta inicializadores
de almacenamiento y bases de datos que deben revisarse antes de hacerlo sobre datos existentes.

Se ha añadido `Swashbuckle.AspNetCore.Annotations` **10.3.1**, alineado con Swashbuckle **10.3.1**,
para usar SwaggerOperation/SwaggerResponse como en el apartado 24.
Los comentarios XML se generan al compilar y se cargan solo si el archivo existe.

## Entradas y salidas comunes

- Las rutas de las tablas se muestran en minúsculas. El documento conserva los nombres de
  controlador (`/api/Auth`, `/api/Categorias`, `/api/Funkos`, `/api/Proveedores`).
- `Authorization: Bearer <token>` para las operaciones protegidas. En el botón Authorize
  se introduce **solo el token**, porque se ha definido un esquema HTTP bearer.
- `login` y `signup` son públicos; su requisito de seguridad se deja explícitamente vacío.
- El cuerpo JSON se usa en Auth, categorías y proveedores. Funkos recibe formulario
  `multipart/form-data`, no el JSON de CreateFunkoDto/UpdateFunkoDto.
- Salida habitual: JSON. `Program.cs` también registra un formateador XML y respeta Accept.
  Que Swagger muestre XML **no prueba** que todos los records y objetos anónimos sean serializables
  con ese formateador; su compatibilidad completa no se ha certificado.
- No hay endpoint de borrado físico en estos controladores. No se documentan rutas inventadas
  por existir métodos HardDelete en los servicios.

### Modelos de salida (nombres JSON)

| Modelo | Campos |
| --- | --- |
| AuthResponseDto | `token: string`, `email: string`, `role: string`, `expiresAt: date-time` UTC |
| UserDto | `id: int64`, `email: string`, `nombre: string`, `apellidos: string`, `role: string` |
| CategoriaDto | `id: string`, `nombre: string`, `isDeleted: boolean`, `createdAt: date-time`, `updatedAt: date-time` |
| ProveedorDto | `id: string` (ObjectId), `nombre: string`, `isDeleted: boolean`, `createdAt: date-time`, `updatedAt: date-time` |
| FunkoDto | `id: int64`, `nombre: string`, `precio: decimal`, `stock: int32`, `imagen: string`, `categoria: CategoriaDto`, `isDeleted: boolean`, `createdAt: date-time`, `updatedAt: date-time` |
| PagedResponse de FunkoDto | `data: FunkoDto[]`, `pagination: { page, pageSize, totalPages, totalItems }` (enteros) |

FunkoDto **no devuelve proveedor**, aunque las operaciones de escritura exigen proveedorId.
La propiedad imagen es una referencia relativa, no un archivo binario en la respuesta.
Los identificadores y fechas se generan en servidor, no se reciben en los DTO de creación.

## Inventario completo

En la columna de errores se indican los propios de cada flujo. Además se aplican los errores
transversales del apartado siguiente cuando se produce una excepción no tratada.
`ADMIN/USER` significa cualquiera de los dos roles, no ambos a la vez.

### AuthController (3 operaciones)

| Método y ruta | Acceso | Entrada | Éxito | Errores propios |
| --- | --- | --- | --- | --- |
| POST `/api/auth/login` | Público | JSON LoginDto | 200 AuthResponseDto | 400 validación/binding; 401 credenciales inválidas |
| POST `/api/auth/signup` | Público | JSON SignupDto | 200 AuthResponseDto, rol USER | 400 validación/binding; 409 email duplicado; 422 rechazo de Identity |
| GET `/api/auth/me` | JWT válido, cualquier rol | Claim NameIdentifier del JWT | 200 UserDto | 401 token; 404 usuario inexistente |

**LoginDto:** email obligatorio con formato email; password obligatoria entre 8 y 100 caracteres.
**SignupDto:** lo anterior más nombre obligatorio hasta 50 caracteres y apellidos obligatorios
hasta 100. Identity exige mayúscula, minúscula, dígito y carácter no alfanumérico.
Los campos DTO fallan con 400; la regla de complejidad que rechaza Identity se transforma en 422.
Signup responde **200**, no 201: se documenta la implementación existente.
Los DTO de salida no incluyen contraseña, hash ni otros campos de Identity.

Ejemplo ficticio de registro (no son credenciales reales):

```json
{"email":"alumna@example.com","password":"Ejemplo123!","nombre":"Alumna","apellidos":"De Prueba"}
```

### CategoriasController (6 operaciones)

| Método y ruta | Acceso | Entrada | Éxito | Errores propios |
| --- | --- | --- | --- | --- |
| GET `/api/categorias` | ADMIN/USER | Ninguna | 200 CategoriaDto[] | Sin error de dominio explícito |
| GET `/api/categorias/{id}` | ADMIN/USER | ID textual en ruta | 200 CategoriaDto | 404 inexistente |
| GET `/api/categorias/nombre?name=Marvel` | ADMIN/USER | name obligatorio en query | 200 CategoriaDto | 400 query ausente; 404 inexistente |
| POST `/api/categorias` | ADMIN | JSON CreateCategoriaDto | 201 CategoriaDto + Location | 400 DTO/JSON; 409 nombre duplicado |
| PUT `/api/categorias/{id}` | ADMIN | ID textual + JSON UpdateCategoriaDto | 200 CategoriaDto | 400 DTO/JSON; 404 inexistente; 409 nombre duplicado |
| DELETE `/api/categorias/{id}` | ADMIN | ID textual | 200 booleano true | 400 tiene Funkos asociados o falla el borrado; 404 inexistente |

CreateCategoriaDto y UpdateCategoriaDto reciben solo `nombre`, obligatorio y máximo 100 caracteres.
Ejemplo: `{"nombre":"Marvel"}`. El ID **no es** un ObjectId de MongoDB.
Las consultas del repositorio no filtran IsDeleted. El control de dependencias considera
cualquier Funko asociado, también los borrados lógicamente.

### ProveedoresController (5 operaciones)

| Método y ruta | Acceso | Entrada | Éxito | Errores propios |
| --- | --- | --- | --- | --- |
| GET `/api/proveedores` | ADMIN | Ninguna | 200 ProveedorDto[] | Sin error de dominio explícito |
| GET `/api/proveedores/{id}` | ADMIN | ObjectId textual | 200 ProveedorDto | 404 inexistente; 500 si el ObjectId tiene formato inválido |
| POST `/api/proveedores` | ADMIN | JSON CreateProveedorDto | 201 ProveedorDto + Location | 400 DTO/JSON |
| PUT `/api/proveedores/{id}` | ADMIN | ObjectId textual + JSON UpdateProveedorDto | 200 ProveedorDto | 400 DTO/JSON; 404 inexistente; 500 ObjectId inválido |
| DELETE `/api/proveedores/{id}` | ADMIN | ObjectId textual | 200 booleano | 404 inexistente; 409 tiene Funkos asociados; 500 ObjectId inválido |

CreateProveedorDto y UpdateProveedorDto reciben solo `nombre`, obligatorio y máximo 100 caracteres.
Ejemplo: `{"nombre":"Distribuciones Coleccionables"}`. ObjectId: 24 caracteres hexadecimales,
por ejemplo `000000000000000000000001` (solo formato de ejemplo, no garantiza existencia).
No hay comprobación de nombre duplicado en el servicio de proveedores. No se promete un 409
por duplicidad de nombre. La lista y la consulta por ID incluyen eliminados lógicamente.
Un segundo DELETE puede responder **200 false** porque el repositorio mide ModifiedCount.

### FunkosController (8 operaciones)

| Método y ruta | Acceso | Entrada | Éxito | Errores propios |
| --- | --- | --- | --- | --- |
| GET `/api/funkos` | ADMIN/USER | Ninguna | 200 FunkoDto[] | Sin error de dominio explícito |
| GET `/api/funkos/{id}` | ADMIN | Número en ruta, acción Int32 | 200 FunkoDto | 400 fuera de rango Int32; 404 inexistente |
| GET `/api/funkos/nombre/{nombre}` | ADMIN | Nombre exacto en ruta | 200 FunkoDto | 404 inexistente |
| GET `/api/funkos/paged` | ADMIN/USER | page=1, pageSize=10 en query | 200 PagedResponse de FunkoDto + Link cuando procede | 400 si no se pueden convertir a entero |
| POST `/api/funkos` | ADMIN | Formulario descrito abajo | 201 FunkoDto + Location | 400 binding o relaciones inexistentes; 409 duplicado/fallo capturado al crear; 422 imagen inválida; 500 almacenamiento/ObjectId inválido |
| PUT `/api/funkos/{id}` | ADMIN | Int32 en ruta + formulario | 200 FunkoDto | 400 binding, relaciones o imagen ausente; 404 inexistente; 409 duplicado; 422 imagen/transacción; 500 almacenamiento/ObjectId inválido |
| DELETE `/api/funkos/{id}` | ADMIN | Int32 en ruta | 200 booleano | 400 fuera de rango Int32; 404 inexistente o ya eliminado si no queda en caché |
| PATCH `/api/funkos/{id}/imagen` | ADMIN | Int64 en ruta + archivo | 200 FunkoDto si completa el flujo | 400 binding o relaciones; 404 inexistente; 409 conflicto; 422 archivo/transacción; 500 almacenamiento o proveedor vacío; véase F-01 |

**Formulario de POST/PUT:** `nombre: string`, `precio: decimal`, `stock: int32`,
`categoriaId: string`, `proveedorId: string` (ObjectId), `archivo: binary`.
POST admite archivo ausente o vacío y usa imagen predeterminada. PUT devuelve 400 si la
imagen está ausente o vacía, a pesar del parámetro nullable. PATCH exige el campo archivo.

**Validación efectiva:** el binding valida conversiones y cadenas no nullable; los atributos
del DTO creado manualmente no se ejecutan automáticamente. Las reglas declaradas en ese DTO
son nombre máximo 100, precio entre 0.01 y 999999.99 y stock entre 1 e Int32.MaxValue.
**No se publican como garantías de validación del formulario**, porque no lo son actualmente.

**Archivo:** appsettings.json configura 5 MiB (5242880 bytes), extensiones .jpg/.jpeg/.png/.gif
y MIME image/jpeg, image/png, image/gif. El servicio comprueba tamaño, extensión, MIME y
nombre; la configuración puede variar por entorno. ValidateFile no comprueba Length == 0;
la detección previa de archivo vacío depende del flujo del servicio.

**Paginación:** page se ajusta como mínimo a 1; pageSize se ajusta al intervalo 1..100.
Una página superior a la última no se limita al máximo: puede devolver data vacío.
Link contiene first/prev si page > 1 y next/last si page < totalPages.
Los listados y la paginación incluyen eliminados lógicamente; el repositorio de consulta por
ID/nombre sí los excluye, aunque la caché puede alterar la observación.

**Notificaciones:** crear (con/sin imagen) avisa por SignalR y encola email. El borrado lógico
encola email solo con resultado true. PUT/PATCH no encolan correo. El destinatario está fijado
en FunkoServices, no se obtiene del usuario que hizo la petición. El éxito HTTP no garantiza
la entrega SMTP: el worker registra el error y no reintenta el mensaje.

## Formatos y estados de error

### Errores de dominio

`Extensions/DomainErrorExtensions.cs` devuelve objetos anónimos que se documentan mediante
MessageErrorResponse; el DTO documental **no sustituye la respuesta real**:

```json
{"message":"No se encontró el recurso solicitado"}
```

Mapeo: NotFoundError -> 404; BadRequestError -> 400; ConflictError -> 409;
BusinessRuleError -> 422; UnauthorizedError -> 401; cualquier otro -> 500 genérico.
ForbiddenError **no tiene rama** y caería en 500, aunque su comentario lo describa como 403.
El 403 de los endpoints actuales procede de la autorización por roles del framework.

BadRequestError.ValidationError tiene otra forma (`message` y `errors` con diccionario de arrays),
pero no se ha encontrado una producción explícita de ese tipo en los servicios de estos endpoints.
Su existencia no justifica anunciarlo como respuesta de cada operación.

### Validación automática de ApiController

400 con ValidationProblemDetails: `type`, `title`, `status`, `errors` y, según el framework,
extensiones como `traceId`. No es el mismo cuerpo que el error de dominio.
Para los cuerpos JSON, 415 puede responder ProblemDetails por Content-Type no admitido.
Las respuestas de validación se documentan en application/problem+json.

### Excepciones no tratadas

`Middleware/ExceptionHandlerMiddleware.cs` escribe JSON:

```json
{
  "errorId":"a1b2c3d4",
  "status":500,
  "message":"Ha ocurrido un error interno",
  "timestamp":"2026-11-09T12:00:00.0000000Z",
  "path":{"value":"/api/proveedores/identificador-invalido","hasValue":true},
  "method":"GET"
}
```

`path` es actualmente un objeto: el middleware serializa `HttpContext.Request.Path`
(`PathString`), no su conversión a cadena. `RequestPathResponse` reproduce sus campos
`value` y `hasValue` exclusivamente para documentar la salida existente. Si `value` es
null, el middleware lo omite. No se ha cambiado el middleware para obtener otro formato.

Mapeo transversal: ArgumentException -> 400; UnauthorizedAccessException -> 401;
TimeoutException -> 408; DbUpdateException -> 409; cualquier otra excepción -> 500.
Estos estados **solo se producen así si la excepción llega al middleware**. Por ejemplo,
el catch al crear un Funko convierte sus excepciones en ConflictError y responde 409.
FormatException no es ArgumentException: un ObjectId inválido puede acabar en 500.

401 del manejador JWT y 403 de roles normalmente no tienen cuerpo. Un 406 puede producirse
si Accept exige un formato sin formateador. También pueden intervenir límites del servidor,
proxy o transporte que no están definidos como contrato de estos controladores.

El filtro documental conserva los éxitos de cada acción y añade los errores transversales.
Si un estado puede tener cuerpo de dominio o excepción, usa `anyOf`, no inventa un único
ProblemDetails. Los posibles rechazos de validación automática se muestran por separado.
Al combinar respuestas se conserva el esquema declarado por la acción: por ejemplo, el 400
de signup combina ValidationProblemDetails y ExceptionErrorResponse para application/json,
no MessageErrorResponse. El formato application/problem+json conserva ValidationProblemDetails.
El 401 transversal por UnauthorizedAccessException también se documenta en acciones públicas;
esto no significa que login/signup exijan JWT. Login conserva además su error de credenciales.

El 500 combina `MessageErrorResponse` y `ExceptionErrorResponse` sin duplicar referencias.
En operaciones con entradas, el 400 en `application/json` también incluye
`ValidationProblemDetails` junto a los esquemas ya declarados, sin anidar ni repetir los
`anyOf`. `application/problem+json` mantiene únicamente `ValidationProblemDetails`.

## Hallazgos pendientes (no corregidos por esta tarea)

| ID | Prioridad | Evidencia y consecuencia |
| --- | --- | --- |
| F-01 | Alta | FunkosController.UpdateImagen construye UpdateFunkoDto sin Proveedor. ProveedorMongoRepository.ExistsById convierte el valor vacío con ObjectId.Parse: puede producir 500 después de eliminar la imagen anterior y guardar la nueva. Si se superase esa comprobación, el servicio guardaría de nuevo el archivo. No es un PATCH seguro. |
| F-02 | Alta | Create/Update de Funkos construyen sus DTO dentro de la acción. ApiController no valida esas instancias: los límites de precio, stock y longitud declarados en los DTO no garantizan el contrato de entrada del formulario. |
| F-03 | Media | GetById/Delete/Update de Funkos reciben int pero sus rutas usan long y el servicio usa long. La salida expone Int64 que algunas acciones no admiten. |
| F-05 | Media | ProveedorMongoRepository y ExistAnyFunkoProveedor usan ObjectId.Parse sin validación previa. IDs mal formados producen 500 en vez de una respuesta de entrada inválida. |
| F-06 | Media | Los listados incluyen IsDeleted; consultas por ID/nombre de categorías y proveedores tampoco lo filtran. No se puede afirmar que todos los GET devuelvan solo recursos activos. |
| F-07 | Media | GlobalExceptionHandler, DomainErrorExtensions y ApiController tienen formatos distintos. ForbiddenError cae en 500 y no en 403. El profesor prioriza ProblemDetails, pero migrar el formato sería un cambio de contrato y no se ha realizado. |
| F-08 | Media | FunkoDto no incluye proveedor aunque POST/PUT lo exigen. Dificulta reconstruir una actualización a partir de la respuesta. |
| F-09 | Media | Program.cs registra AddGraphQlConfig pero no llama a MapGraphQlEndpoints. GraphQL no está publicado por ese pipeline. Fuera de los 22 endpoints de controladores; no se corrige aquí. |
| F-10 | Media | FunkoServices convierte excepciones capturadas al crear en 409 y FunkoTransactionsService convierte fallos transaccionales en 422; pueden mezclar fallos técnicos con errores del cliente y exponer mensajes de excepción. |
| F-11 | Media | GetMe convierte el claim mediante long.Parse. Un claim numérico incorrecto en un token aceptado causaría FormatException/OverflowException y 500, no un error de autenticación tipado. |
| F-12 | Media | La entrega de email no está garantizada por POST/DELETE y el worker no reintenta fallos SMTP. El error TLS observado en la sesión anterior requiere una investigación independiente. |
| F-13 | Baja | Signup devuelve 200, no 201; DELETE devuelve booleano en 200, no 204. No se considera una corrección automática pendiente: debe decidirse si se mantiene el contrato. Swagger ya documenta lo que existe. |

## Otras rutas de la aplicación

SignalR publica `/hubs/funkos` mediante MapHub y está restringido a ADMIN (ver FunkoHub).
No es una acción REST de un controlador: la negociación y los mensajes del hub no se presentan
como si fueran operaciones CRUD en Swagger. GraphQL dispone de su propio esquema; no se
transforman sus queries en endpoints REST ficticios. No hay controlador de descarga de imágenes.

## Comprobaciones y límites

- Pruebas documentales en `FunkoApi.Tests/Infrastructure/SwaggerDocumentationTests.cs`:
  generación del documento con el registro Swagger real, 22 operaciones, resúmenes, IDs únicos,
  seguridad pública/protegida, multipart y archivo binario, respuestas 201/Location,
  DELETE 200 booleano, formatos de error y paginación/Link.
- El host documental de pruebas **no ejecuta Program.cs**, seeders ni envío SMTP, y no arranca
  un servidor contra las bases de datos de la alumna.
- Generar Swagger y pasar tests no equivale a haber probado todos los códigos por HTTP.
- Se comprueba que no existen operaciones con el mismo método y plantilla al normalizar los nombres de parámetros de ruta.
- No se garantiza la serialización XML de todos los modelos ni se ha certificado el transporte
  de GraphQL/SignalR, los límites del proxy o la entrega de emails.
- Los resultados concretos de compilación y pruebas ejecutadas se comunican en la entrega.

### Continuación en el PC de casa (10 de noviembre de 2026)

- `git status --short` no mostró cambios pendientes antes de esta continuación.
- `dotnet build FunkoApi.slnx`: compilación correcta, sin errores. Se mantienen avisos de
  paquetes, comentarios XML y código existente; NuGet advierte de vulnerabilidades de
  gravedad alta en SSH.NET 2024.2.0. No se han actualizado dependencias en esta tarea.
- `dotnet test FunkoApi.Tests/FunkoApi.Tests.csproj --no-build --filter "FullyQualifiedName~SwaggerDocumentationTests"`:
  **17 pruebas correctas, 0 fallidas, 0 omitidas** tras corregir la combinación de esquemas.
- Se ha reforzado la comprobación del 400 de signup y añadido cobertura del 401 de login
  (dominio/excepción) y signup (excepción sin exigir JWT).
- Se ha corregido el README: GET de Funkos paginados exige ADMIN o USER, no es público.
  El resumen XML del PATCH ya no afirma que el flujo sea seguro mientras siga pendiente F-01.
- No se ha arrancado Program.cs, Docker ni la API, ni se han ejecutado peticiones HTTP
  contra datos reales. Los 13 hallazgos de negocio/contrato siguen pendientes; esta
   continuación corrige documentación, no certifica el funcionamiento de todos los endpoints.

### Cierre de Swagger y separación de rutas (10 de noviembre de 2026)

- **F-04 resuelto:** la búsqueda exacta por nombre pasa a
  `GET /api/Funkos/nombre/{nombre}`. Se conserva `GET /api/Funkos/{id:long}` para
  consultar por ID y `GET /api/Funkos/paged` para paginación. No se mantiene un alias
  con la plantilla antigua, porque volvería a introducir la coincidencia. Los clientes
  que buscaban por nombre deben utilizar la nueva URL; los permisos ADMIN no cambian.
- Se han actualizado README, inventario OpenAPI y las cinco peticiones Bruno de
  búsqueda por nombre (cuatro activas y una plantilla de `Resueltas`). No se han ejecutado
  peticiones E2E contra datos existentes.
- Se corrige el esquema de excepciones: `path` es `RequestPathResponse` con
  `value` y `hasValue`, como la respuesta del middleware. Las pruebas deserializan
  el JSON real del manejador con ese DTO documental, sin modificar el manejador.
- El 500 documenta dominio y excepción sin duplicados. El 400 de las operaciones
  con entradas incluye la validación automática en JSON sin perder los errores de dominio.
- Los seis cuerpos JSON se marcan como obligatorios. Login y signup documentan
  email, contraseña de 8 a 100 caracteres y campos obligatorios no nulos; signup
  conserva los máximos de nombre (50) y apellidos (100). Las restricciones de estos
  records posicionales se reflejan en el filtro, ya que el generador no estaba
  recogiendo las anotaciones situadas en los parámetros del constructor.
- No se publican como validación efectiva los rangos de los DTOs de Funkos creados
  dentro de las acciones. No se han modificado las validaciones ni la lógica de los servicios.
- Las pruebas documentales comprueban explícitamente el inventario de las 22 operaciones,
  las entradas JSON/formulario, las salidas, los esquemas corregidos y la ausencia de
  plantillas equivalentes por método HTTP.
- Los otros hallazgos de negocio del inventario siguen fuera de esta corrección de Swagger;
  no se declara que estén solucionados por pasar las pruebas documentales.
- Comprobaciones finales: **77 pruebas relacionadas con Swagger, DTOs documentales y
  middleware correctas**; suite sin integración, **524 correctas, 0 fallidas y 0 omitidas**.
  Compilación del proyecto de tests y su API correcta. `git diff --check` sin errores de espacios.
  Permanecen avisos existentes de paquetes y comentarios XML. No se ha reiniciado la API,
  ejecutado seeders ni realizado una ejecución de Bruno o de integración en este cierre.
- Guía: apartados 24.3, 24.4, 24.6 y 24.9 del material enlazado al principio y las rutas
  con segmento literal del `ProductosController` de `22-ProductosComplete`. La ruta
  `nombre/{nombre}` es la adaptación a esta API; no se atribuye ese nombre exacto al ejemplo.
