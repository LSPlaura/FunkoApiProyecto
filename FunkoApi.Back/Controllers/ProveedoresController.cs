using CSharpFunctionalExtensions;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Services.Proveedor;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Back.Controllers;

/// <summary>
/// Controlador REST encargado de gestionar las peticiones HTTP
/// relacionadas con las operaciones CRUD de los Proveedores.
/// </summary>
/// <param name="service">Servicio encargado de gestionar las operaciones de los Proveedores.</param>
[ApiController]
[Route("api/[controller]")]
public class ProveedoresController(IProveedorService service) : ControllerBase
{
    /// <summary>
    ///     Crea un nuevo Proveedor a partir de los datos proporcionados.
    /// </summary>
    /// <param name="dto">DTO con los datos necesarios para crear el Proveedor.</param>
    /// <returns>Respuesta HTTP con el Proveedor creado o el error correspondiente.</returns>
    [HttpPost]
    public async Task<ActionResult<ProveedorDto>> Create([FromBody] CreateProveedorDto dto)
    {
        var result = await service.Create(dto);

        return result.Match(
            response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            error => error.ToHttpResult<ProveedorDto>()
        );
    }

    /// <summary>
    ///     Elimina lógicamente un Proveedor mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador del Proveedor que se desea eliminar.</param>
    /// <returns>Respuesta HTTP con el resultado de la eliminación o el error correspondiente.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        return await service.SoftDelete(id).Match(
            response => Ok(response),
            error => error.ToHttpResult()
        );
    }

    /// <summary>
    ///     Actualiza los datos de un Proveedor existente.
    /// </summary>
    /// <param name="dto">DTO con los nuevos datos del Proveedor.</param>
    /// <param name="id">Identificador del Proveedor que se desea actualizar.</param>
    /// <returns>Respuesta HTTP con el Proveedor actualizado o el error correspondiente.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ProveedorDto>> Update([FromBody] UpdateProveedorDto dto, string id)
    {
        return await service.Update(dto, id).Match(
            response => Ok(response),
            error => error.ToHttpResult<ProveedorDto>()
        );
    }

    /// <summary>
    ///     Obtiene todos los Proveedores disponibles.
    /// </summary>
    /// <returns>Respuesta HTTP con la colección de Proveedores.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProveedorDto>>> GetAll()
    {
        return Ok(await service.GetAll());
    }

    /// <summary>
    ///     Obtiene un Proveedor mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador del Proveedor que se desea consultar.</param>
    /// <returns>Respuesta HTTP con el Proveedor encontrado o el error correspondiente.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProveedorDto>> GetById(string id)
    {
        return await service.GetById(id).Match(
            response => Ok(response),
            error => error.ToHttpResult<ProveedorDto>()
        );
    }
}