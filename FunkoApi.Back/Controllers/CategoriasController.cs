using CSharpFunctionalExtensions;
using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Services.Get.Categoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Back.Controllers;

/// <summary>
/// Controlador REST encargado de gestionar las peticiones HTTP
/// relacionadas con la consulta de categorías.
/// </summary>
/// <param name="service">Servicio encargado de realizar las operaciones de consulta de categorías.</param>
[ApiController]
[Route("api/[controller]")]
public class CategoriasController(ICategoriaService service) : ControllerBase {
    /// <summary>
    /// Obtiene todas las categorías disponibles.
    /// </summary>
    /// <returns>Respuesta HTTP con la colección de categorías.</returns>
    [Authorize(Roles = "ADMIN,USER")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaDto>>> GetAll() {
        return Ok(await service.GetAll());
    }

    /// <summary>
    /// Obtiene una categoría a partir de su identificador.
    /// </summary>
    /// <param name="id">Identificador de la categoría que se desea consultar.</param>
    /// <returns>Respuesta HTTP con la categoría encontrada o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN,USER")]
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoriaDto>> GetById(string id) {
        return await service.GetById(id).Match(
            response => Ok(response),
            error => error.ToHttpResult<CategoriaDto>()
        );
    }

    /// <summary>
    /// Crea una nueva categoria a partir de los datos proporcionados.
    /// </summary>
    /// <param name="categoriaDto">DTO con los datos necesarios para crear la categoria.</param>
    /// <returns>Respuesta HTTP con la categoria creado o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<CategoriaDto>> Post(CreateCategoriaDto categoriaDto) {
        var res = await service.Create(categoriaDto);
        return res.Match(
            response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response),
            error => error.ToHttpResult<CategoriaDto>()
        );
    }
    
    /// <summary>
    /// Elimina lógicamente una categoria mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador de la categoria que se desea eliminar.</param>
    /// <returns>Respuesta HTTP con el resultado de la eliminación o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id) {
        return await service.SoftDelete(id).Match(
            response => Ok(response),
            error => error.ToHttpResult()
        );
    }
    
    /// <summary>
    /// Actualiza los datos de una categoria existente.
    /// </summary>
    /// <param name="dto">DTO con los nuevos datos de la categoria.</param>
    /// <param name="id">Identificador de la categoria que se desea actualizar.</param>
    /// <returns>Respuesta HTTP con la categoria actualizada o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoriaDto>> Update([FromBody] UpdateCategoriaDto dto, string id) {
        return await service.Update(dto, id).Match(
            response => Ok(response),
            error => error.ToHttpResult<CategoriaDto>()
        );
    }
    
    /// <summary>
    /// Obtiene una categoría a partir de su nombre mediante query string.
    /// </summary>
    /// <param name="name">Nombre de la categoría que se desea consultar.</param>
    /// <returns>Respuesta HTTP con la categoría encontrada o el error correspondiente.</returns>
    [Authorize(Roles = "ADMIN,USER")]
    [HttpGet]
    public async Task<ActionResult<CategoriaDto>> GetByName([FromQuery] string name) {
        return await service.GetByName(name).Match(
            response => Ok(response),
            error => error.ToHttpResult<CategoriaDto>()
        );
    }
}