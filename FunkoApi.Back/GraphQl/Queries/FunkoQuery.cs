using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Services.Funko;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Data;

namespace FunkoApi.Back.GraphQl.Queries;

public class FunkoQuery
{
    [UsePaging(MaxPageSize = 50, DefaultPageSize = 10)]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<Funko>> GetFunkos([Service] IFunkoRepository funkoRepository)
    {
        var result = await funkoRepository.GetAll();
        return result.AsQueryable();
    }

    public async Task<Funko?> GetFunko(long id, [Service] IFunkoRepository funkoRepository)
    {
        var result = await funkoRepository.GetById(id);
        return result;
    }
}