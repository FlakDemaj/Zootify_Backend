using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Repositorys.Interfaces;

public interface IBaseRepository<T>
    where T : BaseGuidEntity
{
    Task<T> CreateAsync(T objectToCreate);
    
    Task DeleteAsync(T objectToDelete);

    Task UpdateAsync(T objectToUpdate);
}