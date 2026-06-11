using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ICollectionRepository : IBaseRepository<CollectionEntity>
{
    Task<List<CollectionEntity>> GetAllCollectionsByNameFilterAsync(
        IAuthentication? authentication,
        string? filterByName,
        BaseSortAndPagingDto paging);

    Task<CollectionEntity?> GetCollectionByGuidAsync(Guid guid);

    Task<CollectionEntity?> GetCollectionWithAuditAsync(Guid guid);

    Task<List<CollectionEntity>> GetDeletedCollectionsForPurgeAsync(DateTime cutoff, int batchSize);
}
