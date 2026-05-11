using Zootify_Backend.Extensions;
using Zootify_Backend.Models.DataAccess.DBContext;
using Zootify_Backend.Models.DataAccess.Entitys.Base;
using Zootify_Backend.Models.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Models.DataAccess.Repositorys.Classes;

/// <summary>
/// Base repository class for performing CRUD operations on entities that extend <see cref="BaseGuidEntity"/>.
/// Inherits from <see cref="ZootifyRepository"/> and implements <see cref="IBaseRepository{T}"/>.
/// </summary>
/// <typeparam name="T">The type of entity that extends <see cref="BaseGuidEntity"/>.</typeparam>
public abstract class BaseRepository<T> : ZootifyRepository, IBaseRepository<T>
    where T : BaseGuidEntity
{
    /// <summary>
    /// Initializes a new <see cref="BaseRepository{T}"/> with the given database context.
    /// </summary>
    public BaseRepository(ZootifyDbContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Persists a new entity to the database and returns the saved instance.
    /// </summary>
    public async Task<T> CreateAsync(T objectToCreate)
    {
        return await Context.CreateAsync(objectToCreate);
    }

    /// <summary>
    /// Removes the given entity from the database.
    /// </summary>
    public async Task DeleteAsync(T objectToDelete)
    {
        await Context.DeleteAsync(objectToDelete);
    }

    /// <summary>
    /// Saves changes made to the given tracked entity back to the database.
    /// </summary>
    public async Task UpdateAsync(T objectToUpdate)
    {
        await Context.UpdateAndSaveChangesAsync(objectToUpdate);
    }
}
