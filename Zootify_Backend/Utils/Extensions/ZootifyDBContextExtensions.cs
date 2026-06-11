using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Extensions;

/// <summary>
/// Extension methods for <see cref="ZootifyDbContext"/> providing common persistence operations.
/// </summary>
public static class ZootifyDbContextExtensions
{
    /// <summary>
    /// Adds the entity to the context, saves changes, and detaches it to avoid stale tracking.
    /// </summary>
    /// <typeparam name="TSaveType">The entity type to persist.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="saveData">The entity instance to save.</param>
    /// <returns>The saved and detached entity.</returns>
    public static async Task<TSaveType> CreateAsync<TSaveType>(
        this ZootifyDbContext context,
        TSaveType saveData)
        where TSaveType : BaseGuidEntity
    {
        try
        {
            var result = context.Add(saveData);
            await context.SaveChangesAsync();
            return result.Entity;
        }
        finally
        {
            context.Entry(saveData).State = EntityState.Detached;
        }
    }
    
    /// <summary>
    /// Adds a mass of entitys to the context, saves changes, and detaches it to avoid stale tracking.
    /// </summary>
    /// <typeparam name="TSaveType">The entity type to persist.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="saveData">The entity instance to save.</param>
    /// <returns>The saved and detached entity.</returns>
    public static async Task<List<TSaveType>> MassCreateAsync<TSaveType>(
        this ZootifyDbContext context,
        List<TSaveType> saveData)
        where TSaveType : BaseGuidEntity
    {
        try
        {
            context.AddRange(saveData);
            await context.SaveChangesAsync();
            
            return saveData;
        }
        finally
        {
            foreach (var entity in saveData)
            {
                context.Entry(entity).State = EntityState.Detached;
            }        
        }
    }

    /// <summary>
    /// Removes the entity from the context and saves changes.
    /// </summary>
    /// <typeparam name="TDeleteType">The entity type to remove.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="deleteData">The entity instance to delete.</param>
    public static async Task DeleteAsync<TDeleteType>(
        this ZootifyDbContext context,
        TDeleteType deleteData)
        where TDeleteType : BaseGuidEntity
    {
        context.Set<TDeleteType>().Remove(deleteData);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Saves changes for the given entity, handling detached-state and concurrency gracefully.
    /// Concurrent update conflicts are silently ignored to prevent overwriting concurrent writes.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="updateData">The entity instance to update.</param>
    public static async Task UpdateAndSaveChangesAsync(
        this ZootifyDbContext context,
        object updateData)
    {
        try
        {
            if (context.Entry(updateData).State == EntityState.Detached)
            {
                context.Entry(updateData).State = EntityState.Modified;
            }
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // Silently ignore concurrent-update conflicts — the last writer wins and a
            // concurrent request will have already committed the newer state.
            if (e is DbUpdateConcurrencyException)
            {
                return;
            }

            throw new ZootifyException(ErrorCode.DatabaseError, e);
        }
        finally
        {
            context.Entry(updateData).State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> for the entity type, with or without EF change tracking.
    /// </summary>
    /// <typeparam name="T">The entity type to query.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="tracking">
    /// <c>true</c> to enable change tracking (required for updates);
    /// <c>false</c> for read-only queries (default, better performance).
    /// </param>
    public static IQueryable<T> GetTrackingSet<T>(
        this ZootifyDbContext context,
        bool tracking = false)
        where T : class
    {
        return tracking ? context.Set<T>().AsTracking() : context.Set<T>().AsNoTracking();
    }
}
