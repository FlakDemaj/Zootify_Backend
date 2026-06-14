using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

/// <summary>
/// Defines basic CRUD operations for entities extending <see cref="BaseGuidEntity"/>.
/// </summary>
public interface IBaseRepository<T>
    where T : BaseGuidEntity
{
    /// <summary>
    /// Persists a new entity to the database and returns the saved instance.
    /// </summary>
    /// <param name="objectToCreate">The entity to persist.</param>
    /// <returns>The saved entity instance.</returns>
    Task<T> CreateAsync(T objectToCreate);
    

    /// <summary>
    /// Persists a set of new entitys to the database and returns the saved instance.
    /// </summary>
    /// <param name="objectsToCreate">The list of entities to persist.</param>
    /// <returns>The saved entity instances.</returns>
    Task<List<T>> MassCreateAsync(List<T> objectsToCreate);

    /// <summary>
    /// Removes the given entity from the database.
    /// </summary>
    /// <param name="objectToDelete">The entity to remove.</param>
    Task DeleteAsync(T objectToDelete);

    /// <summary>
    /// Removes a set of entitys from the database.
    /// </summary>
    /// <param name="objectsToDelete">The list of entities to remove.</param>
    Task MassDeleteAsync(List<T> objectsToDelete);

    /// <summary>
    /// Saves changes made to the given tracked entity back to the database.
    /// </summary>
    /// <param name="objectToUpdate">The entity to update.</param>
    Task UpdateAsync(T objectToUpdate);

    /// <summary>
    /// Saves changes made to a set of tracked entities back to the database.
    /// </summary>
    /// <param name="objectsToUpdate">The list of entities to update.</param>
    Task MassUpdateAsync(List<T> objectsToUpdate);
}
