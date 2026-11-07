using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Hub;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;
using FunkoApi.Back.Services.Funko.Transactions;
using FunkoApi.Back.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace FunkoApi.Back.Services.Funko;

/// <summary>
///     Servicio encargado de gestionar las operaciones CRUD de Funkos.
///     Comprueba las reglas de negocio, utiliza los repositorios y
///     convierte las entidades a DTOs mediante los mapeadores.
/// </summary>
public class FunkoServices(
    IFunkoRepository funkoRepository,
    ICategoriaRepository categoriaRepository,
    IProveedorRepository proveedorRepository,
    ICacheService cache,
    IOptions<CacheOptions> cacheOptions,
    IStorageService storage,
    IOptions<StorageSettings> storageOptions,
    FunkoTransactionsService transactionsService,
    IHubContext<FunkoHub> hubContext,
    ILogger<FunkoServices> logger
    ) : IFunkoService {
    
    private readonly CacheOptions _cacheOptions = cacheOptions.Value;
    private readonly StorageSettings _storageOptions = storageOptions.Value;
    
    private string _funkoKey => "funko:";

    /// <inheritdoc cref="IFunkoService.Create"/>
    public async Task<Result<FunkoDto, DomainError>> Create(CreateFunkoDto item) {
        logger.LogInformation("Iniciando creación de nuevo Funko con nombre '{Nombre}'", item.Nombre);

        if (await funkoRepository.ExitsName(item.Nombre)) {
            logger.LogWarning("Intento de crear Funko fallido: Ya existe un Funko con el nombre '{Nombre}'", item.Nombre);
            return FunkoError.FunkoAlreadyExists(item.Nombre);
        }

        if (!await categoriaRepository.ExistsById(item.Categoria)) {
            logger.LogWarning("Intento de crear Funko fallido: La categoría con ID '{CategoriaId}' no es válida", item.Categoria);
            return CategoriaError.CategoriaBadRequest(item.Categoria);
        }

        if (!await proveedorRepository.ExistsById(item.Proveedor)) {
            logger.LogWarning("Intento de crear Funko fallido: El proveedor con ID '{ProveedorId}' no es válido", item.Proveedor);
            return ProveedorError.ProveedorBadRequest(item.Proveedor);
        }

        var categoria = await categoriaRepository.GetById(item.Categoria);

        if (categoria is null) {
            logger.LogWarning("Intento de crear Funko fallido: Categoría con ID '{CategoriaId}' no encontrada", item.Categoria);
            return CategoriaError.CategoriaNotFound(item.Categoria);
        }

        if (storage.IsEmpty(item.Imagen)) return await CreateNoImage(item, categoria);
        
        return await CreateWithImage(item, categoria);
    }
    
    /// <summary>
    /// Crea un nuevo Funko que no incluye imagen personalizada, asignando la imagen por defecto.
    /// </summary>
    private async Task<Result<FunkoDto, DomainError>> CreateNoImage(CreateFunkoDto item, Models.Categoria categoria) {
        logger.LogDebug("Creando Funko sin imagen personalizada para '{Nombre}'", item.Nombre);

        var model = new Models.Funko(
            0,
            item.Nombre,
            item.Precio,
            item.Stock,
            storage.GetRelativePath(_storageOptions.DefaultImage, _storageOptions.Folders["Funkos"]),
            item.Categoria,
            categoria,
            ObjectId.Parse(item.Proveedor),
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        try {
            var result = await funkoRepository.Create(model);
            logger.LogInformation("Funko '{Nombre}' creado exitosamente sin imagen con ID {Id}", item.Nombre, result.Id);
            
            await NotifyFunkoCreadoAsync(result);
            
            return result.ToDto();
        } catch (Exception ex) {
            logger.LogError(ex, "Error al crear el Funko sin imagen '{Nombre}' en la base de datos", item.Nombre);
            return new ConflictError(ex.Message);
        }
    }
    
    /// <summary>
    /// Crea un nuevo Funko procesando y guardando su archivo de imagen asociado.
    /// </summary>
    private async Task<Result<FunkoDto, DomainError>> CreateWithImage(CreateFunkoDto item, Models.Categoria categoria) {
        logger.LogDebug("Guardando imagen para el nuevo Funko '{Nombre}'", item.Nombre);
        
        var file = await storage.SaveFileAsync(item.Imagen!, _storageOptions.Folders["Funkos"]);
        if (file.IsFailure) {
            logger.LogWarning("Fallo al guardar la imagen para el Funko '{Nombre}'", item.Nombre);
            return file.Error;
        }

        var model = new Models.Funko(
            0,
            item.Nombre,
            item.Precio,
            item.Stock,
            file.Value,
            item.Categoria,
            categoria,
            ObjectId.Parse(item.Proveedor),
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );
        try {
            var result = await funkoRepository.Create(model);
            logger.LogInformation("Funko '{Nombre}' creado exitosamente con imagen con ID {Id}", item.Nombre, result.Id);
            
            await NotifyFunkoCreadoAsync(result);
            
            return result.ToDto();
        } catch (Exception ex) {
            logger.LogError(ex, "Error al crear el Funko con imagen '{Nombre}', ejecutando limpieza de archivo guardado", item.Nombre);
            var resultFile = await storage.DeleteFileAsync(file.Value);
            return new ConflictError(ex.Message);
        }
    }

    /// <inheritdoc cref="IFunkoService.GetById" />
    public async Task<Result<FunkoDto, DomainError>> GetById(long key) {
        logger.LogDebug("Consultando Funko por ID {Key}", key);
        
        var funko = await cache.GetOrSetAsync<Models.Funko>(_funkoKey + key,
            async () => await funkoRepository.GetById(key),
            _cacheOptions.DefaultExpiration);
            
        if (funko == null) {
            logger.LogDebug("Funko con ID {Key} no encontrado", key);
            return FunkoError.FunkoNotFound(key);
        }
        
        return funko.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetByName" />
    public async Task<Result<FunkoDto, DomainError>> GetByName(string name) {
        logger.LogDebug("Consultando Funko por nombre '{Name}'", name);
        
        var funko = await cache.GetOrSetAsync<Models.Funko>(_funkoKey + name,
            async () => await funkoRepository.GetByName(name),
            _cacheOptions.DefaultExpiration);
            
        if (funko == null) {
            logger.LogDebug("Funko con nombre '{Name}' no encontrado", name);
            return FunkoError.FunkoNotFoundByName(name);
        }
        
        return funko.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetPaged" />
    public async Task<(IEnumerable<FunkoDto> Items, int TotalPages, int TotalItems)> GetPaged(int page, int pageSize) {
        logger.LogDebug("Obteniendo lista paginada de Funkos: Página {Page}, Tamaño {PageSize}", page, pageSize);
        
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        var all = await funkoRepository.GetAll();
        var list = all.ToList();
        var totalItems = list.Count;
        
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        var items = list.Skip((page - 1) * pageSize).Take(pageSize);
        
        return (items.Select(p => p.ToDto()), totalPages, totalItems);
    }

    /// <inheritdoc cref="IFunkoService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(long id) {
        logger.LogInformation("Iniciando borrado lógico (SoftDelete) para el Funko con ID {Id}", id);
        
        var funko = await cache.GetAsync<Models.Funko>( _funkoKey + id);
        if (funko is null) {
            funko = await funkoRepository.GetById(id);
            if (funko is null) {
                logger.LogWarning("Funko con ID {Id} no encontrado ni en caché ni en base de datos", id);
                return FunkoError.FunkoNotFound(id);
            }
        }
        var result = await funkoRepository.SoftDelete(id);
    
        if (result) {
            logger.LogInformation("Borrado lógico exitoso para Funko ID {Id}, eliminando de caché", id);
            await cache.RemoveAsync(_funkoKey + id);
        }

        return result;
    }

    /// <inheritdoc cref="IFunkoService.HardDelete" />
    public async Task<Result<bool, DomainError>> HardDelete(long id) {
        logger.LogWarning("Iniciando borrado físico (HardDelete) para el Funko con ID {Id}", id);
        
        var funko = await cache.GetAsync<Models.Funko>( _funkoKey + id);
        if (funko is null) {
            funko = await funkoRepository.GetById(id);
            if (funko is null) {
                logger.LogWarning("Funko con ID {Id} no encontrado ni en caché ni en base de datos", id);
                return FunkoError.FunkoNotFound(id);
            }
        }
        var result = await transactionsService.Delete(funko);
    
        if (result.IsSuccess) {
            logger.LogInformation("Borrado físico exitoso para Funko ID {Id}, eliminando de caché", id);
            await cache.RemoveAsync(_funkoKey + id);
        }

        return result;
    }

    /// <inheritdoc cref="IFunkoService.Update" />
    public async Task<Result<FunkoDto, DomainError>> Update(UpdateFunkoDto item, long key) {
        logger.LogInformation("Iniciando actualización para el Funko con ID {Key}", key);
       
        var oldFunko = await cache.GetAsync<Models.Funko>( _funkoKey + key);
        if (oldFunko is null) {
            oldFunko = await funkoRepository.GetById(key);
            if (oldFunko is null) {
                logger.LogWarning("Funko con ID {Key} no encontrado ni en caché ni en base de datos para actualización", key);
                return FunkoError.FunkoNotFound(key);
            }
        }
        
        if (!await proveedorRepository.ExistsById(item.Proveedor)) {
            logger.LogWarning("Actualización fallida: Proveedor con ID '{ProveedorId}' no válido", item.Proveedor);
            return ProveedorError.ProveedorBadRequest(item.Proveedor);
        }

        if (!await categoriaRepository.ExistsById(item.Categoria)) {
            logger.LogWarning("Actualización fallida: Categoría con ID '{CategoriaId}' no válida", item.Categoria);
            return CategoriaError.CategoriaBadRequest(item.Categoria);
        }
        
        if (item.Nombre != oldFunko.Nombre && await funkoRepository.ExitsName(item.Nombre)) {
            logger.LogWarning("Actualización fallida: Ya existe otro Funko con el nombre '{Nombre}'", item.Nombre);
            return FunkoError.FunkoAlreadyExists(item.Nombre);
        }

        if (storage.IsEmpty(item.Imagen)) {
            logger.LogWarning("Actualización fallida: No se ha enviado ninguna imagen");
            return new BadRequestError("No se ha mandado la imagen");
        }
        
        var file = await storage.SaveFileAsync(item.Imagen!, _storageOptions.Folders["Funkos"]);
        if (file.IsFailure) {
            logger.LogWarning("Actualización fallida al guardar la nueva imagen para Funko ID {Key}", key);
            return file.Error;
        }
        
        var updating = oldFunko with {
            Nombre = item.Nombre,
            Precio = item.Precio,
            Stock = item.Stock,
            Imagen = file.Value,
            CategoriaId = item.Categoria,
            ProveedorId = ObjectId.Parse(item.Proveedor),
            UpdatedAt = DateTime.UtcNow
        };

       var result = await transactionsService.Update(updating, file.Value, oldFunko.Imagen);
       if (result.IsFailure) {
           logger.LogWarning("Fallo en la transacción de actualización para el Funko ID {Key}", key);
           return result.Error;
       }
       
       await cache.RemoveAsync(_funkoKey + key);
       logger.LogInformation("Funko con ID {Key} actualizado exitosamente", key);
       return result.Value.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetAll" />
    public async Task<IEnumerable<FunkoDto>> GetAll() {
        logger.LogDebug("Obteniendo todos los Funkos registrados");
        var lista = await funkoRepository.GetAll();
        return lista.Select(f => f.ToDto());
    }
    
    /// <summary>
    /// Comunica la creación de un funko a los administradores conectados mediante el evento FunkoCreado.
    /// </summary>
    /// <remarks>
    /// Se invoca después de guardar la entidad. Si falla el aviso, registra el error
    /// sin convertir en fallida la creación que ya se ha realizado.
    /// </remarks>
    /// <param name="funko">Entidad guardada cuyos datos se incluirán en el aviso.</param>
    /// <returns>Una tarea que representa el intento de enviar la notificación.</returns>
    private async Task NotifyFunkoCreadoAsync(Models.Funko funko) {
        try {
            var notificacion = new {
                FunkoId = funko.Id,
                Nombre = funko.Nombre,
                Precio = funko.Precio,
                Mensaje = $"Nuevo funko creado: {funko.Nombre}"
            };

            await hubContext.Clients
                .Group("admins")
                .SendAsync("FunkoCreado", notificacion);
            
        } catch (Exception ex) {
            logger.LogWarning(ex, "Error al enviar notificación de funko creado");
        }
    }
}