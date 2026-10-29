using CSharpFunctionalExtensions;
using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Services.Get.Categoria;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Back.Controllers;

/// <summary>
///     Controlador REST encargado de gestionar las peticiones HTTP
///     relacionadas con la consulta de categorías.
/// </summary>
/// <param name="service">Servicio encargado de realizar las operaciones de consulta de categorías.</param>
[ApiController]
[Route("api/[controller]")]
public class CategoriasController(ICategoriaService service) : ControllerBase
{
    /// <summary>
    ///     Obtiene todas las categorías disponibles.
    /// </summary>
    /// <returns>Respuesta HTTP con la colección de categorías.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaDto>>> GetAll()
    {
        return Ok(await service.GetAll());
    }

    /// <summary>
    ///     Obtiene una categoría a partir de su identificador.
    /// </summary>
    /// <param name="id">Identificador de la categoría que se desea consultar.</param>
    /// <returns>Respuesta HTTP con la categoría encontrada o el error correspondiente.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaDto>> GetById(int id)
    {
        return await service.GetById(id).Match(
            response => Ok(response),
            error => error.ToHttpResult<CategoriaDto>()
        );
    }

    /// <summary>
    ///     Obtiene una categoría a partir de su nombre.
    /// </summary>
    /// <param name="name">Nombre de la categoría que se desea consultar.</param>
    /// <returns>Respuesta HTTP con la categoría encontrada o el error correspondiente.</returns>
    [HttpGet("{name}")]
    public async Task<ActionResult<CategoriaDto>> GetByName(string name)
    {
        return await service.GetByName(name).Match(
            response => Ok(response),
            error => error.ToHttpResult<CategoriaDto>()
        );
    }
}