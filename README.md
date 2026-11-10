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

El proyecto resuelve el enunciado didáctico de construir un servicio backend profesional que exponga un catálogo de productos coleccionables (Funkos) mediante una **API REST sobre HTTP**, cubriendo los requisitos típicos de una aplicación real:

- **Autenticación y autorización**: login/registro con ASP.NET Core Identity + JWT, roles `ADMIN` y `USER`, protección por acción con `[Authorize(Roles = "...")]`.
- **Almacenamiento de ficheros**: subida de imágenes de Funkos con validación de extensión, tipo MIME y tamaño, y nombres únicos.
- **Lógica de negocio en servicios**: los controladores son delgados; la orquestación vive en `FunkoServices`, `CategoriaService`, `ProveedorService`, `AuthService` y `FunkoTransactionsService`.
- **Acceso a datos con repositorios**: contrato genérico `ICrud<T, TKey>` y repositorios específicos por agregado.
- **Dos bases de datos**: PostgreSQL (EF Core) para Funciones, Categorías e Identity; MongoDB (driver nativo) para Proveedores.
- **Caché distribuida**: Redis con expiración absoluta y deslizante, más bloqueo distribuido contra *cache stampede*.
- **Manejo de errores tipado**: patrón `Result` con `DomainError` y traducción centralizada a códigos HTTP.
- **Transacciones con compensación**: alta, actualización y borrado de Funkos con rollback en base de datos y eliminación compensatoria de la imagen subida.
- **Paginación estándar**: `PagedResponse<T>` + cabeceras `Link` según RFC 5988.
- **Pruebas**: unitarias (NUnit + Moq + FluentAssertions), integración (Testcontainers con PostgreSQL y MongoDB reales) y E2E (colección de Bruno).

---

## 📑 Tabla de contenidos

| # | Sección |
|---|---------|
| 1 | [✨ Características](#-características) |
| 2 | [🚀 Tecnologías utilizadas](#-tecnologías-utilizadas) |
| 3 | [🏃‍♂️ Inicio rápido](#️-inicio-rápido) |
| 4 | [⚙️ Configuración de la aplicación](#️-configuración-de-la-aplicación) |
| 5 | [🏗️ Arquitectura y patrones](#️-arquitectura-y-patrones) |
| 6 | [🔐 Seguridad (JWT y roles)](#-seguridad-jwt-y-roles) |
| 7 | [📡 Endpoints](#-endpoints) |
| 8 | [🗄️ Modelo de datos y semilla](#️-modelo-de-datos-y-semilla) |
| 9 | [🔄 Ciclo de arranque](#-ciclo-de-arranque) |
| 10 | [📂 Estructura del proyecto](#-estructura-del-proyecto) |
| 11 | [🧪 Estrategia de testing](#-estrategia-de-testing) |
| 12 | [🐳 Despliegue con Docker](#-despliegue-con-docker) |
| 13 | [👥 Usuarios de prueba](#-usuarios-de-prueba) |
| 14 | [📄 Licencia](#-licencia) |

---

## ✨ Características

- 🏪 **CRUD completo**: Funkos, Categorías y Proveedores.
- 🔐 **Autenticación JWT**: login y registro con `UserManager`, contraseñas hasheadas por Identity y token simétrico `HMAC-SHA256`.
- 🔒 **Autorización por roles**: `[Authorize]` y `[Authorize(Roles = "ADMIN,USER")]` a nivel de acción.
- 🖼️ **Almacenamiento de archivos**: `IStorageService` con `FileSystemStorageService`, validación de seguridad y nombres únicos.
- ⚡ **Caché Redis**: `ICacheService` con `RedisCacheService` y `GetOrSetAsync` (lectura → bloqueo distribuido → fábrica → escritura).
- 🗄️ **Multi-base de datos**: PostgreSQL (EF Core 10 + Npgsql) + MongoDB (driver nativo `MongoDB.Driver`).
- 🔁 **Patrón Result**: `CSharpFunctionalExtensions` (`Result<T, DomainError>`) con errores de dominio tipados.
- 📐 **DTOs separados por dirección**: `Create*Dto`, `Update*Dto` y DTOs de lectura; el modelo de dominio nunca se serializa.
- 🗺️ **Mapeadores explícitos**: extensiones `ToDto()` / `ToModel()` en `Mappers/`.
- 🆔 **Identificadores personalizados**: generador de IDs estilo YouTube (11 caracteres) para `Categoria`.
- ⚙️ **Configuración tipada**: `ApiConfig`, `JwtSettings`, `StorageSettings`, `CacheOptions` con `IOptions<T>`.
- 🔗 **Factory de cadenas de conexión**: `ConnectionStringGenerator` construye las cadenas de PostgreSQL, MongoDB y Redis a partir de componentes.
- 📄 **Paginación**: `PagedResponse<T>` con `PageMetadata` y cabecera `Link`.
- 🧪 **Tests**: unitarios, integración con contenedores reales y E2E con Bruno.
- 🐳 **Docker**: `compose.yaml` con PostgreSQL, MongoDB, Redis y la API, con `healthcheck` y volúmenes nombrados.

---

## 🚀 Tecnologías utilizadas

| Capa | Tecnología | Versión |
|------|------------|---------|
| Plataforma | .NET / ASP.NET Core | `net10.0` |
| Lenguaje | C# | 14 |
| ORM relacional | Entity Framework Core | `10.0.14` |
| Proveedor PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL | `10.0.3` |
| Base de datos documentos | MongoDB.Driver (nativo) | `3.12.0` |
| Integración EF + Mongo | MongoDB.EntityFrameworkCore | `10.0.4` |
| Caché | StackExchange.Redis / DistributedCache | `10.0.14` |
| Autenticación | Identity + JwtBearer | `10.0.14` |
| Tokens | Microsoft.IdentityModel.Tokens | `8.23.0` |
| Patrón Result | CSharpFunctionalExtensions | `3.7.0` |
| Documentación API | Swashbuckle.AspNetCore | `10.3.1` |
| Serialización | System.Text.Json | `10.0.12` |
| Logging | Serilog (Console + File) | `4.4.0` |
| Pruebas unitarias | NUnit `4.4.0` + Moq `4.20.72` + FluentAssertions `8.8.0` | — |
| Pruebas de integración | Testcontainers PostgreSql/MongoDb | `4.8.1` |
| Pruebas E2E | Bruno | Colección `FunkoApi - Tests E2E` |
| Contenedores | Docker + compose.yaml | PostgreSQL 17, MongoDB 7.0, Redis 7 |

---

## 🏃‍♂️ Inicio rápido

### Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para las bases de datos y el despliegue completo)

La solución usa el nuevo formato `FunkoApi.slnx`, con dos proyectos:

```
FunkoApi.Back/    → API (ASP.NET Core)
FunkoApi.Tests/   → pruebas (NUnit)
```

### Opción A: desarrollo local

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

# 5. Ejecuta la API pasando los secretos por variables de entorno
#    (en PowerShell)
$env:Jwt__Key="ClaveSecretaMuyLargaYSegura1234567890"
$env:PostgresDb__Host="localhost"
$env:PostgresDb__Port="5432"
$env:PostgresDb__Database="funkoapi"
$env:PostgresDb__User="admin"
$env:PostgresDb__Password="admin123"
$env:MongoDb__Host="localhost"
$env:MongoDb__Port="27017"
$env:MongoDb__Database="funkoapi"
$env:MongoDb__User="admin"
$env:MongoDb__Password="admin123"
$env:Redis__Host="localhost"
$env:Redis__Port="6379"

dotnet run --project FunkoApi.Back
```

Al arrancar, la API imprime en consola el nombre, el entorno y la URL:

```
========================================
  Funko API (DEV)
  Modo desarrollo - depuración activada
  http://localhost:5000
  Swagger: http://localhost:5000/swagger
  Entorno: Development
========================================
```

> **Importante:** `Jwt:Key` debe tener al menos 32 caracteres para `HMAC-SHA256`. Si no se indica, el arranque falla al construir las credenciales de firma.

Swagger está disponible en `/swagger` **solo** en entorno de desarrollo.

### Opción B: todo con Docker (recomendado)

```bash
# Crea el archivo .env con los secretos (ver sección Docker)
cp .env.example .env

# Levanta todos los servicios (incluye la API)
docker compose up -d --build

# Ver logs de la API
docker compose logs -f api

# Detener todo
docker compose down
```

La API queda publicada en **`http://localhost:5000`** (mapeo `5000:8080` del contenedor).

---

## ⚙️ Configuración de la aplicación

La configuración se distribuye en tres ficheros:

| Fichero | Uso |
|---------|-----|
| `appsettings.json` | Valores por defecto |
| `appsettings.Development.json` | Sobrescritura en desarrollo (`Api`, `Logging`, `Serilog`) |
| `appsettings.Production.json` | Sobrescritura en producción (`Api`, hosts Docker, `Logging`) |

### Secciones de `appsettings.json`

| Sección | Claves | Para qué sirve |
|---------|--------|----------------|
| `Api` | `Nombre`, `Mensaje` | Configuración tipada (`ApiConfig`) que se muestra al arrancar |
| `Cache` | `DefaultExpirationMinutes`, `SlidingExpirationMinutes` | TTL de las entradas en caché |
| `Postgres` / `PostgresDb` | `Host`, `Port`, `Database`, `User`, `Password` | Conexión a PostgreSQL |
| `MongoDb` | `Host`, `Port`, `Database`, `User`, `Password` | Conexión a MongoDB |
| `Redis` | `Host`, `Port`, `RedisInstanceName`, `Password` | Conexión y prefijo de claves de Redis |
| `Storage` | `UploadPath`, `MaxFileSize`, `AllowedExtensions`, `AllowedContentTypes`, `DefaultImage`, `Folders` | Reglas de subida de imágenes |
| `Jwt` | `Key`, `Issuer`, `Audience`, `ExpireMinutes` | Firma y validación del token (`ExpirationMinutes`, 30 por defecto) |
| `Logging` / `Serilog` | `LogLevel`, `MinimumLevel`, `WriteTo` | Niveles de log y *sinks* |

### Sobrescritura en tiempo de ejecución

Cualquier clave se puede sobrescribir con variables de entorno usando `__` (doble guion bajo) en lugar de `:`:

```bash
Jwt__Key=...
PostgresDb__Host=localhost
PostgresDb__Port=5432
MongoDb__Host=localhost
Redis__Host=localhost
```

> **Ojo con los nombres:** el código de `DatabaseConfig` lee la sección **`PostgresDb`**, mientras que `appsettings.json` declara `Postgres`. En local hay que definir las variables `PostgresDb__*` (como en el ejemplo de inicio rápido); en Docker ya vienen inyectadas desde `compose.yaml`.

### Configuración tipada

| Clase | Sección | Se consume con |
|-------|---------|----------------|
| `ApiConfig` | `Api` | `IOptions<ApiConfig>` en `Program.cs` |
| `JwtSettings` | `Jwt` | `IOptions<JwtSettings>` en `IdentityConfig` |
| `StorageSettings` | `Storage` | `IOptions<StorageSettings>` en `StorageConfig` y `EfcSeeder` |
| `CacheOptions` | `Redis` | `IOptions<CacheOptions>` en `RedisCacheService` y los servicios de negocio |

---

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
| **DTOs separados** | `Create*Dto`, `Update*Dto` y de lectura. Validación con Data Annotations antes de llegar al servicio. |
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
- **`Services/Funko/Transactions/FunkoTransactionsService`**: transacciones explícitas de EF Core con compensación de ficheros.

---

## 🔐 Seguridad (JWT y roles)

- **Autenticación**: JWT simétrico con `HMAC-SHA256`. La clave viene de `Jwt:Key` (variable de entorno `Jwt__Key`).
- **Identity**: `AppUser : IdentityUser<long>`, con email único, contraseña de mínimo 8 caracteres, dígito obligatorio y bloqueo tras varios intentos fallidos. Los roles viven en la tabla de unión `AspNetUserRoles`.
- **Login**: `FindByEmailAsync` → comprobación de contraseña → `GetRolesAsync` → emisión del token con los *claims* de rol.
- **Autorización**: atributos `[Authorize]`, `[Authorize(Roles = "ADMIN")]` y `[Authorize(Roles = "ADMIN,USER")]` en cada acción.
- **Manejo global de excepciones**: `GlobalExceptionHandler` (`Middleware/ExceptionHandlerMiddleware.cs`) captura cualquier excepción no controlada, registra un `errorId` y devuelve JSON estandarizado sin filtrar detalles internos:

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

- **Validación de archivos**: `FileSystemStorageService` rechaza archivos vacíos, demasiado grandes, extensiones o `Content-Type` no permitidos y nombres con *path traversal* (`..`, `/`, `\`).

---

## 📡 Endpoints

| Controlador | Endpoint | Método | Autorización |
|-------------|----------|--------|--------------|
| `AuthController` | `/api/auth/login` | POST | Público |
| `AuthController` | `/api/auth/signup` | POST | Público |
| `AuthController` | `/api/auth/me` | GET | Autenticado |
| `CategoriasController` | `/api/categorias` | GET | `ADMIN,USER` |
| `CategoriasController` | `/api/categorias/{id}` | GET | `ADMIN,USER` |
| `CategoriasController` | `/api/categorias/nombre?name=...` | GET | `ADMIN,USER` |
| `CategoriasController` | `/api/categorias` | POST | `ADMIN` |
| `CategoriasController` | `/api/categorias/{id}` | PUT | `ADMIN` |
| `CategoriasController` | `/api/categorias/{id}` | DELETE | `ADMIN` |
| `FunkosController` | `/api/funkos` | GET | `ADMIN,USER` |
| `FunkosController` | `/api/funkos/paged?page=1&pageSize=10` | GET | **Público** |
| `FunkosController` | `/api/funkos/{id:long}` | GET | `ADMIN` |
| `FunkosController` | `/api/funkos/{nombre}` | GET | `ADMIN` |
| `FunkosController` | `/api/funkos` | POST | `ADMIN` |
| `FunkosController` | `/api/funkos/{id:long}` | PUT | `ADMIN` |
| `FunkosController` | `/api/funkos/{id:long}` | DELETE | `ADMIN` |
| `FunkosController` | `/api/funkos/{id:long}/imagen` | PATCH | `ADMIN` |
| `ProveedoresController` | `/api/proveedores` | GET | `ADMIN` |
| `ProveedoresController` | `/api/proveedores/{id}` | GET | `ADMIN` |
| `ProveedoresController` | `/api/proveedores` | POST | `ADMIN` |
| `ProveedoresController` | `/api/proveedores/{id}` | PUT | `ADMIN` |
| `ProveedoresController` | `/api/proveedores/{id}` | DELETE | `ADMIN` |

> Los errores de dominio se traducen automáticamente a códigos HTTP (`400`, `401`, `403`, `404`, `409`, `500`) mediante `ToHttpResult<T>()`, sin `switch` en cada controlador.

### Detalles de las peticiones

- **POST/PUT de Funkos** usan `multipart/form-data` con los campos `nombre`, `precio`, `stock`, `categoriaId`, `proveedorId` y el fichero opcional `archivo`.
- **PATCH de imagen** recibe únicamente el fichero `archivo`; borra la imagen anterior del almacenamiento y guarda la nueva.
- **`/api/funkos/paged`** es paginación pública: `page` se normaliza a mínimo 1 y `pageSize` se limita al rango `1..100`. Devuelve `PagedResponse<FunkoDto>` y, si procede, la cabecera `Link`.

### Ejemplo con `curl`

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

Las entradas usan expiración absoluta y deslizante configuradas en `CacheOptions`, con prefijo `FunkoApi` (`RedisInstanceName`).

### Datos iniciales

| Motor | Contenido |
|-------|-----------|
| PostgreSQL | Categorías `Marvel`, `Star Wars`, `Anime` y Funkos `Iron Man` (14,99 €), `Darth Vader` (16,50 €), `Goku Super Saiyan` (15,00 €) |
| MongoDB | Proveedores `Funko Corp`, `Toy Master`, `Geek Toys` |
| Identity | Usuarios `admin@admin.com` y `user@user.com` con sus roles |
| Storage | Imágenes de ejemplo copiadas desde `SeedImages/` a `uploads/funkos/` |

---

## 🔄 Ciclo de arranque

`Program.cs` es el *Composition Root* y ejecuta la siguiente secuencia:

1. Configura **Serilog** (consola + `logs/funkos-.log` con rotación diaria y retención de 7 ficheros).
2. Registra la **configuración tipada** (`ApiConfig`) y **Identity + JWT**.
3. Registra **controladores** (negociación de contenido y formateadores XML) y **Swagger**.
4. Encadena las extensiones del *Composition Root* (`AddCache`, `AddDatabases`, `AddSeeders`, `AddRepositories`, `AddStorage`, `AddServices`).
5. Construye la app y monta el **pipeline**: `UseSerilogRequestLogging` → `UseGlobalExceptionHandler` → `UseAuthentication` → `UseAuthorization` → Swagger (solo DEV) → `MapControllers`.
6. Ejecuta `InitializeStorage`: vacía `uploads/`, recrea las carpetas declaradas en `Storage:Folders` y copia las imágenes de `SeedImages/`.
7. Ejecuta `InitializeDatabaseAsync`.
8. Muestra el banner con nombre, URL y entorno, y arranca con `RunAsync()`.

> ⚠️ **Aviso importante:** `InitializeDatabaseAsync` deja las bases de datos limpias **en cada arranque**: elimina la base de datos de MongoDB y hace `EnsureDeletedAsync()` + `EnsureCreatedAsync()` en PostgreSQL antes de resembrar. Es intencionado para el aula (estado reproducible), pero **no lo uses con datos que quieras conservar**.

También se prepara el directorio `uploads/`, que se sirve como contenido estático y se vacía en cada arranque.

---

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
├── Models/                        # AppUser, Categoria, Funko, Proveedor
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
```

---

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

# Suite completa
dotnet test

# Con cobertura (coverlet.msbuild)
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

---

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
# Construir y levantar todo
docker compose up -d --build

# Estado de los contenedores
docker compose ps

# Logs de un servicio
docker compose logs -f api

# Detener y eliminar contenedores (mantiene volúmenes)
docker compose down

# Detener y eliminar también los volúmenes de datos
docker compose down -v
```

### Variables de entorno (`.env`)

`compose.yaml` lee los secretos del archivo `.env` de la raíz. Ejemplo mínimo:

```env
# PostgreSQL
POSTGRES_USER=admin
POSTGRES_DB=funkoapi
Postgres__Password=admin123
POSTGRES_PORT=5432

# MongoDB
MONGO_ROOT_USER=admin
MONGO_DB=funkoapi
MongoDb__Password=admin123
MONGO_PORT=27017

# Redis
REDIS_PORT=6379
REDIS_PASSWORD=

# API
Jwt__Key=ClaveSecretaMuyLargaYSegura1234567890
```

> Las variables con `__` (doble guion bajo) sustituyen a `:` de la jerarquía de configuración de .NET (`Jwt:Key` → `Jwt__Key`). El `.env` está excluido de Git y de la imagen Docker.

---

## 👥 Usuarios de prueba

El `IdentitySeeder` crea dos usuarios al inicializar la base de datos:

| Usuario | Email | Password | Rol |
|---------|-------|----------|-----|
| Administrador | `admin@admin.com` | `Admin123!` | `ADMIN` |
| Usuario regular | `user@user.com` | `User123!` | `USER` |

Estos usuarios permiten probar los endpoints protegidos con `[Authorize]` y la obtención del token JWT mediante `/api/auth/login`.

---

## 📄 Licencia

Proyecto educativo desarrollado con fines didácticos en el módulo **Entorno Servidor (2DAW)**. Puedes usarlo, modificarlo y compartirlo citando la fuente.

---
