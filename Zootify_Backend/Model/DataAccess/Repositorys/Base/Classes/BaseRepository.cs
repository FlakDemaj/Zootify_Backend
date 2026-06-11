using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys.Base;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;

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
    /// Persists a Set of new entitys to the database and returns the saved instance.
    /// </summary>
    public async Task<List<T>> MassCreateAsync(List<T> objectsToCreate)
    {
        return await Context.MassCreateAsync(objectsToCreate);
    }

    /// <summary>
    /// Removes the given entity from the database.
    /// </summary>
    public async Task DeleteAsync(T objectToDelete)
    {
        await Context.DeleteAsync(objectToDelete);
    }

    /// <summary>
    /// Removes the a set of entitys from the database.
    /// </summary>
    public async Task MassDeleteAsync(List<T> objectsToDelete)
    {
        Context.Set<T>().RemoveRange(objectsToDelete);
        await Context.SaveChangesAsync(); 
    }

    /// <summary>
    /// Saves changes made to the given tracked entity back to the database.
    /// </summary>
    public async Task UpdateAsync(T objectToUpdate)
    {
        await Context.UpdateAndSaveChangesAsync(objectToUpdate);
    }

    /// <summary>
    /// Saves changes made to a set of tracked entities back to the database.
    /// </summary>
    public async Task MassUpdateAsync(List<T> objectsToUpdate)
    {
        try
        {
            Context.Set<T>().UpdateRange(objectsToUpdate);
            await Context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
