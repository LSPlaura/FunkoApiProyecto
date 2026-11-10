using CSharpFunctionalExtensions;
using FunkoApi.Back.Dto;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Dto.Documentation;
using Swashbuckle.AspNetCore.Annotations;
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
    /// <remarks>
    /// Enviar multipart/form-data con nombre, precio, stock, categoriaId, proveedorId y archivo opcional.
    /// proveedorId debe ser un ObjectId hexadecimal de 24 caracteres; categoriaId es el ID textual de categoría.
    /// Sin imagen se usa la predeterminada. Las Data Annotations de CreateFunkoDto no se ejecutan automáticamente,
    /// porque el DTO se construye dentro de la acción; no se garantizan sus rangos en este endpoint.
    /// Tras guardar se avisa por SignalR y se encola un correo al destinatario configurado en el servicio,
    /// no al usuario autenticado. El 201 no garantiza la entrega del correo.
    /// </remarks>
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [SwaggerOperation(OperationId = "Funkos_Create")]
    [SwaggerResponse(201, "Funko creado con categoría anidada, fechas, imagen y cabecera Location. No incluye proveedor en la salida.", typeof(FunkoDto))]
    [SwaggerResponse(400, "Formulario inválido o categoría/proveedor inexistentes.", typeof(MessageErrorResponse))]
    [SwaggerResponse(409, "Nombre duplicado o excepción capturada al guardar el Funko.", typeof(MessageErrorResponse))]
    [SwaggerResponse(422, "La imagen supera el tamaño configurado o incumple extensión, MIME o nombre.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<FunkoDto>> Create(
        [FromForm] string nombre, 
        [FromForm] decimal precio, 
        [FromForm] int stock, 
        [FromForm] string categoriaId, 
        [FromForm] string proveedorId, 
        IFormFile? archivo)
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
    [SwaggerOperation(OperationId = "Funkos_Delete")]
    [SwaggerResponse(200, "Resultado booleano del borrado lógico; se encola un email si es true.", typeof(bool))]
    [SwaggerResponse(400, "El identificador supera el rango Int32 que recibe actualmente la acción.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(404, "Funko inexistente o ya eliminado, salvo que exista una entrada de caché previa.", typeof(MessageErrorResponse))]
    public async Task<ActionResult> Delete(int id) {
        return await service.SoftDelete(id).Match(
            response => Ok(response),
            error => error.ToHttpResult()
        );
    }

    /// <summary>
    /// Actualiza la información de un Funko existente (PUT); el servicio exige una imagen nueva.
    /// </summary>
    /// <param name="nombre">Nombre actualizado del Funko.</param>
    /// <param name="precio">Precio actualizado del Funko.</param>
    /// <param name="stock">Stock actualizado.</param>
    /// <param name="categoriaId">Identificador de la nueva categoría.</param>
    /// <param name="proveedorId">Identificador del nuevo proveedor.</param>
    /// <param name="archivo">Nueva imagen. Aunque el parámetro es nullable, el servicio devuelve 400 si falta o está vacía.</param>
    /// <param name="id">Identificador del Funko que se desea actualizar.</param>
    /// <returns>Respuesta HTTP 200 (OK) con el Funko actualizado, o un error correspondiente.</returns>
    /// <remarks>
    /// Formulario multipart/form-data. Se recibe proveedorId como ObjectId de 24 caracteres.
    /// El DTO se crea dentro de la acción: sus rangos de precio, stock y longitud no se validan automáticamente.
    /// El id de esta acción es Int32 aunque la ruta tiene restricción long. No se encola email al actualizar.
    /// </remarks>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id:long}")]
    [SwaggerOperation(OperationId = "Funkos_Update")]
    [SwaggerResponse(200, "Funko actualizado con categoría anidada.", typeof(FunkoDto))]
    [SwaggerResponse(400, "Binding inválido, categoría/proveedor inexistentes o imagen ausente/vacía.", typeof(MessageErrorResponse))]
    [SwaggerResponse(404, "Funko inexistente.", typeof(MessageErrorResponse))]
    [SwaggerResponse(409, "Nombre duplicado.", typeof(MessageErrorResponse))]
    [SwaggerResponse(422, "Imagen inválida o error transaccional de actualización.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<FunkoDto>> Update(
        [FromForm] string nombre, 
        [FromForm] decimal precio, 
        [FromForm] int stock, 
        [FromForm] string categoriaId, 
        [FromForm] string proveedorId, 
        IFormFile? archivo, 
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
    [SwaggerOperation(OperationId = "Funkos_GetAll")]
    [SwaggerResponse(200, "Lista sin paginar, ordenada por nombre. Actualmente incluye eliminados lógicamente.", typeof(IEnumerable<FunkoDto>))]
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
    [SwaggerOperation(OperationId = "Funkos_GetById")]
    [SwaggerResponse(200, "Funko encontrado, con categoría anidada; el repositorio excluye eliminados.", typeof(FunkoDto))]
    [SwaggerResponse(400, "Número fuera del rango Int32 admitido por la acción.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(404, "Funko inexistente o eliminado; la caché puede afectar al resultado.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<FunkoDto>> GetById(int id) {
        return await service.GetById(id).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    /// Busca y recupera un Funko a partir de su nombre exacto.
    /// </summary>
    /// <param name="nombre">Nombre del Funko que se desea consultar.</param>
    /// <returns>Respuesta HTTP 200 (OK) con el Funko encontrado, o un error si no existe.</returns>
    /// <remarks>
    /// Un segmento numérico se interpreta como id, no como nombre. El segmento reservado paged se interpreta como paginación.
    /// Las rutas GET por id y nombre solo se distinguen mediante restricciones del router; consultar la auditoría OpenAPI.
    /// </remarks>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("{nombre}")]
    [SwaggerOperation(OperationId = "Funkos_GetByNombre")]
    [SwaggerResponse(200, "Funko cuyo nombre coincide exactamente.", typeof(FunkoDto))]
    [SwaggerResponse(404, "No existe un Funko activo con ese nombre.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<FunkoDto>> GetByNombre(string nombre) {
        return await service.GetByName(nombre).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }
    
    /// <summary>
    /// Intenta actualizar la imagen asociada a un Funko; véanse las limitaciones del flujo en las observaciones.
    /// </summary>
    /// <param name="id">Identificador único del Funko cuya imagen se va a modificar.</param>
    /// <param name="archivo">Nuevo archivo de imagen recibido mediante el formulario.</param>
    /// <returns>Respuesta HTTP 200 (OK) con el Funko actualizado, o un error correspondiente.</returns>
    /// <remarks>
    /// Recibe multipart/form-data con archivo obligatorio. ADVERTENCIA DE AUDITORÍA:
    /// borra la imagen anterior antes de completar la operación y guarda la nueva dos veces.
    /// El DTO enviado al servicio no contiene Proveedor: con el repositorio MongoDB actual puede fallar con 500
    /// al convertir el identificador vacío; con otro repositorio puede devolver 400 por proveedor inexistente.
    /// No se garantiza una actualización correcta; no se modifica este comportamiento desde la documentación.
    /// </remarks>
    [Authorize(Roles = "ADMIN")]
    [HttpPatch("{id:long}/imagen")]
    [SwaggerOperation(OperationId = "Funkos_UpdateImagen")]
    [SwaggerResponse(200, "Funko actualizado únicamente si el flujo completo llega a completarse; véase la advertencia.", typeof(FunkoDto))]
    [SwaggerResponse(400, "Archivo ausente, binding inválido o proveedor/categoría rechazados por el servicio.", typeof(MessageErrorResponse))]
    [SwaggerResponse(404, "Funko inexistente.", typeof(MessageErrorResponse))]
    [SwaggerResponse(409, "Conflicto en la actualización.", typeof(MessageErrorResponse))]
    [SwaggerResponse(422, "Archivo inválido o error transaccional.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<FunkoDto>> UpdateImagen(long id, IFormFile archivo) {
        var existente = await service.GetById(id);
        if (existente.IsFailure) return existente.Error.ToHttpResult<FunkoDto>();
        if (!string.IsNullOrEmpty(existente.Value.Imagen)) await storage.DeleteFileAsync(existente.Value.Imagen);

        // Guardar nueva imagen
        var saveResult = await storage.SaveFileAsync(archivo, "funkos");
        if (saveResult.IsFailure) return saveResult.Error.ToHttpResult<FunkoDto>();

        // Actualizar solo la imagen
        var dto = new UpdateFunkoDto { Nombre = existente.Value.Nombre, Precio = existente.Value.Precio, Stock = existente.Value.Stock, Categoria = existente.Value.Categoria.Id, Imagen = archivo };
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
    [Authorize(Roles = "ADMIN,USER")]
    [HttpGet("paged")]
    [SwaggerOperation(OperationId = "Funkos_GetPaged")]
    [SwaggerResponse(200, "Objeto data/pagination, con cabecera Link cuando procede. Incluye eliminados lógicamente.", typeof(PagedResponse<FunkoDto>))]
    [SwaggerResponse(400, "page o pageSize no son enteros válidos. Los valores numéricos fuera de límites se ajustan, no se rechazan.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<PagedResponse<FunkoDto>>> GetPaged(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        var (items, totalPages, totalItems) = await service.GetPaged(page, pageSize);
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var linkHeader = PaginationHelper.CreateLinkHeader(page, totalPages, pageSize, baseUrl);
        if (!string.IsNullOrEmpty(linkHeader)) Response.Headers["Link"] = linkHeader;
        
        return Ok(new PagedResponse<FunkoDto> {
            Data = items.ToList(),
            Pagination = new PageMetadata(page, pageSize, totalPages, totalItems)
        });
    }
}
