# Ejecutar automaticamente la coleccion de Bruno

## Preparacion unica

La API de pruebas debe estar ya arrancada en `http://localhost:5000`.
Este Compose **solo ejecuta Bruno**: no reconstruye, reinicia ni siembra la
API y no elimina volumenes. No usar datos que se deban conservar.

Desde la raiz de FunkoApiProyecto, copiar la plantilla:

En este ordenador ya se ha creado la copia local con las claves vacias;
solo falta rellenarlas. Los pasos de copia sirven para otro ordenador.

```powershell
Copy-Item "FunkoApi.Tests.E2E/environments/cli.example.json" "FunkoApi.Tests.E2E/environments/cli.local.json"
```

Hacer la copia solo si no existe ya `cli.local.json`, para no reemplazar la
configuracion local. Abrir la copia y rellenar **unicamente** los `value`
vacios de `adminPassword` y `userPassword` con las claves de los usuarios
de pruebas. No cambiar `secret: true`. No pegar JWT: los logins los obtienen
y guardan automaticamente en cada recorrido.

La copia local esta ignorada por Git. Los secretos de la aplicacion Bruno
no se transfieren automaticamente a esta CLI. El JSON sigue el formato de
entorno exportado que utiliza el profesor; la coleccion permanece en YAML.
`secret: true` no cifra el JSON ni hace seguro publicarlo: la proteccion aqui
es mantenerlo local y excluirlo del repositorio y de los informes.

No hacen falta Node, npm ni instalar Bruno CLI en Windows. Se utiliza la
imagen oficial `usebruno/cli:latest` como en el Compose del profesor.

## Ejecucion completa

Desde la raiz del proyecto:

```powershell
docker compose -f "FunkoApi.Tests.E2E/compose.bruno.yaml" run --rm bruno
```

Una unica ejecucion recorre, en este orden:

1. AUTHENTICATION: Admin y User.
2. CATEGORIAS.
3. PROVEEDORES.
4. FUNKOS.
5. SIGNALR HTTP: negociacion sin token (401), con USER (403) y con ADMIN (200).

Estas comprobaciones usan HTTP; no abren una conexion SignalR ni verifican la
recepcion del evento FunkoCreado. Bruno CLI no soporta WebSocket (apartado
18.12 de los apuntes). La recepcion debe comprobarse con el cliente SignalR:
dos conexiones ADMIN abiertas, crear un funko y verificar el aviso en ambas.
El cliente se sirve desde http://127.0.0.1:3000, permitido por ClienteSignalR.
No se ha dado por pasada esta ampliacion E2E sin ejecutarla contra una API de pruebas.

No lanzar carpetas en procesos separados: los tokens, IDs y nombres son
variables del recorrido. No se usa paralelismo. Se mantienen `info.seq`,
los nombres unicos de cada login Admin y 250 ms entre peticiones, como en
el ejemplo del profesor. `--bail` para en el primer fallo y devuelve un
codigo de salida distinto de cero, apto para una automatizacion posterior.
No hay reintentos automaticos que repitan creaciones o borrados.

Desde el contenedor, `localhost` seria el propio Bruno: por eso el entorno
usa `host.docker.internal:5000`. Se montan solo la coleccion y `SeedImages`
en lectura, conservando su estructura de rutas. No se monta el `.env` de la
API; solo `reports` permite escritura.
Las carpetas `Pendientes` y `Resueltas` estan fuera de la coleccion activa.

## Informes

Se escriben en `FunkoApi.Tests.E2E/reports`:

- `report.html`: informe visual.
- `report.json`: resultados estructurados.
- `junit-report.xml`: resultado para herramientas de automatizacion.

La configuracion omite cabeceras y cuerpos de los informes para no guardar
contrasenas de login ni tokens de respuesta. No habilitar `--verbose` ni
anadir logs de tokens. Los informes se ignoran tambien en Git y no deben
publicarse sin revisar. Un fallo con `--bail` significa que el resto del
recorrido **no se ha ejecutado**, no que haya pasado.

En CLI 4.3.0 los `test(...)` de Post Response se muestran en el apartado
`Post-Response Tests`, aunque el resumen muestre `Tests 0/0`. Se ha comprobado
que un fallo en ese script marca la peticion como fallida y devuelve codigo 1.
No interpretar `0/0` como que toda la suite esta cubierta o ha pasado.

## Estado comprobado

- Suite C# completa, tras corregir la seccion de configuracion del test de
  infraestructura y anadir la regresion del ObjectId de cache:
  **389 correctos, 0 fallidos, 0 omitidos** (`revision-cache.trx`).
- Imagen oficial de Bruno CLI descargada: version **4.3.0**.
- Coleccion YAML ejecutada sin conversion: GET All sin token devuelve 401 y
  su test Post Response pasa.
- Prueba controlada de fallo: un GET que espera 404 se ejecuto deliberadamente
  sin token; recibio 401 y la CLI devolvio codigo 1. No era la suite completa
  ni un fallo confirmado de la API: verificaba que el runner no da falso verde.
- Compose validado con `config --quiet`. Ejecucion de comprobacion con el
  entorno JSON local y generacion de `verificacion-cli.html`,
  `verificacion-cli.json` y `verificacion-cli.xml` en reports: correctas.
  La ultima comprobacion ejecuto GET All sin token y PATCH imagen sin token
  (ID de sonda): **2 peticiones y 2 tests de Post Response correctos**, ambos
  con 401. No se actualizo ninguna imagen ni se ejecuto el PATCH autenticado.
  JSON/XML incluyen el test de Post Response y su resultado. En ese informe
  no aparecen claves, tokens, cabeceras ni cuerpos.
- Lucia ejecuto el recorrido autenticado y compartio: 52 peticiones correctas,
  1 fallida (PUT proveedor devolvia ID con ceros) y 51 omitidas por --bail.
  Se reprodujo esa perdida de ObjectId en dos casos unitarios antes del
  arreglo. Se configuro ObjectIdJsonConverter en lectura y escritura de
  RedisCacheService y despues pasaron los 389 tests C#.
  El cambio no se ha desplegado ni probado aun en el contenedor de la API;
  sigue pendiente volver a ejecutar el recorrido E2E completo con ese cambio.
  No se ha dado por pasada la suite E2E completa.
- No se ha reconstruido ni reiniciado la API. Si el contenedor ejecuta una
  compilacion anterior, hay que planificar su reconstruccion: su arranque
  actual borra y vuelve a sembrar datos. Este runner no lo hace por su cuenta.
- No se ha configurado ni ejecutado CI/CD ni publicado ningun archivo.

## Referencias y adaptacion

- [Compose de Bruno del profesor](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Cli/docker-compose.yml):
  imagen oficial, carpetas explicitas, host.docker.internal, delay e informes.
- [Entorno JSON del profesor](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests.E2E/Bruno-Local/environments/TiendaApi__NET_-_Environment.json):
  formato `variables`, `name`, `value`, `enabled`, `secret`.
- [Instrucciones Bruno CLI](https://github.com/joseluisgs/TiendaDawApi-NetCore#bruno-cli).

Adaptacion: cuatro carpetas propias, puerto 5000, entorno privado sin claves
en el repositorio, conservacion de rutas de imagenes y sin reinicios de API.
Las opciones `--bail`, `--reporter-skip-body` y `--reporter-skip-all-headers`
se han comprobado en la ayuda de la CLI instalada. No se incorpora la
arquitectura ni las carpetas ajenas de Tienda.

La correccion de cache mantiene el procedimiento del
[RedisCacheService de soluciones/13-ProductosCache](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/13-ProductosCache/ProductosTest/Cache/RedisCacheService.cs).
La adaptacion concreta es usar en ambos sentidos el ObjectIdJsonConverter
que ya existia en este proyecto. No se desactiva la cache ni se debilita el
test de Bruno que comprueba que el ID del proveedor se conserva.
