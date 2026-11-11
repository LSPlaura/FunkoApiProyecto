# FunkoApi.Back 🛍️

[![.NET](https://img.shields.io/badge/.NET-10-blue)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10-blue)](https://dotnet.microsoft.com/en-us/apps/aspnet)
[![C#](https://img.shields.io/badge/C%23-14-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10-blue)](https://docs.microsoft.com/en-us/ef/core/)
[![MongoDB](https://img.shields.io/badge/MongoDB-7-green)](https://www.mongodb.com/)
[![Redis](https://img.shields.io/badge/Redis-7-red)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-blue)](https://docs.docker.com/compose/)

**API REST didáctica de catálogo de Funkos** desarrollada con **.NET 10, ASP.NET Core 10 y C# 14** como parte del módulo *Entorno Servidor* (2DAW, Tema 2).

Implementa una API RESTful completa sobre HTTP con **autenticación JWT, autorización por roles, almacenamiento de ficheros, servicios de negocio, repositorios genéricos, dos bases de datos (PostgreSQL + MongoDB) y caché distribuida Redis**. Incluye pruebas unitarias, de integración con Testcontainers y E2E con Bruno.

---

## 🎯 Descripción del problema

El proyecto implementa un catálogo de productos coleccionables (Funkos) mediante una **API REST sobre HTTP**. Estas son sus funcionalidades actuales; la lista no sustituye al enunciado ni a los criterios de evaluación de la práctica:

- **Autenticación y autorización**: login/registro con ASP.NET Core Identity + JWT, roles `ADMIN` y `USER`, protección por acción con `[Authorize(Roles = "...")]`.
- **Almacenamiento de ficheros**: subida de imágenes de Funkos con validación de extensión, tipo MIME y tamaño, y nombres únicos.
- **Lógica de negocio en servicios**: los controladores son delgados; la orquestación vive en `FunkoServices`, `CategoriaService`, `ProveedorService`, `AuthService` y `FunkoTransactionsService`.
- **Acceso a datos con repositorios**: contrato genérico `ICrud<T, TKey>` y repositorios específicos por agregado.
- **Dos bases de datos**: PostgreSQL (EF Core) para Funkos, Categorías e Identity; MongoDB (driver nativo) para Proveedores.
- **Caché distribuida**: Redis con expiración absoluta y deslizante, más bloqueo distribuido contra *cache stampede*.
- **Manejo de errores tipado**: patrón `Result` con `DomainError` y traducción centralizada a códigos HTTP.
- **Transacciones y archivos**: el servicio transaccional de Funkos implementa actualización y borrado físico con operaciones sobre la base de datos y las imágenes. El sistema de archivos no forma parte de la transacción de PostgreSQL; no se garantiza una reversión conjunta de ambos recursos.
- **Paginación estándar**: `PagedResponse<T>` + cabeceras `Link` según RFC 5988.
- **Pruebas**: unitarias (NUnit + Moq + FluentAssertions), integración (Testcontainers con PostgreSQL y MongoDB reales) y E2E (colección de Bruno).

---

## 📑 Tabla de contenidos

| # | Sección |
|---|---------|
| 1 | [✨ Características](#caracteristicas) |
| 2 | [🚀 Tecnologías utilizadas](#tecnologias) |
| 3 | [🏃‍♂️ Inicio rápido y cambio de entorno](#inicio-rapido) |
| 4 | [⚙️ Configuración de la aplicación](#configuracion) |
| 5 | [🏗️ Arquitectura y patrones](#arquitectura) |
| 6 | [🔐 Seguridad (JWT y roles)](#seguridad) |
| 7 | [📡 Endpoints](#endpoints) |
| 8 | [🗄️ Modelo de datos y semilla](#modelo-datos) |
| 9 | [🔄 Ciclo de arranque](#arranque) |
| 10 | [📂 Estructura del proyecto](#estructura) |
| 11 | [🧪 Estrategia de testing](#testing) |
| 12 | [📢 Probar SignalR con Live Server](#signalr) |
| 13 | [🐳 Despliegue con Docker](#docker) |
| 14 | [👥 Usuarios de prueba](#usuarios-prueba) |
| 15 | [📄 Licencia](#licencia) |

---

<a id="caracteristicas"></a>
## ✨ Características

- 🏪 **CRUD completo**: Funkos, Categorías y Proveedores.
- 🔐 **Autenticación JWT**: login y registro con `UserManager`, contraseñas hasheadas por Identity y token simétrico `HMAC-SHA256`.
- 🔒 **Autorización por roles**: `[Authorize]` y `[Authorize(Roles = "ADMIN,USER")]` a nivel de acción.
- 🖼️ **Almacenamiento de archivos**: `IStorageService` con `FileSystemStorageService`, validación de seguridad y nombres únicos.
- ⚡ **Caché Redis**: `ICacheService` con `RedisCacheService` y `GetOrSetAsync` (lectura → bloqueo distribuido → fábrica → escritura).
- 🗄️ **Multi-base de datos**: PostgreSQL (EF Core 10 + Npgsql) + MongoDB (driver nativo `MongoDB.Driver`).
- 🔁 **Patrón Result**: `CSharpFunctionalExtensions` (`Result<T, DomainError>`) con errores de dominio tipados.
- 📐 **DTOs en REST**: `Create*Dto`, `Update*Dto` y DTOs de lectura. GraphQL define sus propios tipos y resolvers.
- 🗺️ **Mapeadores explícitos**: extensiones `ToDto()` / `ToModel()` en `Mappers/`.
- 🆔 **Identificadores personalizados**: generador de IDs estilo YouTube (11 caracteres) para `Categoria`.
- ⚙️ **Configuración tipada**: `ApiConfig`, `JwtSettings`, `StorageSettings`, `CacheOptions` con `IOptions<T>`.
- 🔗 **Factory de cadenas de conexión**: `ConnectionStringGenerator` dispone de métodos para PostgreSQL, MongoDB y Redis. El registro actual de PostgreSQL construye la cadena directamente en `DatabaseConfig`.
- 📄 **Paginación**: `PagedResponse<T>` con `PageMetadata` y cabecera `Link`.
- 🧪 **Tests**: unitarios, integración con contenedores reales y E2E con Bruno.
- 🐳 **Docker**: `compose.yaml` con PostgreSQL, MongoDB, Redis y la API, con `healthcheck` y volúmenes nombrados.
- 📢 **SignalR**: hub `/hubs/funkos` exclusivo para `ADMIN`, con notificaciones `FunkoCreado` y cliente JavaScript de prueba.
- 🔎 **GraphQL**: servidor Hot Chocolate con consultas, tipos, resolvers y DataLoader.
- ✉️ **Email en segundo plano**: cola y worker, con simulación en memoria en desarrollo por defecto o SMTP con MailKit mediante `Email:UseSmtp`.
- 🚦 **Rate limiting**: límite por IP y ámbito; 200 solicitudes/minuto para POST de Funkos y 1000/minuto para el resto. Devuelve `429` y `Retry-After` al superar el límite.

---

<a id="tecnologias"></a>
## 🚀 Tecnologías utilizadas

| Capa | Tecnología | Versión |
|------|------------|---------|
| Plataforma | .NET / ASP.NET Core | `net10.0` |
| Lenguaje | C# | 14 |
| ORM relacional | Entity Framework Core | `10.0.14` |
| Proveedor PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL | `10.0.3` |
| Base de datos documentos | MongoDB.Driver (nativo) | `3.12.0` |
| Integración EF + Mongo | MongoDB.EntityFrameworkCore | `10.0.4` |
| Caché distribuida | Microsoft.Extensions.Caching.StackExchangeRedis | `10.0.14` |
| Autenticación | Identity + JwtBearer | `10.0.14` |
| Tokens | Microsoft.IdentityModel.Tokens | `8.23.0` |
| Patrón Result | CSharpFunctionalExtensions | `3.7.0` |
| Documentación API | Swashbuckle.AspNetCore | `10.3.1` |
| Serialización | System.Text.Json | Incluido en .NET 10 |
| GraphQL | HotChocolate / GreenDonut | `16.7.3` |
| Email SMTP | MailKit | `4.18.2` |
| Logging | Serilog (Console + File) | `4.4.0` |
| Pruebas unitarias | NUnit `4.4.0` + Moq `4.20.72` + FluentAssertions `8.8.0` | — |
| Pruebas de integración | Testcontainers PostgreSql/MongoDb | `4.8.1` |
| Pruebas E2E | Bruno | Colección `FunkoApi - Tests E2E` |
| Contenedores | Docker + compose.yaml | PostgreSQL 17, MongoDB 7.0, Redis 7 |

---

<a id="inicio-rapido"></a>
## 🏃‍♂️ Inicio rápido

### Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para las bases de datos y el despliegue completo)

La solución usa el nuevo formato `FunkoApi.slnx`, con dos proyectos:

```
FunkoApi.Back/    → API (ASP.NET Core)
FunkoApi.Tests/   → pruebas (NUnit)
```

### Preparar la configuración

Antes de arrancar, prepara el **`.env` de la raíz** siguiendo la [sección de variables de entorno](#variables-env). Si ya existe y funciona, consérvalo: **no lo sobrescribas**. Para una instalación nueva, copia `.env.example` a `.env` y rellena `Postgres__Password`, `MongoDb__Password` y `Jwt__Key` (al menos 32 caracteres). La plantilla deja esos secretos vacíos y el correo en modo simulado; no está lista para arrancar sin completar la configuración.

### Opción A: API local y bases de datos en Docker

```bash
# 1. Clona el repositorio
git clone https://github.com/LSPlaura/FunkoApiProyecto

# 2. Sitúate en la raíz de la solución
cd FunkoApiProyecto

# 3. Restaura paquetes y compila
dotnet restore
dotnet build

# 4. Levanta la infraestructura (PostgreSQL, MongoDB, Redis)
docker compose up -d postgres mongodb redis

# 5. Continúa con la carga del .env y la elección del entorno que aparecen debajo.
```

#### Arrancar la API y seleccionar el entorno (PowerShell)

**Fuente:** [9.1.4, Ficheros .env](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/09-configuracion-logging.md#914-ficheros-env) y [25.4, Variables de entorno](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/25-perfiles.md#254-variables-de-entorno). Se utiliza el bloque de carga del profesor y la variable `ASPNETCORE_ENVIRONMENT`. No hay un script de arranque adicional.

Ejecuta los siguientes bloques desde la **raíz del repositorio**, donde está el `.env`, usando PowerShell. La carga del `.env` y el arranque deben hacerse en la misma terminal. En una terminal nueva hay que cargarlo otra vez. Para este procedimiento no debe haber otra variable `DOTNET_ENVIRONMENT` seleccionando un entorno diferente.

**1. Cargar el `.env` (bloque del apartado 9.1.4):**

```powershell
Get-Content .env | ForEach-Object {
    if ($_ -match '^\s*([^#=]+)=(.*)$') {
        Set-Item -Path "env:$($Matches[1].Trim())" -Value $Matches[2].Trim()
    }
}
```

**2. Elegir un entorno y ejecutar:**

```powershell
# Desarrollo: recrea las bases de datos de prueba en cada arranque
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project FunkoApi.Back
```

Para Production, únicamente si su base de datos y sus tablas ya están preparadas:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet run --project FunkoApi.Back
```

Detén la ejecución con `Ctrl+C` antes de arrancar en el otro entorno. Si conservas la terminal, basta con cambiar `ASPNETCORE_ENVIRONMENT` y ejecutar de nuevo `dotnet run --project FunkoApi.Back`; no hace falta repetir `restore` o `build` por cambiar el entorno.

**Datos de este proyecto:** `--project FunkoApi.Back` selecciona el backend. PostgreSQL usa la sección `Postgres`, igual que las variables del `.env`. Los nombres de las bases se configuran en `appsettings.Development.json` (`funkos_dev`) y `appsettings.Production.json` (`funkoapi`), siguiendo la separación por entornos del apartado 25.11. Son los nombres de esta práctica, no datos copiados del ejemplo del profesor.

El `.env` local contiene los datos de conexión y secretos. Para respetar esta separación, **no debe definir `Postgres__Database` ni `MongoDb__Database`**: esas variables sobrescribirían los nombres de base elegidos por los JSON. Tampoco debe seleccionar el entorno mediante `DOTNET_ENVIRONMENT`.

| Ejecución local | PostgreSQL y MongoDB | Inicialización | Herramientas |
|---|---|---|---|
| Development | `funkos_dev` | Elimina y recrea los datos; ejecuta los seeders | Swagger y exploración de GraphQL |
| Production | `funkoapi` | Requiere tablas existentes; ejecuta el seeder de Identity | Sin Swagger; introspección de GraphQL desactivada |

> ⚠️ **En ambos entornos se intenta vaciar `uploads` en cada arranque.** Este comportamiento pertenece al código actual, no al comando utilizado. No ejecutes la inicialización con imágenes que quieras conservar.

Al arrancar en Development, la API imprime en consola el nombre, el entorno y la URL:

```
========================================
  Funko API (DEV)
  Modo desarrollo - depuración activada
  http://localhost:5000
  Swagger: http://localhost:5000/swagger
  GraphQL: http://localhost:5000/graphql
  Entorno: Development
========================================
```

> **Importante:** configura `Jwt__Key` con una clave propia de al menos 32 caracteres. La clave de relleno del JSON no es una configuración válida para utilizar la autenticación.

Swagger está disponible en `/swagger` **solo** en entorno de desarrollo.

#### Errores de arranque frecuentes

| Mensaje | Qué comprobar |
|---|---|
| `No password has been provided` | Que hayas cargado el `.env` antes del arranque y que `Postgres__Password` tenga un valor válido. No publiques su contenido. |
| `address already in use` en el puerto 5000 | Que no estén ejecutándose a la vez la API local y la API de Docker. Para usar la local, `docker compose stop api` detiene solo la API de Docker, no las bases de datos. |
| `Access to the path … is denied` al preparar `uploads` | La limpieza de una imagen ha fallado. Comprueba si está abierta y revisa sus permisos; no presupongas que necesita ejecutarse como administrador. El método registra el error y continúa, de modo que ver el banner no garantiza que las imágenes se hayan preparado. |

Los mensajes `warning CS…` son avisos de compilación, no el mismo fallo que una excepción de arranque. Los avisos `NU1903` sobre `SSH.NET` señalan una vulnerabilidad de una dependencia y deben revisarse aparte; que la compilación termine no elimina esa advertencia.

### Opción B: API y bases de datos en Docker

```bash
# Prepara antes el .env según la sección de configuración; no sobrescribas uno existente.

# Levanta la API y sus dependencias (PostgreSQL, MongoDB y Redis)
docker compose up -d --build api

# Ver logs de la API
docker compose logs -f api

# Detener todo
docker compose down
```

La API queda publicada en **`http://localhost:5000`** (mapeo `5000:8080` del contenedor).

**El Compose actual selecciona Development**, pero inyecta `funkoapi` como nombre de ambas bases, sobrescribiendo `funkos_dev` de los JSON. Por tanto, con los valores por defecto, **la API Docker elimina y recrea `funkoapi` en cada arranque**. Esa es también la base usada por Production local: no utilices esta opción si necesitas conservar sus datos. No hay un perfil Docker de Production configurado en este archivo.

### Comandos habituales: API y pruebas

Ejecuta estos comandos desde la **raíz del repositorio**, con Docker Desktop en ejecución y el archivo `.env` preparado. Antes de la primera ejecución de los tests, ejecuta `dotnet restore "FunkoApi.Tests/FunkoApi.Tests.csproj"`: `--no-restore` requiere los paquetes ya restaurados en tu equipo; construir la imagen Docker no los restaura en el host.

```bash
# 1. Construir y levantar la API y sus dependencias
docker compose up -d --build api

# 2. Ejecutar las pruebas unitarias y de integración
dotnet test "FunkoApi.Tests/FunkoApi.Tests.csproj" --no-restore

# 3. Ejecutar las pruebas E2E con Bruno contra la API levantada
docker compose -f "FunkoApi.Tests.E2E/compose.bruno.yaml" run --rm bruno
```

Espera a que la API termine de inicializarse antes de ejecutar Bruno (puedes consultar `docker compose logs -f api` o abrir `http://localhost:5000/swagger`). Las pruebas de integración crean sus propios contenedores con Testcontainers; las E2E sí utilizan la API publicada en el puerto 5000 y modifican sus datos de prueba.

---

<a id="configuracion"></a>
## ⚙️ Configuración de la aplicación

La configuración se distribuye en tres ficheros:

| Fichero | Uso |
|---------|-----|
| `appsettings.json` | Valores por defecto |
| `appsettings.Development.json` | Nombre de las bases `funkos_dev`, API, CORS y niveles de log |
| `appsettings.Production.json` | Nombre de las bases `funkoapi`, hosts locales, API, CORS y niveles de log; Docker inyecta sus propios valores |

### Secciones de `appsettings.json`

| Sección | Claves | Para qué sirve |
|---------|--------|----------------|
| `Api` | `Nombre`, `Mensaje` | Configuración tipada (`ApiConfig`) que se muestra al arrancar |
| `Cache` | `DefaultExpirationMinutes`, `SlidingExpirationMinutes` | TTL de las entradas en caché |
| `Postgres` | `Host`, `Port`, `Database`, `User`, `Password` | Conexión a PostgreSQL |
| `MongoDb` | `Host`, `Port`, `Database`, `User`, `Password` | Conexión a MongoDB |
| `Redis` | `Host`, `Port`, `RedisInstanceName`, `Password` | Conexión y prefijo de claves de Redis |
| `Storage` | `UploadPath`, `MaxFileSize`, `AllowedExtensions`, `AllowedContentTypes`, `DefaultImage`, `Folders` | Reglas de subida de imágenes |
| `Jwt` | `Key`, `Issuer`, `Audience`, `ExpireMinutes` | Firma y validación del token (`ExpirationMinutes`, 30 por defecto) |
| `Cors` | `AllowedOrigins` | Orígenes permitidos para el cliente del navegador y SignalR |
| `Email` | `Host`, `Port`, `Username`, `Password`, `From`, `DisplayName`, `Security`, `UseSmtp` | Configuración SMTP; `UseSmtp` permite activar o desactivar el envío real |
| `Logging` / `Serilog` | `LogLevel`, `MinimumLevel`, `WriteTo` | Niveles de log y *sinks* |

### Sobrescritura en tiempo de ejecución

Cualquier clave se puede sobrescribir con variables de entorno usando `__` (doble guion bajo) en lugar de `:`:

```bash
Jwt__Key=...
Postgres__Host=localhost
Postgres__Port=5432
MongoDb__Host=localhost
Redis__Host=localhost
```

### Configuración tipada

| Clase | Sección | Se consume con |
|-------|---------|----------------|
| `ApiConfig` | `Api` | `IOptions<ApiConfig>` en `Program.cs` |
| `JwtSettings` | `Jwt` | `IOptions<JwtSettings>` en `IdentityConfig` |
| `StorageSettings` | `Storage` | `IOptions<StorageSettings>` en `StorageConfig` y `EfcSeeder` |
| `CacheOptions` | `Cache` | `IOptions<CacheOptions>` en `RedisCacheService` y los servicios de negocio |

> **Expiración JWT:** actualmente `appsettings.json` declara `Jwt:ExpireMinutes`, pero `JwtSettings` y `AuthService` usan `ExpirationMinutes`. Para cambiar la duración efectiva utiliza `Jwt__ExpirationMinutes`; si no se configura, se mantienen los 30 minutos por defecto.

---

<a id="arquitectura"></a>
## 🏗️ Arquitectura y patrones

El proyecto sigue una **arquitectura en capas estilo Onion-like**, donde las dependencias apuntan hacia el dominio:

```
Presentation (Controllers)
    ↓ depende de
Application (Services, DTOs, Mappers)
    ↓ depende de
Domain (Models, Errors, Result)
    ↑ implementado por
Infrastructure (EF Core, MongoDB, Redis, Storage, Identity, Config)
```

### Principios aplicados

| Principio | Cómo se aplica |
|-----------|----------------|
| **SRP** | Controladores delgados: solo traducen HTTP → servicio → respuesta. La lógica vive en los servicios. |
| **DIP** | Inyección por constructor con interfaces (`IXxxService`, `IXxxRepository`, `ICacheService`, `IStorageService`). |
| **OCP / ISP** | `ICrud<T, TKey>` define el contrato mínimo; cada repositorio añade solo lo que necesita (`GetByName`, `ExistsById`, `ExistAnyFunkoProveedor`…). |
| **ROP** | `Result<T, DomainError>` como tipo de retorno en servicios, con traducción centralizada a HTTP en `DomainErrorExtensions.ToHttpResult<T>()`. |
| **Repository** | `ICrud<T, TKey>` + `ICategoriaRepository`, `IFunkoRepository`, `IProveedorRepository`. |
| **Service Layer** | `ICrudService<TValue, TKey, TCreate, TUpdate>`, `IFunkoService`, `ICategoriaService`, `IProveedorService`, `IAuthService`. |
| **DTOs separados** | `Create*Dto`, `Update*Dto` y de lectura. En POST/PUT de Funkos se construyen dentro de la acción: sus Data Annotations no se validan automáticamente. |
| **Mapeo explícito** | Extensiones estáticas en `Mappers/` (`ToDto`, `ToModel`), sin AutoMapper. |
| **Composition Root** | Todo el registro de DI vive en `Infrastructure/*Config.cs` y se encadena en `Program.cs`. |

### Registro de dependencias

`Program.cs` encadena las extensiones del *Composition Root*:

```csharp
builder.Services
    .AddCache(builder.Configuration)        // Redis + ICacheService
    .AddDatabases(builder.Configuration)    // PostgreSQL (EF Core) + MongoDB
    .AddSeeders(builder.Configuration)      // EfcSeeder, MongoSeeder
    .AddRepositories(builder.Configuration) // IFunkoRepository, ICategoriaRepository, IProveedorRepository
    .AddStorage(builder.Configuration)      // IStorageService
    .AddServices();                         // Servicios de negocio
```

`AddIdentityConfiguration` se registra aparte porque necesita también `IWebHostEnvironment`.

| Extensión | Fichero | Registra |
|-----------|---------|----------|
| `AddDatabases` | `Infrastructure/DatabaseConfig.cs` | `AppDbContext`, `IMongoClient`, `IMongoDatabase`, `DbContextMongo` |
| `AddIdentityConfiguration` | `Infrastructure/IdentityConfig.cs` | Identity, JWT Bearer, autorización |
| `AddCache` | `Infrastructure/CacheConfig.cs` | `IDistributedCache`, `IConnectionMultiplexer`, `ICacheService` |
| `AddStorage` | `Infrastructure/StorageConfig.cs` | `IStorageService` |
| `AddRepositories` | `Infrastructure/RepositoriesConfig.cs` | Repositorios (`Scoped`) |
| `AddServices` | `Infrastructure/ServicesConfig.cs` | Servicios de negocio (`Scoped`) |
| `AddSeeders` | `Infrastructure/SeederConfig.cs` | `EfcSeeder`, `MongoSeeder` |

### Piezas destacadas

- **`Factory/ConnectionStringGenerator`**: construye cadenas de conexión para PostgreSQL, MongoDB y Redis a partir de componentes (`BuildPostgres`, `BuildMongoDb`, `BuildRedis`), evitando cadenas *hardcodeadas*.
- **`Generators/YouTubeIdValueGenerator`**: `ValueGenerator<string>` de EF Core que produce IDs de 11 caracteres (`A-Z`, `a-z`, `0-9`, `-_`) para `Categoria`.
- **`Dto/PagedResponse<T>` + `PageMetadata`**: contrato de paginación (`Data`, `Page`, `PageSize`, `TotalPages`, `TotalItems`).
- **`Helpers/PaginationHelper`**: genera la cabecera `Link` (`first`, `prev`, `next`, `last`) según RFC 5988.
- **`Services/Funko/Transactions/FunkoTransactionsService`**: transacciones explícitas de EF Core para actualización y borrado físico. Las operaciones de archivos se realizan aparte; no constituyen una transacción conjunta con PostgreSQL.

---

<a id="seguridad"></a>
## 🔐 Seguridad (JWT y roles)

- **Autenticación**: JWT simétrico con `HMAC-SHA256`. La clave viene de `Jwt:Key` (variable de entorno `Jwt__Key`).
- **Identity**: `AppUser : IdentityUser<long>`, con email único, contraseña de mínimo 8 caracteres, dígito obligatorio y bloqueo tras varios intentos fallidos. Los roles viven en la tabla de unión `AspNetUserRoles`.
- **Login**: `FindByEmailAsync` → comprobación de contraseña → `GetRolesAsync` → emisión del token con los *claims* de rol.
- **Autorización**: atributos `[Authorize]`, `[Authorize(Roles = "ADMIN")]` y `[Authorize(Roles = "ADMIN,USER")]` en cada acción.
- **Manejo de excepciones HTTP**: `GlobalExceptionHandler` (`Middleware/ExceptionHandlerMiddleware.cs`) captura las excepciones que llegan a ese middleware durante una petición y registra un `errorId`. No trata las excepciones de inicialización ni garantiza capturar los errores del worker. Devuelve una respuesta como esta para los errores no contemplados específicamente:

  ```json
  {
    "errorId": "a1b2c3d4",
    "status": 500,
    "message": "Ha ocurrido un error interno",
    "timestamp": "2026-01-01T12:00:00.0000000Z",
    "path": "/api/funkos",
    "method": "GET"
  }
  ```

  Mapeo de excepciones: `ArgumentException` → 400, `UnauthorizedAccessException` → 401, `TimeoutException` → 408, `DbUpdateException` → 409, resto → 500.

- **Validación de archivos**: `FileSystemStorageService` comprueba tamaño máximo, extensión, `Content-Type` y nombre. El tratamiento de una imagen ausente o vacía depende también de la operación del servicio; no debe interpretarse como una garantía general de validación de rutas.

---

<a id="endpoints"></a>
## 📡 Endpoints

| Controlador | Endpoint | Método | Autorización |
|-------------|----------|--------|--------------|
| `AuthController` | `/api/auth/login` | POST | Público |
| `AuthController` | `/api/auth/signup` | POST | Público |
| `AuthController` | `/api/auth/me` | GET | Autenticado |
| `CategoriasController` | `/api/categorias` | GET | `ADMIN,USER` |
| `CategoriasController` | `/api/categorias/paged?page=1&pageSize=10` | GET | `ADMIN,USER` |
| `CategoriasController` | `/api/categorias/{id}` | GET | `ADMIN,USER` |
| `CategoriasController` | `/api/categorias/nombre?name=...` | GET | `ADMIN,USER` |
| `CategoriasController` | `/api/categorias` | POST | `ADMIN` |
| `CategoriasController` | `/api/categorias/{id}` | PUT | `ADMIN` |
| `CategoriasController` | `/api/categorias/{id}` | DELETE | `ADMIN` |
| `FunkosController` | `/api/funkos` | GET | `ADMIN,USER` |
| `FunkosController` | `/api/funkos/paged?page=1&pageSize=10` | GET | `ADMIN,USER` |
| `FunkosController` | `/api/funkos/{id:long}` | GET | `ADMIN` |
| `FunkosController` | `/api/funkos/nombre/{nombre}` | GET | `ADMIN` |
| `FunkosController` | `/api/funkos` | POST | `ADMIN` |
| `FunkosController` | `/api/funkos/{id:long}` | PUT | `ADMIN` |
| `FunkosController` | `/api/funkos/{id:long}` | DELETE | `ADMIN` |
| `FunkosController` | `/api/funkos/{id:long}/imagen` | PATCH | `ADMIN` |
| `ProveedoresController` | `/api/proveedores` | GET | `ADMIN` |
| `ProveedoresController` | `/api/proveedores/paged?page=1&pageSize=10` | GET | `ADMIN` |
| `ProveedoresController` | `/api/proveedores/{id}` | GET | `ADMIN` |
| `ProveedoresController` | `/api/proveedores` | POST | `ADMIN` |
| `ProveedoresController` | `/api/proveedores/{id}` | PUT | `ADMIN` |
| `ProveedoresController` | `/api/proveedores/{id}` | DELETE | `ADMIN` |

> `ToHttpResult<T>()` traduce errores de dominio a `400`, `401`, `404`, `409`, `422` o `500`. El `403` corresponde a la autorización por roles y no a esa extensión.

### Detalles de las peticiones

- **POST/PUT de Funkos** usan `multipart/form-data` con los campos `nombre`, `precio`, `stock`, `categoriaId`, `proveedorId` y `archivo`. En POST la imagen es opcional; en PUT el servicio exige una imagen nueva. El DTO se construye dentro del controlador, por lo que sus rangos de Data Annotations no se comprueban automáticamente.
- **PATCH de imagen** recibe únicamente el fichero `archivo`; borra la imagen anterior del almacenamiento y guarda la nueva.
- **`/api/funkos/paged`** requiere `ADMIN` o `USER`: `page` se normaliza a mínimo 1 y `pageSize` se limita al rango `1..100`. Devuelve `PagedResponse<FunkoDto>` (`data` y `pagination`) y, si procede, la cabecera `Link`.

### Ejemplo con `curl`

Este bloque utiliza sintaxis Bash (las continuaciones con `\` no son sintaxis PowerShell). También puedes realizar las peticiones desde Swagger en Development o desde la colección de Bruno.

```bash
# 1. Obtener token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"Admin123!"}'

# 2. Usar el token en un endpoint protegido
curl http://localhost:5000/api/funkos \
  -H "Authorization: Bearer <TOKEN>"
```

---

<a id="modelo-datos"></a>
## 🗄️ Modelo de datos y semilla

### PostgreSQL (EF Core)

Contexto `AppDbContext : IdentityDbContext<AppUser, IdentityRole<long>, long>`:

| Entidad | Tabla | Clave | Notas |
|---------|-------|-------|-------|
| `AppUser` | `AspNetUsers` | `long` | Identity |
| `Categoria` | `Categorias` | `string` (11 chars) | `YouTubeIdValueGenerator`, nombre único, máx. 100 |
| `Funko` | `Funkos` | `long` (identidad) | Nombre único, `Precio` con precisión `(18,2)`, FK a `Categoria` |

`Funko.ProveedorId` es un `ObjectId` de MongoDB convertido a `string` en la base relacional mediante `HasConversion`; la integridad entre ambos motores se valida en la capa de servicio.

### MongoDB (driver nativo)

Contexto `DbContextMongo` sobre la colección `proveedores`:

| Entidad | Colección | Clave |
|---------|-----------|-------|
| `Proveedor` | `proveedores` | `ObjectId` |

### Redis

| Uso | Claves |
|-----|--------|
| Caché de Funkos | `funko:{id}` |
| Caché de Categorías | `categoria:{id}` |
| Caché de Proveedores | `proveedor:{id}` |
| Bloqueo distribuido | `lock:{clave}` (TTL 10 s) |

Las entradas usan expiración absoluta y deslizante configuradas en `CacheOptions`. El prefijo se lee de `Redis:RedisInstanceName`; las claves de bloqueo se manejan directamente en Redis. La configuración actual no añade un prefijo diferente por entorno.

### Datos iniciales

| Motor | Contenido |
|-------|-----------|
| PostgreSQL | Categorías `Marvel`, `Star Wars`, `Anime` y Funkos `Iron Man` (14,99 €), `Darth Vader` (16,50 €), `Goku Super Saiyan` (15,00 €) |
| MongoDB | Proveedores `Funko Corp`, `Toy Master`, `Geek Toys` |
| Identity | Usuarios `admin@admin.com` y `user@user.com` con sus roles |
| Storage | Copia imágenes a `uploads/funkos/` solo si existe `SeedImages/funkos/` dentro del ContentRoot del backend. La carpeta `SeedImages/` de la raíz no se utiliza automáticamente desde esa ruta. |

---

<a id="arranque"></a>
## 🔄 Ciclo de arranque

`Program.cs` es el *Composition Root* y ejecuta la siguiente secuencia:

1. Configura **Serilog** (consola + `logs/funkos-.log` con rotación diaria y retención de 7 ficheros).
2. Registra la **configuración tipada** (`ApiConfig`) y **Identity + JWT**.
3. Registra **controladores** (negociación de contenido JSON, sin añadir formateadores XML), **Swagger**, **SignalR** y **CORS**.
4. Encadena las extensiones del *Composition Root* (`AddCache`, `AddDatabases`, `AddSeeders`, `AddRepositories`, `AddStorage`, `AddServices`).
5. Registra **GraphQL**, **rate limiting** y los servicios de **email** con su worker. Construye la app y monta el **pipeline**: `UseSerilogRequestLogging` → `UseGlobalExceptionHandler` → `UseCors` → `UseAuthentication` → `UseAuthorization` → `UseRateLimiter`. Mapea GraphQL, Swagger (solo DEV), controladores y `/hubs/funkos`.
6. Ejecuta `InitializeStorage`: intenta vaciar `uploads/`, recrear las carpetas de `Storage:Folders` y copiar las imágenes desde `SeedImages/` **dentro del ContentRoot del backend**, si esa carpeta existe. Si falla, registra el error y continúa; no garantiza que las imágenes estén disponibles.
7. Ejecuta `InitializeDatabaseAsync`.
8. Muestra el banner con nombre, URL y entorno, y arranca con `RunAsync()`.

> ⚠️ **Aviso importante:** en **Development**, `InitializeDatabaseAsync` deja las bases de datos limpias **en cada arranque**: elimina la base de datos de MongoDB y hace `EnsureDeletedAsync()` + `EnsureCreatedAsync()` en PostgreSQL antes de resembrar. El `compose.yaml` actual utiliza Development, por lo que también ocurre con Docker. En otros entornos se omite esta recreación, pero se ejecuta el seeder de Identity. **No arranques esta configuración con datos que quieras conservar**.

También se prepara el directorio `uploads/`, que se vacía en cada arranque, independientemente del entorno. El pipeline actual no registra `UseStaticFiles`, por lo que no publica esas imágenes como contenido estático.

La separación de bases de datos locales no implica un aislamiento completo: `uploads` y la configuración de Redis se comparten entre las ejecuciones locales de Development y Production. El log `Caché: Redis (Producción)` es un texto fijo de `CacheConfig`, no una comprobación del entorno activo.

---

<a id="estructura"></a>
## 📂 Estructura del proyecto

```
FunkoApi.Back/
├── Program.cs                     # Composition Root: DI, pipeline, arranque
├── Controllers/                   # Auth, Categorias, Funkos, Proveedores
├── Services/                      # Lógica de negocio
│   ├── Auth/                      # AuthService + IAuthService
│   ├── Categoria/                 # CategoriaService + ICategoriaService
│   ├── Common/                    # ICrudService genérico
│   ├── Funko/                     # FunkoServices + IFunkoService
│   │   └── Transactions/          # FunkoTransactionsService
│   └── Proveedor/                 # ProveedorService + IProveedorService
├── Repositories/                  # Acceso a datos
│   ├── Common/                    # ICrud<T, TKey>
│   ├── Categoria/                 # ICategoriaRepository + CategoriaRepository (EF Core)
│   ├── Funko/                     # IFunkoRepository + FunkoRepository (EF Core)
│   └── Proveedor/                 # IProveedorRepository + ProveedorMongoRepository (Mongo)
├── Domain/                        # AppUser, Categoria, Funko, Proveedor (namespace Models)
├── Dto/                           # Contratos de entrada/salida
│   ├── Auth/  Categoria/  Funko/  Proveedor/
│   └── PagedResponse.cs           # PagedResponse<T> + PageMetadata
├── Mappers/                       # ToDto() / ToModel() por agregado
├── Errors/                        # DomainError + errores por área
├── Extensions/                    # DomainErrorExtensions.ToHttpResult()
├── Middleware/                    # ExceptionHandlerMiddleware (GlobalExceptionHandler)
├── Infrastructure/                # Composition Root
│   ├── DatabaseConfig.cs          # EF Core + MongoDB
│   ├── IdentityConfig.cs          # Identity + JWT + autorización
│   ├── CacheConfig.cs             # Redis + ICacheService
│   ├── StorageConfig.cs           # IStorageService
│   ├── RepositoriesConfig.cs      # Repositorios
│   ├── ServicesConfig.cs          # Servicios
│   ├── SeederConfig.cs            # Seeders
│   ├── DatabaseInitializationExtensions.cs
│   └── StorageInitializationExtensions.cs
├── Data/                          # AppDbContext, DbContextMongo, Seed/
│   ├── Seed/Efc/                  # Categorías y Funkos
│   ├── Seed/Identity/             # Roles y usuarios
│   └── Seed/Mongo/                # Proveedores
├── Config/                        # ApiConfig, JwtSettings, StorageSettings
├── Cache/                         # ICacheService, RedisCacheService, CacheOptions
├── Storage/                       # IStorageService, FileSystemStorageService
├── Factory/                       # ConnectionStringGenerator
├── Generators/                    # YouTubeIdValueGenerator, YouTubeIdAttribute
├── Helpers/                       # PaginationHelper (RFC 5988)
├── Conventers/                    # ObjectIdJsonConverter
├── appsettings.json               # Valores por defecto
├── appsettings.Development.json   # Sobrescritura desarrollo
├── appsettings.Production.json    # Sobrescritura producción
├── FunkoApi.Back.csproj
├── FunkoApi.http                  # Peticiones de prueba
└── Dockerfile                     # Multi-stage build .NET 10

FunkoApi.Tests/
├── Cache/  Config/  Controllers/  Data/  Dto/  Errors/
├── Extensions/  Factory/  Generators/  Helpers/  Infrastructure/
├── Mappers/  Middleware/  Models/  Repositories/  Services/  Storage/
└── FunkoApi.Tests.csproj

FunkoApi.Clients/
└── signalr-client-js/index.html    # Cliente manual de SignalR (Live Server)

FunkoApi.Tests.E2E/
├── Bruno/FunkoApi - Tests E2E/     # Colección de peticiones
├── compose.bruno.yaml             # Ejecución de Bruno CLI con Docker
├── environments/cli.local.json    # Entorno para acceder a la API desde Docker
└── reports/                       # Informes generados por Bruno
```

La API también incluye las carpetas `Hub/`, `GraphQl/` y `Services/Email/` para tiempo real, consultas GraphQL y envío de correo, respectivamente.

---

<a id="testing"></a>
## 🧪 Estrategia de testing

Se sigue la pirámide de pruebas: **unitarios como base**, integración con contenedores reales y E2E con Bruno.

### Frameworks

- **NUnit 4.4** + **Moq 4.20** + **FluentAssertions 8.8** para pruebas unitarias.
- **Testcontainers 4.8** (`Testcontainers.PostgreSql`, `Testcontainers.MongoDb`) para integración con PostgreSQL 17 y MongoDB 7 efímeros.
- **Microsoft.AspNetCore.Mvc.Testing** para pruebas de controladores.
- **Bruno** para la colección E2E.

### Categorías de pruebas

Solo las pruebas de integración llevan atributo:

```csharp
[Category("Integration")]
```

El resto son unitarias y no requieren Docker.

### Ejecución

```bash
# Solo pruebas de integración (requiere Docker en ejecución)
dotnet test --filter "Category=Integration"

# Todo excepto integración (rápido, sin Docker)
dotnet test --filter "Category!=Integration"

# Suite completa de pruebas unitarias y de integración (paquetes ya restaurados)
dotnet test "FunkoApi.Tests/FunkoApi.Tests.csproj" --no-restore

# Con cobertura (coverlet.collector)
dotnet test --collect:"XPlat Code Coverage"
```

### Cobertura por área

Más de 50 ficheros de prueba:

| Área | Qué se prueba |
|------|---------------|
| `Services/*` | Reglas de negocio, uso de caché, errores de dominio, transacciones y compensación |
| `Repositories/*` | Repositorios EF Core y Mongo (integración con Testcontainers) |
| `Controllers/*` | Respuestas HTTP, roles y códigos de estado |
| `Mappers/*` | Conversión entidad ↔ DTO |
| `Errors/*`, `Extensions/*` | Errores tipados y traducción a HTTP |
| `Config/*`, `Infrastructure/*` | Registro de DI, binding de opciones, seeders e inicialización |
| `Dto/*` | Validación con Data Annotations |
| `Cache/*` | `Get`, `Set`, `Remove` y `GetOrSet` sobre dobles de `IDistributedCache` |
| `Storage/*` | Validaciones de ficheros y rutas |
| `Middleware/*` | Formato y códigos del manejador global de excepciones |
| `Helpers/*`, `Generators/*`, `Factory/*` | Paginación, generación de IDs y cadenas de conexión |

### Pruebas E2E con Bruno

La colección vive en:

```
FunkoApi.Tests.E2E/Bruno/FunkoApi - Tests E2E/
```

Con Bruno (o su CLI) se ejecuta contra la API levantada, incluyendo el flujo de autenticación (obtener el token en `/api/auth/login` y enviarlo como `Authorization: Bearer <token>` a los endpoints protegidos).

```bash
docker compose -f "FunkoApi.Tests.E2E/compose.bruno.yaml" run --rm bruno
```

Este Compose ejecuta las carpetas de autenticación, categorías, proveedores, funkos y **SignalR HTTP**, usando `FunkoApi.Tests.E2E/environments/cli.local.json`. Dentro del contenedor, la API se alcanza mediante `http://host.docker.internal:5000`. Los informes se guardan en `FunkoApi.Tests.E2E/reports/`: `report.json`, `junit-report.xml` y `report.html`.

**Las comprobaciones HTTP de SignalR no sustituyen la prueba de recepción de eventos en tiempo real**: para ver `FunkoCreado` utiliza el cliente JavaScript descrito a continuación.

---

<a id="signalr"></a>
## 📢 Probar SignalR con Live Server

El cliente está en **[`FunkoApi.Clients/signalr-client-js/index.html`](FunkoApi.Clients/signalr-client-js/index.html)**. El hub se publica en **`http://localhost:5000/hubs/funkos`** y solo admite el rol **`ADMIN`**.

1. Levanta la API con `docker compose up -d --build api` y espera a que termine de arrancar.
2. Abre la raíz del proyecto en **Visual Studio Code**, con la extensión **Live Server** instalada.
3. Busca `FunkoApi.Clients/signalr-client-js/index.html`, haz clic derecho y selecciona **Open with Live Server**. No abras el archivo directamente con `file://`.
4. Utiliza el origen **`http://127.0.0.1:5500`**. Si abriste la raíz del repositorio, la URL será `http://127.0.0.1:5500/FunkoApi.Clients/signalr-client-js/index.html`.
5. En **Iniciar sesión como administrador**, introduce las credenciales del administrador de prueba de la sección **Usuarios de prueba** y pulsa **Iniciar sesión**. El cliente llama a `/api/auth/login` y rellena automáticamente el JWT.
6. En **Conectar a SignalR**, comprueba que la URL sea `http://localhost:5000/hubs/funkos` y pulsa **Conectar**. También puedes pegar manualmente un JWT válido de administrador, **sin el prefijo `Bearer`**.
7. Cuando aparezca **Conectado**, mantén la página abierta y crea un Funko mediante `POST /api/funkos` desde Swagger o Bruno, autenticándote como administrador y utilizando una categoría y un proveedor existentes.
8. Comprueba que aparece **FunkoCreado** en **Notificaciones recibidas**. Al terminar, pulsa **Desconectar**.

> **CORS:** `FunkoApi.Back/appsettings.Development.json` permite exactamente `http://127.0.0.1:5500` y `http://127.0.0.1:3000`. `http://localhost:5500` y otros puertos no están incluidos. Si Live Server abre otro origen, utiliza uno de los permitidos o configura el origen exacto en `Cors:AllowedOrigins` y reinicia/reconstruye la API. En Production la lista está vacía por defecto.

El cliente carga la librería JavaScript de SignalR desde un CDN, por lo que necesita acceso a Internet. Si el JWT caduca, vuelve a iniciar sesión y conecta de nuevo.

Referencia de clase: [18. WebSockets y SignalR](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/18-websockets-signalr.md), apartados 18.5 (JWT y CORS), 18.7 (notificaciones desde servicios), 18.8 (cliente JavaScript) y 18.12 (limitaciones de Bruno CLI). Las rutas, campos y el evento anteriores corresponden a este proyecto.

---

<a id="docker"></a>
## 🐳 Despliegue con Docker

El orquestador está en la raíz (`compose.yaml`) y define cuatro servicios:

| Servicio | Imagen | Contenedor | Puerto |
|----------|--------|------------|--------|
| `postgres` | `postgres:17-alpine` | `funkoapi-postgres` | `5432` |
| `mongodb` | `mongo:7.0` | `funkoapi-mongodb` | `27017` |
| `redis` | `redis:7-alpine` | `funkoapi-redis` | `6379` |
| `api` | build desde `FunkoApi.Back/Dockerfile` | `funkoapi-api` | `5000 → 8080` |

Características del orquestador:

- **`healthcheck`** en las tres bases de datos; la API arranca cuando están sanas (`depends_on.condition: service_healthy`).
- **Volúmenes nombrados** `postgres-data`, `mongodb-data` y `redis-data` para persistir los datos.
- **Redes**: todos los servicios comparten la red *bridge* `funkoapi-network`.
- **Multi-stage build** en el `Dockerfile` (`sdk:10.0` → `aspnet:10.0`), ejecutando como usuario no root (`$APP_UID`).

### Comandos

```bash
# Construir y levantar la API y sus dependencias
docker compose up -d --build api

# Estado de los contenedores
docker compose ps

# Logs de un servicio
docker compose logs -f api

# Detener y eliminar contenedores (mantiene volúmenes)
docker compose down

# PELIGRO: elimina también los volúmenes y sus datos; no es necesario para reiniciar
docker compose down -v
```

<a id="variables-env"></a>
### Variables de entorno (`.env`)

El `.env` de la raíz no se publica. Docker Compose lo lee para los contenedores; la API local necesita cargarlo con el bloque PowerShell de [Inicio rápido](#inicio-rapido). Cada línea tiene formato `CLAVE=valor`, sin comillas, como en el apartado 9.1.4 del profesor.

**Preparación local:** configura los hosts, puertos publicados, usuarios y secretos que lee `DatabaseConfig`, `CacheConfig` e `IdentityConfig`. Las claves de PostgreSQL empiezan por `Postgres__`, no por `PostgresDb__`. Los usuarios y contraseñas deben coincidir con los de los contenedores existentes. No definas los nombres de base en el `.env` local: se seleccionan con los JSON del entorno.

La plantilla `.env.example` incluye las claves de la API local y las de Compose, sin contraseñas ni claves JWT reales. Si cambias un usuario o un puerto, ajusta tanto su clave local como la de Compose indicada en los comentarios. No cambies credenciales esperando que se actualicen automáticamente las bases de datos de volúmenes ya existentes.

**Preparación Docker:** Compose obtiene las contraseñas de `Postgres__Password` y `MongoDb__Password`, y la clave de firma de `Jwt__Key`. Los valores siguientes son una **plantilla documental, no credenciales válidas ni un archivo listo para copiar**; sustituye los marcadores por valores propios:

```env
# PostgreSQL
POSTGRES_USER=admin
POSTGRES_DB=funkoapi
Postgres__Password=<CONTRASEÑA_POSTGRESQL>
POSTGRES_PORT=5432

# MongoDB
MONGO_ROOT_USER=admin
MONGO_DB=funkoapi
MongoDb__Password=<CONTRASEÑA_MONGODB>
MONGO_PORT=27017

# Redis
REDIS_PORT=6379
REDIS_PASSWORD=

# API
Jwt__Key=<CLAVE_PROPIA_DE_AL_MENOS_32_CARACTERES>
```

> Las variables con `__` (doble guion bajo) sustituyen a `:` de la jerarquía de configuración de .NET (`Jwt:Key` → `Jwt__Key`). El `.env` está excluido de Git y de la imagen Docker.

Las variables `POSTGRES_*`, `MONGO_*` y `REDIS_PORT` del ejemplo configuran Compose: no sustituyen por sí solas las claves de conexión que lee la API local. Compose inyecta sus hosts y nombres de base en el contenedor `api`. Su entorno actual es Development, con la recreación de datos descrita anteriormente. El servicio Redis no configura `requirepass`: definir `REDIS_PASSWORD` no activa autenticación por sí solo.

Para el correo, `Email__UseSmtp=true` activa MailKit también en Development. Si quieres la simulación en memoria, utiliza `Email__UseSmtp=false`; no incluyas credenciales SMTP en documentación ni archivos publicados.

---

<a id="usuarios-prueba"></a>
## 👥 Usuarios de prueba

El `IdentitySeeder` crea dos usuarios al inicializar la base de datos:

| Usuario | Email | Password | Rol |
|---------|-------|----------|-----|
| Administrador | `admin@admin.com` | `Admin123!` | `ADMIN` |
| Usuario regular | `user@user.com` | `User123!` | `USER` |

Estos usuarios permiten probar los endpoints protegidos con `[Authorize]` y la obtención del token JWT mediante `/api/auth/login`.

---

<a id="licencia"></a>
## 📄 Licencia

Proyecto educativo desarrollado en el módulo **Entorno Servidor (2DAW)**.

---
