# Pruebas E2E con Bruno

Coleccion: `Bruno/FunkoApi - Tests E2E`. Se mantiene el formato YAML original;
no se han convertido ni renombrado archivos a `.bru`.

Carpetas activas: `1 - AUTHENTICATION`, `2 - CATEGORIAS`,
`3 - PROVEEDORES` y `4 - FUNKOS`, en ese orden.
Ejecucion automatica con Docker e informes: [AUTOMATIZACION.md](AUTOMATIZACION.md).
Las siguientes secciones detallan categorias. Para las nuevas carpetas,
consultar [proveedores y funkos: orden, resultados y limites](PROVEEDORES-Y-FUNKOS.md).
Hay 7 peticiones de regresion de Funkos fuera de la coleccion activa, en
`Pendientes/Funkos`: no se han dado por resueltos los fallos que las afectan.
Las correcciones de nombre, archivos no permitidos y autorizacion de
paginacion estan aplicadas en el codigo y sus pruebas se han actualizado;
queda comprobarlas en ejecucion.

## Preparacion

- Usar solo una API y bases de datos de desarrollo/pruebas. El arranque actual
  de la API borra y vuelve a sembrar bases de datos y limpia imagenes.
- Deben estar disponibles los usuarios sembrados por
  `FunkoApi.Back/Data/Seed/Identity/IdentitySeeder.cs` y los datos de
  `FunkoApi.Back/Data/Seed/Efc/EfcSeeder.cs`: la categoria Marvel con su Funko
  Iron Man. La prueba de borrado de categoria en uso depende de esos datos.
  No ejecutarla contra categorias reales que se deban conservar.
- Abrir en Bruno la copia del proyecto y seleccionar el entorno **Local**.
- Mantener `baseUrl` y `adminEmail`. Anadir en Variables:
  `userEmail = user@user.com`.
- En **Secrets**, configurar `adminPassword` y `userPassword` con las claves de
  los respectivos usuarios de pruebas. No son las claves de PostgreSQL/MongoDB.
  No pegar claves ni JWT en las peticiones ni publicarlos en el repositorio.
  Este cambio no modifica el entorno ni guarda ninguna contrasena.

## Orden del recorrido

Primero ejecutar `1 - AUTHENTICATION`: login Admin y login User. Ambos guardan
su token automaticamente. El login Admin genera nombres con `Date.now()`,
siguiendo la generacion de datos unicos del ejemplo SIGNUP del profesor.

Despues ejecutar `2 - CATEGORIAS` **secuencialmente, segun `info.seq`**:

| seq | Peticion | Resultado esperado |
| --- | --- | --- |
| 1 | [002] Crear Admin | 201, guarda `categoriaId` |
| 2 | [014] Crear nombre duplicado | 409 |
| 3 | [003] Consultar ID Admin | 200 |
| 4 | [004] Consultar nombre Admin | 200 |
| 5 | [015] Listar Admin | 200, array |
| 6 | [016] Listar User | 200, array |
| 7 | [017] Consultar ID User | 200 |
| 8 | [018] Consultar nombre User | 200 |
| 9 | [019] Actualizar nombre vacio | 400 |
| 10 | [020] Actualizar nombre mayor de 100 caracteres | 400 |
| 11 | [021] Consultar Marvel sembrada | 200, guarda `categoriaEnUsoId` |
| 12 | [022] Actualizar la categoria E2E al nombre Marvel | 409 |
| 13 | [023] Intentar borrar Marvel, utilizada por Iron Man | 400 |
| 14 | [024] Crear con rol USER | 403 |
| 15 | [025] Actualizar con rol USER | 403 |
| 16 | [026] Borrar con rol USER | 403 |
| 17 | [027] Listar sin token | 401 |
| 18 | [028] Consultar ID sin token | 401 |
| 19 | [029] Consultar nombre sin token | 401 |
| 20 | [007] Crear sin token | 401 |
| 21 | [030] Actualizar sin token | 401 |
| 22 | [031] Borrar sin token | 401 |
| 23 | [008] Consultar ID inexistente | 404 |
| 24 | [009] Actualizar ID inexistente | 404 |
| 25 | [010] Borrar ID inexistente | 404 |
| 26 | [011] Consultar nombre inexistente | 404 |
| 27 | [012] Crear nombre vacio | 400 |
| 28 | [013] Crear nombre mayor de 100 caracteres | 400 |
| 29 | [005] Actualizar Admin | 200 |
| 30 | [006] Borrar Admin | 200, cuerpo `true` |

Los numeros entre corchetes se conservan para reconocer las peticiones que
ya habia creado Lucia; **no representan ahora el orden de ejecucion**.
Si Bruno tiene cambios sin guardar, no sobrescribir con una pestana antigua
los archivos que se han actualizado externamente.

## Autenticacion y autorizacion

- `401`: no se envia token. Hay una peticion por cada accion del controlador.
- `403`: se envia un token valido de USER al crear, actualizar o borrar.
- `200`: ADMIN y USER pueden listar y consultar por ID/nombre.
- `201/200`: ADMIN puede crear, actualizar y borrar.

No configurar un Bearer global en la coleccion/carpeta: las pruebas sin auth
no declaran autenticacion, igual que la exportacion original de Bruno. Las
peticiones autenticadas llevan explicitamente `{{adminToken}}` o `{{userToken}}`.
No sustituir un 403 esperado por 401: si falla el login User, hay que corregir
su secreto/configuracion antes de comprobar autorizacion.

## Datos y limites

`categoriaNombre` y `categoriaNombreActualizada` son nuevos en cada login Admin.
Hay que repetir **los logins y todo el recorrido**, no repetir solo la carpeta
de categorias con variables del recorrido anterior. El borrado es logico y
quedan filas de pruebas marcadas como eliminadas: no se borran fisicamente ni
se limpian bases de datos desde esta coleccion.

Los GET actuales del repositorio no filtran `IsDeleted`; por eso no se ha
inventado una comprobacion de 404 tras el borrado. Esa regla necesita aclarar
el enunciado antes de modificar la API.

La validacion del nombre y el rechazo de borrado con Funkos son adaptaciones
a los DTO, servicio, errores y datos sembrados de este proyecto. No se prueban
fallos de infraestructura ni carreras concurrentes simuladas desde Bruno.

## Referencias del profesor

- [YAML de una peticion autenticada, ejemplo pequeno UD02](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/02-ProductosControllersApi/bruno/Prueba%20Api/Me.yml).
  Solo se sigue la estructura YAML; no se copian tokens del ejemplo.
- [POST rechazado por rol USER, 403](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/2%20-%20CATEGOR%C3%8DAS/%5B018%5D%20POST%20-%20Error%20rol%20USER.bru).
- [POST duplicado, 409](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/2%20-%20CATEGOR%C3%8DAS/%5B017%5D%20POST%20-%20Error%20nombre%20duplicado.bru).
- [Login User y guardado de userToken](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/1%20-%20AUTHENTICATION/%5B007%5D%20SIGNIN%20-%20Usuario.bru).
  Aqui se adapta a `/api/Auth/login`, email y `role` directo en la respuesta.
- [Nombre unico con Date.now y bru.setVar](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/1%20-%20AUTHENTICATION/%5B006%5D%20SIGNUP%20-%20Usuario%20nuevo.bru).
- [UD02, testing: forma de errores, apartado 28.10.1](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/28-testing.md#28101-tests-de-la-forma-de-los-errores).

## Estado de comprobacion

Tras corregir la configuracion de StorageInitializationExtensionsTests,
se ejecuto la suite C# completa. Tras anadir dos casos de regresion para el
ObjectId en cache y corregir su serializacion, la ultima ejecucion da
**389 correctos, 0 fallidos y 0 omitidos**.
Incluye los 79 tests de integracion con Testcontainers.
Bruno CLI 4.3.0 ha ejecutado YAML, un test 401 y una sonda deliberada de fallo
con codigo de salida 1. Se han generado los tres informes de comprobacion.
En la ejecucion autenticada compartida por Lucia pasaron 52 peticiones,
fallo el PUT de proveedor por el ID con ceros y se omitieron 51 por --bail.
La causa se reprodujo y corrigio en cache; queda desplegar ese cambio en
la API de pruebas y repetir E2E. No se da por pasada la coleccion completa.
Se mantienen avisos de compilacion/paquetes, incluido NU1903 de SSH.NET.
Para ejecutar la coleccion deben estar configurados los secretos Admin/User.
La ejecucion local automatica y los informes estan preparados y comprobados
parcialmente; no se ha configurado ni ejecutado CI/CD.
