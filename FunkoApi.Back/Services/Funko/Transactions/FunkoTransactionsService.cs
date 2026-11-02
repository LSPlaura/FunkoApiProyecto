using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Data;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FunkoApi.Back.Services.Funko.Transactions;

public class FunkoTransactionsService(
    AppDbContext context,
    string cacheKeyPrefix,
    IOptions<CacheOptions> cacheOptions,
    ICacheService cache,
    IStorageService storage)
{
    public async Task<Result<bool, DomainError>> Delete(Models.Funko funko)
    {
        Log.Information("Iniciando proceso de borrado para el Funko con ID {Id}", funko.Id);

        var imagePath = funko.Imagen;
        
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Funkos.Attach(funko);
            context.Funkos.Remove(funko);
            await context.SaveChangesAsync();
            try
            {
                if (!string.IsNullOrEmpty(imagePath))
                {
                    await storage.DeleteFileAsync(imagePath);
                }
            }
            catch (Exception fileEx)
            {
                Log.Warning(fileEx, "Fallo al borrar el archivo físico {ImagePath}. Ejecutando Rollback de BDD.",
                    imagePath);

                await transaction.RollbackAsync();

                return Result.Failure<bool, DomainError>(
                    new BusinessRuleError($"No se pudo eliminar el archivo asociado al Funko: {fileEx.Message}"));
            }
            await transaction.CommitAsync();

            Log.Information("Funko {Id} eliminado correctamente de la base de datos y almacenamiento", funko.Id);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Log.Error(ex, "Error crítico al intentar eliminar el Funko {Id}", funko.Id);
            return Result.Failure<bool, DomainError>(
                new BusinessRuleError($"Error en el proceso de eliminación: {ex.Message}"));
        }
    }
    
    public async Task<Result<Models.Funko, DomainError>> Update(
        Models.Funko funkoToUpdate, 
        string newImage, 
        string oldImage)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Funkos.Update(funkoToUpdate);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            
            await storage.DeleteFileAsync(oldImage);

            Log.Information("Funko {Id} actualizado correctamente en base de datos y almacenamiento", funkoToUpdate.Id);
            return funkoToUpdate;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            await storage.DeleteFileAsync(newImage);

            Log.Error(ex, "Error crítico al intentar actualizar el Funko {Id}", funkoToUpdate.Id);
            return Result.Failure<Models.Funko, DomainError>(
                new BusinessRuleError($"Error en el proceso de actualización: {ex.Message}"));
        }
    }
}