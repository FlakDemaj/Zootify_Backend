using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ICollectionRepository : IBaseRepository<CollectionEntity>
{
    Task<List<CollectionEntity>> GetPublicCollectionsAsync();

    Task<CollectionEntity?> GetCollectionByGuidAsync(Guid guid);
}
