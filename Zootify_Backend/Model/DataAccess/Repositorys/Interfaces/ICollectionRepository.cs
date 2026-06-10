using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ICollectionRepository : IBaseRepository<CollectionEntity>
{
    Task<List<CollectionEntity>> GetAllCollectionsByNameFilterAsync(
        IAuthentication? authentication,
        string? filterByName,
        string? orderBy,
        string? orderDirection);

    Task<CollectionEntity?> GetCollectionByGuidAsync(Guid guid);

    Task<CollectionEntity?> GetCollectionWithAuditAsync(Guid guid);
}
