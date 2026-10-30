using CSharpFunctionalExtensions;
using FunkoApi.Back.Dtos.Funko;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Services.Funko;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Back.Controllers;

/// <summary>
///     Controlador REST encargado de gestionar las peticiones HTTP
///     relacionadas con las operaciones CRUD de los Funkos.
/// </summary>
/// <param name="service">Servicio encargado de gestionar las operaciones de los Funkos.</param>
[ApiController]
[Route("api/[controller]")]
public class FunkosController(IFunkoService service) : ControllerBase
{
    /// <summary>
    ///     Crea un nuevo Funko a partir de los datos proporcionados.
    /// </summary>
    /// <param name="dto">DTO con los datos necesarios para crear el Funko.</param>
    /// <returns>Respuesta HTTP con el Funko creado o el error correspondiente.</returns>
    [HttpPost]
    public async Task<ActionResult<FunkoDto>> Create([FromBody] CreateFunkoDto dto)
    {
        var result = await service.Create(dto);

        return result.Match(
            response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    ///     Elimina lógicamente un Funko mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador del Funko que se desea eliminar.</param>
    /// <returns>Respuesta HTTP con el resultado de la eliminación o el error correspondiente.</returns>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        return await service.SoftDelete(id).Match(
            response => Ok(response),
            error => error.ToHttpResult()
        );
    }

    /// <summary>
    ///     Actualiza los datos de un Funko existente.
    /// </summary>
    /// <param name="dto">DTO con los nuevos datos del Funko.</param>
    /// <param name="id">Identificador del Funko que se desea actualizar.</param>
    /// <returns>Respuesta HTTP con el Funko actualizado o el error correspondiente.</returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FunkoDto>> Update([FromBody] UpdateFunkoDto dto, int id)
    {
        return await service.Update(dto, id).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    ///     Obtiene todos los Funkos disponibles.
    /// </summary>
    /// <returns>Respuesta HTTP con la colección de Funkos.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FunkoDto>>> GetAll()
    {
        return Ok(await service.GetAll());
    }

    /// <summary>
    ///     Obtiene un Funko mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador del Funko que se desea consultar.</param>
    /// <returns>Respuesta HTTP con el Funko encontrado o el error correspondiente.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FunkoDto>> GetById(int id)
    {
        return await service.GetById(id).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }

    /// <summary>
    ///     Obtiene un Funko a partir de su nombre.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea consultar.</param>
    /// <returns>Respuesta HTTP con el Funko encontrado o el error correspondiente.</returns>
    [HttpGet("{nombre}")]
    public async Task<ActionResult<FunkoDto>> GetByNombre(string name)
    {
        return await service.GetByName(name).Match(
            response => Ok(response),
            error => error.ToHttpResult<FunkoDto>()
        );
    }
}