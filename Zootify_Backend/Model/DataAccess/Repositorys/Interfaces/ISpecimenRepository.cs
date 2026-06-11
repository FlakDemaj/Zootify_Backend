using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ISpecimenRepository : IBaseRepository<SpecimenEntity>
{
    Task<SpecimenEntity?> GetSpecimenByGuidAsync(
        Guid guid,
        bool includeImage = false,
        bool includeTags = false);

    Task<SpecimenEntity?> GetSpecimenWithDetailByGuidAsync(Guid guid);

    Task<int> CountByStatusAsync(SpecimenStatus status, IAuthentication authentication);

    Task<int> CountOwnAsync(Guid userGuid);

    Task<int> CountPublicAsync();

    Task<(List<SpecimenEntity> Items, int TotalCount)> GetSpecimensInCollectionPagedAsync(
        Guid collectionGuid,
        IAuthentication? authentication,
        BaseSortAndPagingDto paging);

    Task<(List<SpecimenEntity> Items, int TotalCount)> GetSpecimensPagedAsync(
        Guid? userGuid,
        SpecimenStatus? status,
        BaseSortAndPagingDto paging);

    Task<List<SpecimenEntity>> GetSpecimensBySpeciesGuidsAsync(
        List<Guid> speciesGuids,
        SpecimenStatus? status = null);

    Task<List<SpecimenEntity>> GetDeletedSpecimensForPurgeAsync(DateTime cutoff, int batchSize);

    Task<List<SpecimenEntity>> GetRejectedSpecimensForPurgeAsync(DateTime cutoff, int batchSize);
}