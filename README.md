# FunkoApi.Back 🛍️

[![.NET](https://img.shields.io/badge/.NET-10-blue)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10-blue)](https://dotnet.microsoft.com/en-us/apps/aspnet)
[![C#](https://img.shields.io/badge/C%23-14-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10-blue)](https://docs.microsoft.com/en-us/ef/core/)
[![MongoDB](https://img.shields.io/badge/MongoDB-7-green)](https://www.mongodb.com/)
[![Redis](https://img.shields.io/badge/Redis-7-red)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-blue)](https://docs.docker.com/compose/)

**API REST didáctica de catálogo de Funkos** desarrollada con **.NET 10, ASP.NET Core 10 y C# 14** como parte del módulo *Entorno Servidor* (2DAW, Tema 2).

Implementa una API RESTful completa sobre HTTP con **autenticación JWT, autorización por roles, almacenamiento de ficheros, servicios de negocio, repositorios, dos bases de datos (PostgreSQL + MongoDB) y caché Redis**. Incluye pruebas unitarias, de integración (Testcontainers) y E2E con Bruno.

---

## 🎯 Descripción del problema

El proyecto resuelve el enunciado didáctico de construir un servicio backend profesional que exponga un catálogo de productos coleccionables (Funkos) mediante una **API REST sobre HTTP**, cubriendo los requisitos típicos de una aplicación real:

- **Autenticación y autorización**: login/registro con JWT, roles `ADMIN` y `USER`, protección de endpoints con `[Authorize]`.
- **Almacenamiento de ficheros**: subida de imágenes de Funkos con validación de seguridad (extensión, tipo MIME, tamaño, nombre).
- **Lógica de negocio en servicios**: los controladores son delgados; la orquestación vive en servicios (`FunkoServices`, `CategoriaService`, `ProveedorService`, `AuthService`).
- **Acceso a datos con repositorios**: patrón Repository sobre dos tecnologías distintas.
- **Dos bases de datos**: PostgreSQL (EF Core) para entidades relacionales y MongoDB (driver nativo) para proveedores.
- **Caché distribuida**: Redis para lecturas frecuentes.
- **Manejo de errores tipado**: patrón `Result` con `DomainError` y traducción centralizada a códigos HTTP.
- **Transacciones con compensación**: borrado/actualización de Funkos con rollback de base de datos y eliminación compensatoria de archivos.
- **Pruebas**: unitarias (NUnit + Moq + FluentAssertions), integración (Testcontainers con PostgreSQL y MongoDB reales) y E2E (colección de Bruno).

---

## 📑 Tabla de contenidos

| # | Sección |
|---|---------|
| 1 | [✨ Características](#-características) |
| 2 | [🚀 Tecnologías utilizadas](#-tecnologías-utilizadas) |
| 3 | [🏃‍♂️ Inicio rápido](#️-inicio-rápido) |
| 4 | [🧪 Estrategia de testing](#-estrategia-de-testing) |
| 5 | [🏗️ Arquitectura y patrones](#️-arquitectura-y-patrones) |
| 6 | [🔐 Seguridad (JWT y roles)](#-seguridad-jwt-y-roles) |
| 7 | [📡 Endpoints](#-endpoints) |
| 8 | [🗄️ Entidades por base de datos](#️-entidades-por-base-de-datos) |
| 9 | [📂 Estructura del proyecto](#-estructura-del-proyecto) |
| 10 | [🐳 Despliegue con Docker](#-despliegue-con-docker) |
| 11 | [👥 Usuarios de prueba](#-usuarios-de-prueba) |
| 12 | [📄 Licencia](#-licencia) |

---

## ✨ Características

- 🏪 **CRUD completo**: Funkos, Categorías y Proveedores.
- 🔐 **Autenticación JWT**: login y registro con `UserManager` / `SignInManager` de ASP.NET Core Identity.
- 🔒 **Autorización por roles**: políticas y atributo `[Authorize(Roles = "ADMIN")]` / `"USER"`.
- 🖼️ **Almacenamiento de archivos**: `IStorageService` con `FileSystemStorageService`, validación de seguridad y nombres únicos.
- ⚡ **Caché Redis**: `ICacheService` con `RedisCacheService`, expiración absoluta y deslizante, bloqueo contra *cache stampede*.
- 🗄️ **Multi-base de datos**: PostgreSQL (EF Core) + MongoDB (driver nativo, sin `IdentityDbContext`).
- 🔁 **Patrón Result**: `CSharpFunctionalExtensions` (`Result<T, DomainError>`) con errores de dominio tipados (`NotFoundError`, `ConflictError`, `BusinessRuleError`, etc.).
- 📐 **DTOs separados por dirección**: `Create*Dto`, `Update*Dto`, `*Dto` (lectura). El dominio nunca se serializa directamente.
- 🗺️ **Mapeadores manuales**: extensiones `ToDto()` / `ToModel()` para entidad ↔ DTO.
- ⚙️ **Transacciones con compensación**: `FunkoTransactionsService` con `BeginTransactionAsync`, rollback de EF Core y eliminación de imagen subida si falla.
- 🧪 **Tests**: unitarios, de integración con contenedores reales y E2E con Bruno.
- 🐳 **Docker**: `compose.yaml` con PostgreSQL, MongoDB, Redis y API.

---

## 🚀 Tecnologías utilizadas

| Capa | Tecnología | Versión / Uso |
|------|------------|---------------|
| Plataforma | .NET / ASP.NET Core | 10.0 |
| Lenguaje | C# | 14 |
| ORM relacional | Entity Framework Core + Npgsql | PostgreSQL |
| Base de datos documentos | MongoDB.Driver | Driver nativo (sin EF Core) |
| Caché | StackExchange.Redis | Redis distribuido |
| Autenticación | ASP.NET Core Identity + JWT Bearer | HMAC-SHA256 |
| Patrón Result | CSharpFunctionalExtensions | `Result<T, TError>` |
| Documentación API | Swashbuckle.AspNetCore | Swagger / OpenAPI |
| Serialización JSON | System.Text.Json | Con `ObjectIdJsonConverter` para MongoDB |
| Logging | Serilog | Consola y archivo rodante |
| Pruebas unitarias | NUnit + Moq + FluentAssertions | AAA estricto |
| Pruebas de integración | Testcontainers | PostgreSQL + MongoDB reales |
| Pruebas E2E | Bruno | Colección `FunkoApi - Tests E2E` |
| Contenedores | Docker + compose.yaml | Orquestación de servicios |

---

## 🏃‍♂️ Inicio rápido

### Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para bases de datos y despliegue completo)

### Opción A: Desarrollo local

```bash
# 1. Clona el repositorio
git clone https://github.com/LSPlaura/FunkoApiProyecto

# 2. Sitúate en el proyecto API
cd "C:\Users\TU_USUARIO\RiderProjects\FunkoApiProyecto"

# 3. Restaura paquetes y compila
dotnet restore
dotnet build

# 4. Levanta la infraestructura (PostgreSQL, MongoDB, Redis)
docker compose -f compose.yaml up -d postgres mongodb redis

# 5. Ejecuta la API
dotnet run
```

La API escucha por defecto en `http://localhost:5000` (puerto configurable en `compose.yaml` / `launchSettings`).  
Swagger está disponible en `/swagger` solo en entorno de desarrollo.

### Opción B: Todo con Docker (recomendado)

Desde la raíz de la solución:

```bash
# Copia el ejemplo de variables de entorno si no existe .env
cp .env.example .env   # o crea .env con tus secretos

# Levanta todos los servicios (incluye la API)
docker compose up -d --build

# Ver logs de la API
docker compose logs -f api

# Detener todo
docker compose down
```

> **Nota:** Las variables secretas (`Jwt__Key`, `PostgresDb__Password`, `MongoDb__Password`, etc.) van en `.env` y no se suben a Git (están en `.gitignore` y `.dockerignore`). Los nombres de variable usan `__` en lugar de `:` por limitación de YAML/shell (ej. `Jwt__Key`, `Cache__RedisConfiguration`).

---

## 🧪 Estrategia de testing

Se sigue la pirámide de pruebas: **unitarios como base**, integración con contenedores reales y E2E con Bruno.

### Frameworks

- **NUnit 4** + **Moq 4** + **FluentAssertions 8** (unitarios)
- **Testcontainers 4** (PostgreSQL y MongoDB efímeros para integración)
- **Bruno** (colección `FunkoApi - Tests E2E`)

### Ejecución

```bash
# Solo tests unitarios (rápidos, sin Docker)
dotnet test --filter "FullyQualifiedName~Unit"

# Tests de integración (requiere Docker en ejecución)
dotnet test --filter "FullyQualifiedName~Integration"

# Todo sin integración
SKIP_INTEGRATION_TESTS=true dotnet test
```

### Pruebas E2E con Bruno

La colección vive en:

```
FunkoApi.Tests.E2E/Bruno/FunkoApi - Tests E2E/
```

Con Bruno (o su CLI) puedes ejecutar la colección contra la API levantada, incluyendo el flujo de autenticación (obtener `token` en `/api/auth/login` y enviarlo en el header `Authorization: Bearer <token>` a los endpoints protegidos).

---

## 🏗️ Arquitectura y patrones

El proyecto sigue una **arquitectura en capas estilo Onion-like**, donde las dependencias apuntan hacia el dominio:

```
Presentation (Controllers)
    ↓ depende de
Application (Services)
    ↓ depende de
Domain (Models, Errors, Result)
    ← depende de
Infrastructure (EF Core, MongoDB, Redis, Storage, Identity)
```

### Principios aplicados

| Principio | Cómo se aplica |
|-----------|----------------|
| **SRP** | Controladores delgados: solo traducen HTTP → servicio → respuesta. La lógica de negocio vive en los servicios. |
| **DIP** | Inyección por constructor con interfaces (`IXxxService`, `IXxxRepository`). Nada de `new` de dependencias concretas en el dominio. |
| **ROP** | `Result<T, DomainError>` como tipo de retorno en servicios; errores tipados y traducidos centralizadamente a HTTP mediante `DomainErrorExtensions.ToHttpResult<T>()`. |
| **Repository** | `ICrud<T, TKey>` genérico más interfaces específicas (`IFunkoRepository`, `ICategoriaRepository`, `IProveedorRepository`). |
| **DTOs separados** | `Create*Dto`, `Update*Dto`, `*Dto` de lectura. Validación con Data Annotations antes de llegar al servicio. |
| **Mapeo explícito** | Extensiones estáticas en `Mappers/` (`ToDto`, `ToModel`) sin AutoMapper. |

---

## 🔐 Seguridad (JWT y roles)

- **Autenticación**: JWT simétrico con `HMAC-SHA256`. La clave proviene de variable de entorno `Jwt__Key`.
- **Identity**: `AppUser : IdentityUser<long>` con email único, roles en tabla de unión `AspNetUserRoles` (no como propiedad de la entidad).
- **Login**: `FindByEmailAsync` → `CheckPasswordSignInAsync(..., lockoutOnFailure: false)` → `GetRolesAsync` → generación de token.
- **Autorización**: atributos `[Authorize(Roles = "ADMIN")]` y `[Authorize(Roles = "ADMIN,USER")]` en controladores y acciones.
- **Manejo de errores global**: `GlobalExceptionHandler` convierte excepciones en respuestas JSON estandarizadas sin filtrar detalles internos.
- **Validación de archivos**: `FileSystemStorageService` rechaza archivos vacíos, demasiado grandes, extensiones no permitidas, `Content-Type` no permitido y nombres con traversal (`..`, `/`, `\`).

---

## 📡 Endpoints

| Controlador | Endpoint | Método | Rol |
|-------------|----------|--------|-----|
| `AuthController` | `/api/auth/login` | POST | Público |
| `AuthController` | `/api/auth/signup` | POST | Público |
| `AuthController` | `/api/auth/me` | GET | Autenticado |
| `CategoriasController` | `/api/categorias` | GET | ADMIN, USER |
| `CategoriasController` | `/api/categorias/{id}` | GET | ADMIN, USER |
| `CategoriasController` | `/api/categorias/nombre` | GET | ADMIN, USER |
| `CategoriasController` | `/api/categorias` | POST | ADMIN |
| `CategoriasController` | `/api/categorias/{id}` | PUT | ADMIN |
| `CategoriasController` | `/api/categorias/{id}` | DELETE | ADMIN |
| `FunkosController` | `/api/funkos` | GET | ADMIN, USER |
| `FunkosController` | `/api/funkos/{id:long}` | GET | ADMIN |
| `FunkosController` | `/api/funkos/{nombre}` | GET | ADMIN |
| `FunkosController` | `/api/funkos/paged` | GET | Público |
| `FunkosController` | `/api/funkos` | POST | ADMIN |
| `FunkosController` | `/api/funkos/{id:long}` | PUT | ADMIN |
| `FunkosController` | `/api/funkos/{id:long}` | DELETE | ADMIN |
| `FunkosController` | `/api/funkos/{id:long}/imagen` | PATCH | ADMIN |
| `ProveedoresController` | `/api/proveedores` | GET | ADMIN |
| `ProveedoresController` | `/api/proveedores/{id}` | GET | ADMIN |
| `ProveedoresController` | `/api/proveedores` | POST | ADMIN |
| `ProveedoresController` | `/api/proveedores/{id}` | PUT | ADMIN |
| `ProveedoresController` | `/api/proveedores/{id}` | DELETE | ADMIN |

> Los errores de dominio se traducen automáticamente a códigos HTTP (`404`, `400`, `401`, `409`, `500`) mediante `ToHttpResult<T>()` sin `switch` en cada controlador.

---

## 🗄️ Entidades por base de datos

### PostgreSQL (EF Core)
Contexto `AppDbContext` (`IdentityDbContext<AppUser, IdentityRole<long>, long>`):

| Entidad | Tabla | Clave |
|---------|-------|-------|
| `AppUser` | `AspNetUsers` | `long` (Identity) |
| `Categoria` | `Categorias` | `string` (YouTube ID de 11 chars) |
| `Funko` | `Funkos` | `long` |

### MongoDB (driver nativo)
Contexto `DbContextMongo` sobre `IMongoCollection<Proveedor>` (colección `"proveedores"`):

| Entidad | Colección | Clave |
|---------|-----------|-------|
| `Proveedor` | `proveedores` | `ObjectId` |

### Redis
Caché distribuida para lecturas frecuentes de categorías y funkos, con bloqueo distribuido contra *cache stampede*.

---

## 📂 Estructura del proyecto

```
FunkoApi.Back/
├── Program.cs                    # Composition Root: DI, middleware, endpoints
├── Controllers/                  # Auth, Categorias, Funkos, Proveedores
├── Services/                     # Lógica de negocio (Auth, Funko, Categoria, Proveedor)
│   ├── Funko/Transactions/       # FunkoTransactionsService (transacciones + compensación)
├── Repositories/                 # Acceso a datos (EF Core + Mongo nativo)
│   ├── Categoria/, Funko/, Proveedor/, Common/
├── Models/                       # Entidades de dominio (AppUser, Categoria, Funko, Proveedor)
├── Dtos/                         # Contratos de entrada/salida por área
├── Mappers/                      # Mapeo manual entidad ↔ DTO
├── Validators/                   # (Data Annotations en DTOs)
├── Errors/                       # DomainError y errores por área (records sellados)
├── Extensions/                   # DomainErrorExtensions.ToHttpResult()
├── Middleware/                   # GlobalExceptionHandler
├── Infrastructures/              # DI: DatabaseConfig, IdentityConfig, CacheConfig, StorageConfig, ServicesConfig…
├── Data/                         # AppDbContext, DbContextMongo, Seed (Efc, Identity, Mongo)
├── Storage/                      # IStorageService, FileSystemStorageService
├── Cache/                        # ICacheService, RedisCacheService
├── Factory/                      # ConnectionStringGenerator
├── Helpers/                      # PaginationHelper (RFC 5988 Link headers)
├── Conventers/                   # ObjectIdJsonConverter (MongoDB ↔ JSON)
├── appsettings.json              # Valores por defecto
├── appsettings.Development.json  # Sobrescritura desarrollo
├── appsettings.Production.json   # Sobrescritura producción
├── Dockerfile                    # Multi-stage build .NET 10
└── FunkoApi.Back.csproj
```

---

## 🐳 Despliegue con Docker

El orquestador está en la raíz (`compose.yaml`) y define cuatro servicios:

- `postgres`: PostgreSQL 17 Alpine
- `mongodb`: MongoDB 7.0
- `redis`: Redis 7 Alpine
- `api`: FunkoApi.Back (imagen construida desde `FunkoApi.Back/Dockerfile`)

### Comandos

```bash
# Desde la raíz de la solución
docker compose up -d --build

# Ver estado de los contenedores
docker compose ps

# Ver logs de un servicio
docker compose logs -f api

# Detener y eliminar contenedores (mantiene volúmenes)
docker compose down

# Detener y eliminar también los volúmenes de datos
docker compose down -v
```

### Variables de entorno (`.env`)

Se recomienda crear un archivo `.env` en la raíz con los secretos. Ejemplo mínimo:

```env
POSTGRES_USER=admin
POSTGRES_PASSWORD=admin123
POSTGRES_DB=funkoapi
MONGO_ROOT_USER=admin
MONGO_ROOT_PASSWORD=admin123
MONGO_DB=funkoapi
Jwt__Key=ClaveSecretaMuyLargaYSegura1234567890
```

> Las variables con `__` (doble guion bajo) se usan en lugar de `:` para evitar problemas en YAML y shell (ej. `Jwt__Key`, `Cache__RedisConfiguration`, `PostgresDb__Password`).

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