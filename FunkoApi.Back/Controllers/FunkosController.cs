using CSharpFunctionalExtensions;
using FunkoApi.Back.Dto;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Helpers;
using FunkoApi.Back.Services.Funko;
using FunkoApi.Back.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace FunkoApi.Back.Controllers;

/// <summary>
/// Controlador REST encargado de gestionar las peticiones HTTP
/// relacionadas con el CRUD completo y la gestión multimedia de los Funkos.
/// </summary>
/// <param name="service">Servicio de lógica de negocio para los Funkos.</param>
/// <param name="storage">Servicio de gestión de almacenamiento físico de archivos.</param>
[ApiController]
[Route("api/[controller]")]
public class FunkosController(IFunkoService service, IStorageService storage) : ControllerBase {
    
    /// <summary>
    /// Crea un nuevo Funko en el sistema, permitiendo adjuntar una imagen opcionalmente.
    /// </summary>
    /// <param name="nombre">Nombre único del Funko.</param>
    /// <param name="precio">Precio de venta del artículo.</param>
    /// <param name="stock">Cantidad disponible en inventario.</param>
    /// <param name="categoriaId">Identificador de la categoría asociada.</param>
    /// <param name="proveedorId">Identificador del proveedor asociado.</param>
    /// <param name="archivo">Archivo de imagen opcional para el Funko.</param>
    /// <returns>Respuesta HTTP 201 (Created) con el DTO del Funko recién creado, o un error detallado si falla.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<FunkoDto>> Create(
        [FromForm] string nombre, 
        [FromForm] decimal precio, 
        [FromForm] int stock, 
        [FromForm] string categoriaId, 
        [FromForm] string proveedorId, 
        [FromForm] IFormFile? archivo)
    {
        var dto = new CreateFunkoDto { Nombre = nombre, Precio = precio, Stock =  stock, Categoria = categoriaId, Proveedor = proveedorId, Imagen = archivo };
        var result = await service.Create(dto);
        return result.Match(
            response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    /// Realiza un borrado lógico de un Funko específico mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador numérico del Funko a eliminar.</param>
    /// <returns>Respuesta HTTP 200 (OK) con el resultado booleano de la operación, o un error si no se encuentra.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(int id) {
        return await service.SoftDelete(id).Match(
            response => Ok(response),
            error => error.ToHttpResult()
        );
    }

    /// <summary>
    /// Actualiza por completo la información de un Funko existente (PUT).
    /// </summary>
    /// <param name="nombre">Nombre actualizado del Funko.</param>
    /// <param name="precio">Precio actualizado del Funko.</param>
    /// <param name="stock">Stock actualizado.</param>
    /// <param name="categoriaId">Identificador de la nueva categoría.</param>
    /// <param name="proveedorId">Identificador del nuevo proveedor.</param>
    /// <param name="archivo">Nueva imagen opcional para reemplazar la actual.</param>
    /// <param name="id">Identificador del Funko que se desea actualizar.</param>
    /// <returns>Respuesta HTTP 200 (OK) con el Funko actualizado, o un error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<FunkoDto>> Update(
        [FromForm] string nombre, 
        [FromForm] decimal precio, 
        [FromForm] int stock, 
        [FromForm] string categoriaId, 
        [FromForm] string proveedorId, 
        [FromForm] IFormFile? archivo, 
        int id)
    {
        var dto = new UpdateFunkoDto() { Nombre = nombre, Precio = precio, Stock =  stock, Categoria = categoriaId, Proveedor = proveedorId, Imagen = archivo };
        return await service.Update(dto, id).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    /// Obtiene el listado completo de todos los Funkos disponibles en el sistema.
    /// </summary>
    /// <returns>Respuesta HTTP 200 (OK) con la colección de Funkos.</returns>
    [Authorize(Roles = "ADMIN,USER")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FunkoDto>>> GetAll() {
        return Ok(await service.GetAll());
    }

    /// <summary>
    /// Recupera los detalles de un Funko específico mediante su identificador único.
    /// </summary>
    /// <param name="id">Identificador numérico del Funko.</param>
    /// <returns>Respuesta HTTP 200 (OK) con el Funko encontrado, o un error 404 si no existe.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<FunkoDto>> GetById(int id) {
        return await service.GetById(id).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    /// Busca y recupera un Funko a partir de su nombre exacto.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea consultar.</param>
    /// <returns>Respuesta HTTP 200 (OK) con el Funko encontrado, o un error si no existe.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("{nombre}")]
    public async Task<ActionResult<FunkoDto>> GetByNombre(string name) {
        return await service.GetByName(name).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }
    
    /// <summary>
    /// Actualiza únicamente la imagen asociada a un Funko, eliminando la anterior del almacenamiento de forma segura.
    /// </summary>
    /// <param name="id">Identificador único del Funko cuya imagen se va a modificar.</param>
    /// <param name="archivo">Nuevo archivo de imagen recibido mediante el formulario.</param>
    /// <returns>Respuesta HTTP 200 (OK) con el Funko actualizado, o un error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPatch("{id:long}/imagen")]
    public async Task<ActionResult<FunkoDto>> UpdateImagen(long id, IFormFile archivo) {
        var existente = await service.GetById(id);
        if (existente.IsFailure) return existente.Error.ToHttpResult<FunkoDto>();
        if (!string.IsNullOrEmpty(existente.Value.Imagen)) await storage.DeleteFileAsync(existente.Value.Imagen);

        // Guardar nueva imagen
        var saveResult = await storage.SaveFileAsync(archivo, "funkos");
        if (saveResult.IsFailure) return saveResult.Error.ToHttpResult<FunkoDto>();

        // Actualizar solo la imagen
        var dto = new UpdateFunkoDto { Nombre = existente.Value.Nombre, Precio = existente.Value.Precio, Stock = existente.Value.Stock, Categoria = existente.Value.Categoria.Id, Proveedor = existente.Value.Proveedor.Id, Imagen = archivo };
        var updateResult = await service.Update(dto, id);
        return updateResult.IsSuccess ? Ok(updateResult.Value) : updateResult.Error.ToHttpResult<FunkoDto>();
    }
    
    /// <summary>
    /// Obtiene una lista paginada de objetos <see cref="FunkoDto"/> y configura automáticamente 
    /// las cabeceras HTTP de navegación estándar (RFC 5988 Link headers) para facilitar la paginación en el cliente.
    /// </summary>
    /// <param name="page">Número de la página actual que se desea consultar (por defecto: 1).</param>
    /// <param name="pageSize">Cantidad máxima de elementos por página solicitados (por defecto: 10, limitado de forma segura entre 1 y 100).</param>
    /// <returns>
    /// Una tarea que representa la operación asíncrona, devolviendo un <see cref="ActionResult{T}"/> 
    /// que contiene la respuesta estructurada <see cref="PagedResponse{FunkoDto}"/> con los elementos y metadatos.
    /// </returns>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<FunkoDto>>> GetPaged(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        var (items, totalPages, totalItems) = await service.GetPaged(page, pageSize);
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var linkHeader = PaginationHelper.CreateLinkHeader(page, totalPages, pageSize, baseUrl);
        if (!string.IsNullOrEmpty(linkHeader))
        {
            Response.Headers["Link"] = linkHeader;
        }
        
        return Ok(new PagedResponse<FunkoDto>
        {
            Data = items.ToList(),
            Pagination = new PageMetadata(page, pageSize, totalPages, totalItems)
        });
    }
}