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
    public BaseRepository(ZootifyDbContext context)
        : base(context)
    {
    }

    public async Task<T> CreateAsync(T objectToCreate)
    {
        return await Context.CreateAsync(objectToCreate);
    }
    
    public async Task DeleteAsync(T objectToDelete)
    {
        await Context.DeleteAsync(objectToDelete);
    }

    public async Task UpdateAsync(T objectToUpdate)
    {
        await Context.UpdateAndSaveChangesAsync(objectToUpdate);
    }
}