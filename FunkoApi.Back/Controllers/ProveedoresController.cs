using CSharpFunctionalExtensions;
using FunkoApi.Back.Dto;
using FunkoApi.Back.Helpers;
using FunkoApi.Back.Dto.Proveedor;
using FunkoApi.Back.Dto.Documentation;
using Swashbuckle.AspNetCore.Annotations;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Services.Proveedor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Back.Controllers;

/// <summary>
/// Controlador REST encargado de gestionar las peticiones HTTP
/// relacionadas con las operaciones CRUD de los Proveedores.
/// </summary>
/// <param name="service">Servicio encargado de gestionar las operaciones de los Proveedores.</param>
[ApiController]
[Route("api/[controller]")]
public class ProveedoresController(IProveedorService service) : ControllerBase {
    /// <summary>
    /// Crea un nuevo Proveedor a partir de los datos proporcionados.
    /// </summary>
    /// <param name="dto">DTO con los datos necesarios para crear el Proveedor.</param>
    /// <returns>Respuesta HTTP con el Proveedor creado o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    [SwaggerOperation(OperationId = "Proveedores_Create")]
    [SwaggerResponse(201, "Proveedor creado con ObjectId textual y cabecera Location. No se comprueba nombre duplicado.", typeof(ProveedorDto))]
    [SwaggerResponse(400, "Nombre obligatorio, no vacío y de hasta 100 caracteres; JSON válido.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<ProveedorDto>> Create([FromBody] CreateProveedorDto dto) {
        var result = await service.Create(dto);

        return result.Match(
            response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            error => error.ToHttpResult<ProveedorDto>()
        );
    }

    /// <summary>
    /// Elimina lógicamente un Proveedor mediante su identificador.
    /// </summary>
    /// <param name="id">ObjectId del proveedor: cadena de 24 dígitos hexadecimales.</param>
    /// <remarks>Un ObjectId mal formado puede lanzar FormatException y producir 500 en lugar de 400.</remarks>
    /// <returns>Respuesta HTTP con el resultado de la eliminación o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id}")]
    [SwaggerOperation(OperationId = "Proveedores_Delete")]
    [SwaggerResponse(200, "Resultado booleano del borrado lógico. Puede ser false si no se modifica ningún documento.", typeof(bool))]
    [SwaggerResponse(404, "Proveedor inexistente.", typeof(MessageErrorResponse))]
    [SwaggerResponse(409, "El proveedor tiene Funkos asociados y no se puede borrar.", typeof(MessageErrorResponse))]
    public async Task<ActionResult> Delete(string id) {
        return await service.SoftDelete(id).Match(
            response => Ok(response),
            error => error.ToHttpResult()
        );
    }

    /// <summary>
    /// Actualiza los datos de un Proveedor existente.
    /// </summary>
    /// <param name="dto">DTO con los nuevos datos del Proveedor.</param>
    /// <param name="id">ObjectId del proveedor: cadena de 24 dígitos hexadecimales.</param>
    /// <remarks>Un ObjectId mal formado puede producir 500. No se comprueba nombre duplicado.</remarks>
    /// <returns>Respuesta HTTP con el Proveedor actualizado o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}")]
    [SwaggerOperation(OperationId = "Proveedores_Update")]
    [SwaggerResponse(200, "Proveedor actualizado.", typeof(ProveedorDto))]
    [SwaggerResponse(400, "Nombre obligatorio y de hasta 100 caracteres; JSON válido.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(404, "Proveedor inexistente.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<ProveedorDto>> Update([FromBody] UpdateProveedorDto dto, string id) {
        return await service.Update(dto, id).Match(
            response => Ok(response),
            error => error.ToHttpResult<ProveedorDto>()
        );
    }

    /// <summary>
    /// Obtiene todos los Proveedores disponibles.
    /// </summary>
    /// <returns>Respuesta HTTP con la colección de Proveedores.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpGet]
    [SwaggerOperation(OperationId = "Proveedores_GetAll")]
    [SwaggerResponse(200, "Lista sin paginar, ordenada por nombre; incluye los eliminados lógicamente.", typeof(IEnumerable<ProveedorDto>))]
    public async Task<ActionResult<IEnumerable<ProveedorDto>>> GetAll() {
        return Ok(await service.GetAll());
    }

    /// <summary>Obtiene una página de proveedores consultada en MongoDB.</summary>
    /// <param name="page">Número de página, por defecto 1.</param>
    /// <param name="pageSize">Tamaño de página, por defecto 10 y ajustado entre 1 y 100.</param>
    /// <returns>Datos, metadatos y cabecera Link cuando procede.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("paged")]
    [SwaggerOperation(OperationId = "Proveedores_GetPaged")]
    [SwaggerResponse(200, "Objeto data/pagination. Incluye eliminados lógicamente.", typeof(PagedResponse<ProveedorDto>))]
    [SwaggerResponse(400, "page o pageSize no son enteros válidos.", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<PagedResponse<ProveedorDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10) {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalPages, totalItems) = await service.GetPaged(page, pageSize);
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var linkHeader = PaginationHelper.CreateLinkHeader(page, totalPages, pageSize, baseUrl);
        if (!string.IsNullOrEmpty(linkHeader)) Response.Headers["Link"] = linkHeader;

        return Ok(new PagedResponse<ProveedorDto> {
            Data = items.ToList(),
            Pagination = new PageMetadata(page, pageSize, totalPages, totalItems)
        });
    }

    /// <summary>
    /// Obtiene un Proveedor mediante su identificador.
    /// </summary>
    /// <param name="id">ObjectId del proveedor: cadena de 24 dígitos hexadecimales.</param>
    /// <remarks>Un ObjectId mal formado puede producir 500. La consulta no filtra IsDeleted.</remarks>
    /// <returns>Respuesta HTTP con el Proveedor encontrado o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("{id}")]
    [SwaggerOperation(OperationId = "Proveedores_GetById")]
    [SwaggerResponse(200, "Proveedor encontrado, con estado y fechas.", typeof(ProveedorDto))]
    [SwaggerResponse(404, "Proveedor inexistente.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<ProveedorDto>> GetById(string id) {
        return await service.GetById(id).Match(
            response => Ok(response),
            error => error.ToHttpResult<ProveedorDto>()
        );
    }
}
