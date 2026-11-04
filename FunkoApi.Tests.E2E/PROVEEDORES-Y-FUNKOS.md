# Proveedores y Funkos: pruebas E2E

## Estado y uso

Se han escrito 23 peticiones en `3 - PROVEEDORES` y 49 en `4 - FUNKOS`.
Hay otras 7 peticiones fuera de la coleccion activa, en `Pendientes/Funkos`.
Son resultados **esperados segun el codigo revisado**, no una ejecucion
confirmada. Se han revisado estaticamente los archivos, roles y orden.
Primero se comprobaron los tests de los cambios de la API. Despues se
corrigio la seccion de configuracion de StorageInitializationExtensionsTests
y se ejecuto toda la suite:

```powershell
dotnet test "FunkoApi.Tests/FunkoApi.Tests.csproj" --no-restore --logger "trx;LogFileName=revision-completa.trx" --results-directory "FunkoApi.Tests/TestResults/Revision"
```

Resultado: **387 pruebas correctas, 0 fallidas y 0 omitidas**, incluidos
los 79 tests de integracion. Los tests directos de controlador no comprueban
el binding de la ruta ni la autorizacion del pipeline HTTP.
Se mantienen avisos de compilacion/paquetes; no se han modificado paquetes.
La CLI 4.3.0 ya ha ejecutado YAML y generado informes con una comprobacion
sin token; tambien se verifico el codigo de salida 1 con un fallo provocado.
Ver [automatizacion](AUTOMATIZACION.md). Quedan el recorrido autenticado
completo, sus formularios y los secretos locales. No se ha configurado CI/CD.

Usar exclusivamente una API de pruebas con datos prescindibles. Las peticiones
crean, actualizan y borran logicamente datos de prueba; no borran fisicamente
filas ni reinician bases de datos. El arranque de la API sigue siendo
destructivo, como se advierte en el README principal.

1. Seleccionar el entorno **Local**, con los secretos Admin y User ya
   configurados. No hacen falta nuevas contrasenas para estas carpetas.
2. Ejecutar los dos logins de `1 - AUTHENTICATION` antes del recorrido.
   Admin genera los nombres unicos; User guarda `userToken` para comprobar 403.
3. Ejecutar las carpetas secuencialmente segun `info.seq`, sin paralelismo.
   Para repetir un recorrido, repetir primero los logins.
4. No sobrescribir desde pestanas antiguas de Bruno los archivos actualizados
   externamente. Revisar las nuevas carpetas antes de lanzarlas.

Los numeros de cada nueva carpeta coinciden con su `seq`. Los nombres unicos
se generan con `Date.now()` y las dependencias se guardan con `bru.setVar`,
como los scripts del profesor. Los tokens siempre son variables; no hay JWT
ni claves pegados en las peticiones nuevas.

## Formularios e imagenes

POST y PUT de **proveedores** llevan JSON con `nombre`.

POST y PUT de **funkos** llevan `multipart-form`, porque la accion recibe
`[FromForm]`: campos `nombre`, `precio`, `stock`, `categoriaId`, `proveedorId`
y opcionalmente `archivo`. No copiar el JSON de productos de Tienda a estas
acciones. No fijar manualmente la cabecera multipart ni su boundary.

La imagen es el archivo existente `SeedImages/funkos/DefaultImage.jpg`, cuya
ruta desde la raiz de la coleccion es
`../../../SeedImages/funkos/DefaultImage.jpg`. Se sube una copia al servidor;
la suite no modifica el archivo original. El archivo rechazado es
`assets/archivo-no-imagen.txt`, anadido como dato de pruebas.
Las rutas de los archivos se han preparado relativas a la coleccion, no a
`C:\Users\...`; hay que verificar su resolucion al abrirla en Bruno.

El Funko que se actualiza se crea **con imagen propia subida**, no con la
imagen compartida por defecto: el servicio elimina la imagen antigua en PUT.
El Funko sin imagen se comprueba y se borra logicamente, pero no se actualiza.

## 3 - PROVEEDORES

Todas las acciones requieren ADMIN, tambien las consultas. No hay busqueda
por nombre en este controlador. No se ha inventado un 409 por duplicados:
`ProveedorService.Create` no comprueba el nombre duplicado.

| Peticion | Esperado |
| --- | --- |
| [001] Crear Admin | 201, ID string, guarda `proveedorId` |
| [002] Listar Admin | 200, array |
| [003] Consultar ID Admin | 200, proveedor creado |
| [004] Crear nombre vacio | 400, errores de validacion |
| [005] Crear nombre largo | 400, errores de validacion |
| [006] Actualizar nombre vacio | 400, errores de validacion |
| [007] Actualizar nombre largo | 400, errores de validacion |
| [008] Consultar inexistente | 404, mensaje |
| [009] Actualizar inexistente | 404, mensaje |
| [010] Borrar inexistente | 404, mensaje |
| [011] Listar User | 403 |
| [012] Consultar ID User | 403 |
| [013] Crear User | 403 |
| [014] Actualizar User | 403 |
| [015] Borrar User | 403 |
| [016] Listar sin token | 401 |
| [017] Consultar ID sin token | 401 |
| [018] Crear sin token | 401 |
| [019] Actualizar sin token | 401 |
| [020] Borrar sin token | 401 |
| [021] Actualizar Admin | 200, nombre actualizado |
| [022] Consultar actualizado | 200, comprueba persistencia/cache |
| [023] Borrar Admin | 200, `true` |

Se usa `fffffffffffffffffffffffe` como ID inexistente: es una cadena de 24
caracteres hexadecimales, no un ID mal formado. El repositorio usa
`ObjectId.Parse` sin validar antes; no se ha inventado que un ID como `abc`
produzca 400. Tampoco se ha afirmado que GET de proveedor borrado devuelva
404: el repositorio actual no filtra `IsDeleted` en esa consulta.

El borrado de proveedor **con Funkos** se comprueba en [040] de la carpeta 4:
alli se crean las dependencias propias. Devuelve **409**, no el 422 de Tienda.

## 4 - FUNKOS

Esta carpeta crea su propia categoria y proveedor. No reutiliza el
`categoriaId` ni el `proveedorId` de las carpetas 2 y 3, que ya se han borrado
logicamente al finalizar sus recorridos.

| Peticion | Esperado |
| --- | --- |
| [001] Preparar categoria | 201, guarda `funkoCategoriaId` |
| [002] Preparar proveedor | 201, guarda `funkoProveedorId` |
| [003] Crear sin imagen | 201, guarda `funkoSinImagenId`, imagen por defecto |
| [004] Crear con imagen | 201, guarda `funkoId`, ID numerico |
| [005] Listar Admin | 200, array |
| [006] Listar User | 200, array |
| [007] Consultar ID Admin | 200, Funko creado |
| [008] Consultar nombre en la ruta | 200, mismo Funko |
| [009] Crear nombre duplicado | 409, mensaje |
| [010] Actualizar a nombre del otro Funko | 409, mensaje |
| [011] Crear con categoria inexistente | 400, mensaje |
| [012] Crear con proveedor inexistente | 400, mensaje |
| [013] Actualizar con categoria inexistente | 400, mensaje |
| [014] Actualizar con proveedor inexistente | 400, mensaje |
| [015] Actualizar sin imagen | 400, mensaje `No se ha mandado la imagen` |
| [016] Crear con archivo .txt | 422, mensaje de extension no permitida |
| [017] Actualizar con archivo .txt | 422, mensaje de extension no permitida |
| [018] Consultar ID inexistente | 404, mensaje |
| [019] Consultar nombre inexistente | 404, mensaje |
| [020] Actualizar ID inexistente | 404, mensaje |
| [021] Borrar ID inexistente | 404, mensaje |
| [022] PATCH imagen de ID inexistente | 404, mensaje |
| [023] PATCH sin archivo obligatorio | 400, errores de binding/validacion |
| [024] Crear User | 403 |
| [025] Actualizar User | 403 |
| [026] Borrar User | 403 |
| [027] Consultar ID User | 403 |
| [028] Consultar nombre User | 403 |
| [029] PATCH imagen User | 403 |
| [030] Listar sin token | 401 |
| [031] Consultar ID sin token | 401 |
| [032] Consultar nombre sin token | 401 |
| [033] Crear sin token | 401 |
| [034] Actualizar sin token | 401 |
| [035] Borrar sin token | 401 |
| [036] PATCH imagen sin token | 401 |
| [037] Paginar sin token, pagina 1 tamano 2 | 401 |
| [038] Paginar con pagina 0 y tamano 101 | 200, ajusta a pagina 1 y tamano 100 |
| [039] Paginar con pagina `abc` | 400, errores de binding/validacion |
| [040] Borrar proveedor propio en uso | 409, mensaje |
| [041] Borrar categoria propia en uso | 400, mensaje |
| [042] Actualizar Admin con imagen | 200, nuevo nombre/precio/stock |
| [043] Consultar actualizado | 200, comprueba valores persistidos |
| [044] Borrar Funko principal | 200, `true` |
| [045] Consultar Funko principal borrado | 404, mensaje |
| [046] Borrar Funko sin imagen | 200, `true` |
| [047] Consultar Funko sin imagen borrado | 404 |
| [048] Paginar Admin, pagina 1 tamano 2 | 200, `data` y `pagination` |
| [049] Paginar User, pagina 1 tamano 2 | 200, `data` y `pagination` |

El ID inexistente es `2147483647`, dentro del rango `int` que recibe la
accion aunque su restriccion de ruta sea `long`. Debe estar libre en la BD
de pruebas. Los GET por ID/nombre de Funkos si filtran los borrados; las
listas no lo hacen. No se comprueba un total fijo que dependa de ejecuciones
anteriores ni se atribuye al listado un filtrado que no tiene.

Los servicios consideran dependencias incluso los Funkos borrados
logicamente. Por eso los auxiliares de esta carpeta no se borran al final:
seguirian teniendo referencias fisicas. No se hace un borrado fisico ni se
altera el criterio de negocio para forzar una limpieza en verde.
Quedan tambien los archivos de los Funkos borrados logicamente. La suite no
debe ejecutarse con datos de produccion ni usarse como limpieza de datos.

## Alcance: PATCH y validaciones sin modificar

### Validacion de los formularios de Funkos

En `FunkosController.Create/Update` se reciben parametros separados de
formulario y se construye el DTO dentro de la accion. No hay una validacion
explicita posterior en esas acciones/servicio para aplicar sus rangos.
Por inspeccion, no se puede dar por hecho que `Range` y `MaxLength` de ese DTO
rechacen precio negativo, stock cero o nombres largos en esos endpoints.

Se han preparado seis regresiones (POST/PUT para cada caso) que esperan 400,
respaldadas por las restricciones de los DTO y el ejemplo de precio invalido
del profesor. Estan en `Pendientes/Funkos`, **fuera de la coleccion activa**.
No se han cambiado a esperar 201, 200, 409 o 500 para ocultar el problema.
Si se ejecutan sin corregirlo pueden insertar datos invalidos o modificar el
Funko de pruebas. Hay que revisar la API y verificar su comportamiento antes
de integrarlas, despues de la preparacion y antes del borrado.

### PATCH de imagen

`FunkosController.UpdateImagen` elimina primero la imagen anterior, guarda la
nueva y construye un `UpdateFunkoDto` sin `Proveedor`. Despues el servicio
comprueba ese proveedor y vuelve a guardar la imagen. Esto puede fallar y
dejar archivos/estado incoherentes. Se ha preparado el caso positivo 200 en
`Pendientes/Funkos`, pero **no se incluye en una ejecucion automatica** ni se
ha tocado el controlador para arreglarlo sin pedirlo.

Los casos activos PATCH de 401/403, ID inexistente y archivo obligatorio
ausente se esperan rechazar antes de modificar imagenes. Falta verificarlos
en ejecucion. No se ha anadido al recorrido un PATCH con archivo invalido
sobre un Funko existente, porque la accion borra la imagen anterior antes
de validar el archivo.

### Busqueda por nombre

Corregido el nombre del parametro de `GetByNombre`: ahora coincide con
`{nombre}`. Las pruebas activas llaman a `/api/funkos/{{funkoNombre}}`, sin
query auxiliar, y esperan 200 o 404 segun exista. No se cambia el permiso ADMIN.
La plantilla anteriormente pendiente se conserva en `Resueltas/Funkos`, fuera
de la coleccion; su escenario esta integrado en [008]. Esa ubicacion indica
que el cambio esta aplicado, no que se haya verificado ejecutando la API.

### Errores de almacenamiento

`StorageError.ExtensionNoPermitida` devuelve `BusinessRuleError`, que no
hereda de `BadRequestError`. Se ha anadido el mapeo de ese tipo en las dos
sobrecargas de `DomainErrorExtensions.ToHttpResult`, siguiendo exactamente
el ejemplo del profesor: `UnprocessableEntityObjectResult` (422) y el mensaje
del error. Ya no cae por omision en el 500 generico.
Se han reintegrado POST/PUT con archivo .txt como [016]/[017] y se ha ajustado
el esperado anterior 400 a 422 para seguir ese criterio de clase. Esto no
cambia `StorageError` ni el codigo del PATCH. El mapeo se aplica a todos los
`BusinessRuleError`, no solo a archivos. Se han ampliado las pruebas unitarias
para ambas sobrecargas y errores concretos de extension/tipo de contenido.

### Autorizacion de paginacion

Por peticion de Lucia, `GetPaged` lleva ahora `[Authorize(Roles = "ADMIN,USER")]`,
igual que GetAll. [037] espera 401 sin token; [048]/[049] comprueban el acceso
con Admin/User y esperan 200. [038]/[039] envian token Admin para poder
comprobar limites y errores de parametros en lugar de quedarse en el 401.
No se cambian el resto de permisos ni los limites de paginacion.

Los archivos de `Pendientes/Funkos` son plantillas para integrar mas adelante
en la coleccion principal (rutas de archivo relativas a esa coleccion).
No forman otra coleccion ejecutable ni se pueden lanzar aislados sin sus
variables/preparacion. No hace falta moverlos a mano ahora.

Quedan pendientes unicamente las seis regresiones de validacion de DTO y el
PATCH positivo, expresamente fuera del alcance de esta correccion. No se
ha modificado su implementacion ni se han dado por pasadas sus pruebas.

## Fuentes y adaptaciones

Se conserva la estructura YAML de la coleccion y los ejemplos de
[UD02, peticion autenticada Me.yml](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/02-ProductosControllersApi/bruno/Prueba%20Api/Me.yml).
Los scripts siguen el estilo del profesor `test`, `expect`, `res.getStatus`,
`res.getBody` y variables de recorrido, sin introducir otra arquitectura.

- [Proveedor: crear y guardar identificador](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/14%20-%20PROVEEDORES/%5B094%5D%20POST%20-%20Crear%20sin%20codigo%20(Admin).bru).
  Adaptado a `id` string de Mongo y DTO con solo `nombre`; no codigo/descripcion/contacto.
- [Proveedor: rol USER devuelve 403](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/14%20-%20PROVEEDORES/%5B093%5D%20GET%20-%20Rol%20USER%20(403).bru).
- [Proveedor con productos asociados](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/14%20-%20PROVEEDORES/%5B098%5D%20DELETE%20-%20Con%20productos%20(422).bru).
  Aqui el servicio devuelve ConflictError (409) y se usan dependencias propias.
- [Producto: creacion, datos de respuesta e ID para siguientes peticiones](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/3%20-%20PRODUCTOS/%5B027%5D%20POST%20-%20Crear%20(Admin).bru).
  Adaptado a formulario y FunkoDto real, sin descripcion ni proveedor en la respuesta.
- [Subida multipart de imagen](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/3%20-%20PRODUCTOS/%5B031%5D%20PATCH%20-%20Subir%20imagen.bru).
  Adaptado al campo `archivo`, a YAML y a la imagen ya existente en este repo.
- [Precio negativo: esperado 400](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/3%20-%20PRODUCTOS/%5B034%5D%20POST%20-%20Error%20precio.bru).
  Se mantiene ese objetivo en la regresion pendiente, sin darla por pasada.
- [Ejemplo pequeno de mapeo DomainError a HTTP](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/07-ProductosResult/ProductosResult/Extensions/DomainErrorExtensions.cs).
  Referencia del mapeo BusinessRuleError a 422 en esta correccion.
- [Controlador del ejemplo de Identity](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/15-ProductosIdentity/ProductosTest/Controllers/ProductosController.cs).
  Se reproduce su atributo de roles `ADMIN,USER`, aplicado a GetPaged por
  peticion de Lucia, sin copiar los permisos del resto de ese proyecto.

Los codigos concretos, DTO, reglas de dependencias, permisos y paginacion se
han adaptado leyendo los controladores, servicios, repositorios y errores
actuales de FunkoApi.Back. No se han trasladado modulos ni la arquitectura
completa de Tienda.
