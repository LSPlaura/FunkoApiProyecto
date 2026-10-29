using CSharpFunctionalExtensions;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;

namespace FunkoApi.Back.Services.Proveedor;

public class ProveedorService : IProveedorService
{
    public Task<Result<ProveedorDto, DomainError>> Create(CreateProveedorDto item)
    {
        throw new NotImplementedException();
    }

    public Task<Result<ProveedorDto, DomainError>> GetById(long key)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool, DomainError>> SoftDelete(long id)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool, DomainError>> HardDelete(long id)
    {
        throw new NotImplementedException();
    }

    public Task<Result<ProveedorDto, DomainError>> Update(UpdateProveedorDto item, long key)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ProveedorDto>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<Result<ProveedorDto, DomainError>> GetByName(string name)
    {
        throw new NotImplementedException();
    }
}