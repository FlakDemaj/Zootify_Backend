using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ISpecimenCollectionRepository : IBaseRepository<SpecimenCollectionEntity>
{
    Task<SpecimenCollectionEntity?> GetByCollectionAndSpecimenAsync(Guid collectionGuid, Guid specimenGuid);
}
