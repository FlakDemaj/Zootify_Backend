using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Defines basic CRUD operations for entities extending <see cref="BaseGuidEntity"/>.
/// </summary>
public interface IBaseRepository<T>
    where T : BaseGuidEntity
{
    /// <summary>
    /// Persists a new entity to the database and returns the saved instance.
    /// </summary>
    Task<T> CreateAsync(T objectToCreate);

    /// <summary>
    /// Removes the given entity from the database.
    /// </summary>
    Task DeleteAsync(T objectToDelete);

    /// <summary>
    /// Saves changes made to the given tracked entity back to the database.
    /// </summary>
    Task UpdateAsync(T objectToUpdate);
}
