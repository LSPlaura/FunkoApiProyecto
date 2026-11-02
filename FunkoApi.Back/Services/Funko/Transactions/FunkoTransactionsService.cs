using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Data;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunkoApi.Back.Services.Funko.Transactions;

/// <summary>
/// Servicio encargado de gestionar las operaciones transaccionales complejas 
/// y el almacenamiento asociado para las entidades Funko.
/// </summary>
public class FunkoTransactionsService(
    AppDbContext context,
    string cacheKeyPrefix,
    IOptions<CacheOptions> cacheOptions,
    ICacheService cache,
    IStorageService storage,
    ILogger<FunkoTransactionsService> logger)
{
    /// <summary>
    /// Elimina un Funko de la base de datos de forma transaccional junto con su archivo de imagen asociado.
    /// </summary>
    /// <param name="funko">Instancia del Funko a eliminar.</param>
    /// <returns>Un resultado que indica <c>true</c> si la operación fue exitosa, o un <see cref="DomainError"/> si falla.</returns>
    public async Task<Result<bool, DomainError>> Delete(Models.Funko funko)
    {
        logger.LogInformation("Iniciando proceso de borrado para el Funko con ID {Id}", funko.Id);

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
                logger.LogWarning(fileEx, "Fallo al borrar el archivo físico {ImagePath}. Ejecutando Rollback de BDD.",
                    imagePath);

                await transaction.RollbackAsync();

                return Result.Failure<bool, DomainError>(
                    new BusinessRuleError($"No se pudo eliminar el archivo asociado al Funko: {fileEx.Message}"));
            }
            await transaction.CommitAsync();

            logger.LogInformation("Funko {Id} eliminado correctamente de la base de datos y almacenamiento", funko.Id);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error crítico al intentar eliminar el Funko {Id}", funko.Id);
            return Result.Failure<bool, DomainError>(
                new BusinessRuleError($"Error en el proceso de eliminación: {ex.Message}"));
        }
    }
    
    /// <summary>
    /// Actualiza la información de un Funko de forma transaccional gestionando el reemplazo de su imagen en el almacenamiento.
    /// </summary>
    /// <param name="funkoToUpdate">Instancia del Funko con los datos actualizados.</param>
    /// <param name="newImage">Ruta o identificador de la nueva imagen cargada.</param>
    /// <param name="oldImage">Ruta o identificador de la imagen anterior que debe ser eliminada.</param>
    /// <returns>El Funko actualizado, o un <see cref="DomainError"/> si ocurre algún fallo durante el proceso.</returns>
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

            logger.LogInformation("Funko {Id} actualizado correctamente en base de datos y almacenamiento", funkoToUpdate.Id);
            return funkoToUpdate;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            await storage.DeleteFileAsync(newImage);

            logger.LogError(ex, "Error crítico al intentar actualizar el Funko {Id}", funkoToUpdate.Id);
            return Result.Failure<Models.Funko, DomainError>(
                new BusinessRuleError($"Error en el proceso de actualización: {ex.Message}"));
        }
    }
}