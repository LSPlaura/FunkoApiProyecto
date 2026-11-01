using CSharpFunctionalExtensions;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Services.Funko;
using FunkoApi.Back.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace FunkoApi.Back.Controllers;

/// <summary>
/// Controlador REST encargado de gestionar las peticiones HTTP
/// relacionadas con las operaciones CRUD de los Funkos.
/// </summary>
/// <param name="service">Servicio encargado de gestionar las operaciones de los Funkos.</param>
[ApiController]
[Route("api/[controller]")]
public class FunkosController(IFunkoService service, IStorageService storage) : ControllerBase {
    /// <summary>
    ///     Crea un nuevo Funko a partir de los datos proporcionados.
    /// </summary>
    /// <param name="dto">DTO con los datos necesarios para crear el Funko.</param>
    /// <returns>Respuesta HTTP con el Funko creado o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<FunkoDto>> Create([FromBody] CreateFunkoDto dto) {
        var result = await service.Create(dto);

        return result.Match(
            response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    /// Elimina lógicamente un Funko mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador del Funko que se desea eliminar.</param>
    /// <returns>Respuesta HTTP con el resultado de la eliminación o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id) {
        return await service.SoftDelete(id).Match(
            response => Ok(response),
            error => error.ToHttpResult()
        );
    }

    /// <summary>
    /// Actualiza los datos de un Funko existente.
    /// </summary>
    /// <param name="dto">DTO con los nuevos datos del Funko.</param>
    /// <param name="id">Identificador del Funko que se desea actualizar.</param>
    /// <returns>Respuesta HTTP con el Funko actualizado o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FunkoDto>> Update([FromBody] UpdateFunkoDto dto, int id) {
        return await service.Update(dto, id).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    /// Obtiene todos los Funkos disponibles.
    /// </summary>
    /// <returns>Respuesta HTTP con la colección de Funkos.</returns>
    [Authorize(Roles = "ADMIN,USER")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FunkoDto>>> GetAll() {
        return Ok(await service.GetAll());
    }

    /// <summary>
    /// Obtiene un Funko mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador del Funko que se desea consultar.</param>
    /// <returns>Respuesta HTTP con el Funko encontrado o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FunkoDto>> GetById(int id) {
        return await service.GetById(id).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    /// Obtiene un Funko a partir de su nombre.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea consultar.</param>
    /// <returns>Respuesta HTTP con el Funko encontrado o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("{nombre}")]
    public async Task<ActionResult<FunkoDto>> GetByNombre(string name) {
        return await service.GetByName(name).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }
    
    
    /// <summary>
    /// Guarda una imagen y solicita la creación de un Funko con los datos de un formulario multipart/form-data.
    /// </summary>
    /// <param name="nombre">Nombre del Funko.</param>
    /// <param name="precio">Precio del Funko.</param>
    /// <param name="stock">Unidades disponibles.</param>
    /// <param name="categoriaId">Identificador de la categoría asociada.</param>
    /// <param name="proveedorId">Identificador del proveedor asociado.</param>
    /// <param name="archivo">Imagen que se guarda en la carpeta de Funkos.</param>
    /// <returns>Respuesta 201 con el Funko creado y su ubicación, o el error de almacenamiento o creación.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPost("imagen")]
    public async Task<ActionResult<FunkoDto>> CreateConImagen(
        [FromForm] string nombre, 
        [FromForm] decimal precio, 
        [FromForm] int stock, 
        [FromForm] string categoriaId, 
        [FromForm] ObjectId proveedorId, 
        [FromForm] IFormFile archivo) {
        
        var saveResult = await storage.SaveFileAsync(archivo, "funkos");
        if (saveResult.IsFailure) return saveResult.Error.ToHttpResult<FunkoDto>();

        var dto = new CreateFunkoDto { Nombre = nombre, Precio = precio, Stock =  stock, Categoria = categoriaId, Proveedor = proveedorId.ToString(), Imagen = saveResult.Value };
        var result = await service.Create(dto);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : result.Error.ToHttpResult<FunkoDto>();
    }
    
    
    /// <summary>
    /// Solicita la actualización de la imagen de un Funko, conservando sus demás datos.
    /// Elimina la imagen anterior, si tiene una referencia, antes de guardar la nueva.
    /// </summary>
    /// <param name="id">Identificador del Funko cuya imagen se desea actualizar.</param>
    /// <param name="archivo">Nueva imagen recibida mediante un formulario multipart/form-data.</param>
    /// <returns>Respuesta 200 con el Funko actualizado, o el error de consulta, guardado o actualización.</returns>
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
        var dto = new UpdateFunkoDto { Nombre = existente.Value.Nombre, Precio = existente.Value.Precio, Stock = existente.Value.Stock, Categoria = existente.Value.Categoria.Id, Proveedor = existente.Value.Proveedor.Id, Imagen = saveResult.Value };
        var updateResult = await service.Update(dto, id);
        return updateResult.IsSuccess ? Ok(updateResult.Value) : updateResult.Error.ToHttpResult<FunkoDto>();
    }
}
