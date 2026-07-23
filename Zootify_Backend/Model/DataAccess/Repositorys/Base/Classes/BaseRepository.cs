using Microsoft.EntityFrameworkCore;
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
    /// <param name="context">The database context to use for persistence operations.</param>
    public BaseRepository(ZootifyDbContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Persists a new entity to the database and returns the saved instance.
    /// </summary>
    /// <param name="objectToCreate">The entity to persist.</param>
    /// <returns>The saved entity instance.</returns>
    public async Task<T> CreateAsync(T objectToCreate)
    {
        return await Context.CreateAsync(objectToCreate);
    }

    /// <summary>
    /// Persists a Set of new entitys to the database and returns the saved instance.
    /// </summary>
    /// <param name="objectsToCreate">The list of entities to persist.</param>
    /// <returns>The saved entity instances.</returns>
    public async Task<List<T>> MassCreateAsync(List<T> objectsToCreate)
    {
        return await Context.MassCreateAsync(objectsToCreate);
    }

    /// <summary>
    /// Removes the given entity from the database.
    /// </summary>
    /// <param name="objectToDelete">The entity to remove.</param>
    public async Task DeleteAsync(T objectToDelete)
    {
        await Context.DeleteAsync(objectToDelete);
    }

    /// <summary>
    /// Removes the a set of entitys from the database.
    /// </summary>
    /// <param name="objectsToDelete">The list of entities to remove.</param>
    public async Task MassDeleteAsync(List<T> objectsToDelete)
    {
        Context.Set<T>().RemoveRange(objectsToDelete);
        await Context.SaveChangesAsync(); 
    }

    /// <summary>
    /// Saves changes made to the given tracked entity back to the database.
    /// </summary>
    /// <param name="objectToUpdate">The entity to update.</param>
    public async Task UpdateAsync(T objectToUpdate)
    {
        await Context.UpdateAndSaveChangesAsync(objectToUpdate);
    }

    /// <summary>
    /// Saves changes made to a set of entities back to the database.
    /// Only the roots are marked modified — unlike <c>UpdateRange</c>, which attaches the whole
    /// navigation graph and throws an identity conflict as soon as two roots from a no-tracking
    /// query reference the same related entity (e.g. two loans sharing a borrower).
    /// </summary>
    /// <param name="objectsToUpdate">The list of entities to update.</param>
    public async Task MassUpdateAsync(List<T> objectsToUpdate)
    {
        foreach (var objectToUpdate in objectsToUpdate)
        {
            var entry = Context.Entry(objectToUpdate);

            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Modified;
            }
        }

        await Context.SaveChangesAsync();
    }
}
