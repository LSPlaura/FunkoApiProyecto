using CSharpFunctionalExtensions;
using FunkoApi.Back.Dto.Categoria;
using FunkoApi.Back.Dto.Documentation;
using Swashbuckle.AspNetCore.Annotations;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Services.Categoria;
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
    [SwaggerOperation(OperationId = "Categorias_GetAll")]
    [SwaggerResponse(200, "Colección sin paginar. El repositorio actual incluye categorías eliminadas lógicamente.", typeof(IEnumerable<CategoriaDto>))]
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
    [SwaggerOperation(OperationId = "Categorias_GetById")]
    [SwaggerResponse(200, "Categoría encontrada, con estado y fechas.", typeof(CategoriaDto))]
    [SwaggerResponse(404, "No existe una categoría con ese identificador.", typeof(MessageErrorResponse))]
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
    [SwaggerOperation(OperationId = "Categorias_Create")]
    [SwaggerResponse(201, "Categoría creada; cabecera Location con su URL.", typeof(CategoriaDto))]
    [SwaggerResponse(400, "Nombre obligatorio, no vacío y de hasta 100 caracteres; JSON válido.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(409, "Nombre de categoría duplicado.", typeof(MessageErrorResponse))]
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
    [SwaggerOperation(OperationId = "Categorias_Delete")]
    [SwaggerResponse(200, "Borrado lógico realizado. Cuerpo booleano true, no 204.", typeof(bool))]
    [SwaggerResponse(400, "La categoría está asociada a Funkos o no se pudo efectuar el borrado.", typeof(MessageErrorResponse))]
    [SwaggerResponse(404, "Categoría inexistente.", typeof(MessageErrorResponse))]
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
    [SwaggerOperation(OperationId = "Categorias_Update")]
    [SwaggerResponse(200, "Categoría actualizada.", typeof(CategoriaDto))]
    [SwaggerResponse(400, "Nombre obligatorio y de hasta 100 caracteres; JSON válido.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(404, "Categoría inexistente o no encontrada al actualizar.", typeof(MessageErrorResponse))]
    [SwaggerResponse(409, "El nuevo nombre pertenece a otra categoría.", typeof(MessageErrorResponse))]
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
    [HttpGet("nombre")]
    [SwaggerOperation(OperationId = "Categorias_GetByName")]
    [SwaggerResponse(200, "Categoría con el nombre exacto indicado en name.", typeof(CategoriaDto))]
    [SwaggerResponse(400, "Falta el parámetro obligatorio name.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(404, "No existe una categoría con ese nombre.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<CategoriaDto>> GetByName([FromQuery] string name) {
        return await service.GetByName(name).Match(
            response => Ok(response),
            error => error.ToHttpResult<CategoriaDto>()
        );
    }
}
