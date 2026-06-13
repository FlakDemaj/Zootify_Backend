using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with specimen-collection link query operations.
/// </summary>
public interface ISpecimenCollectionRepository : IBaseRepository<SpecimenCollectionEntity>
{
    /// <summary>
    /// Returns the specimen-collection link matching both GUIDs, or <c>null</c> if no such link exists.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection.</param>
    /// <param name="specimenGuid">The GUID of the specimen.</param>
    Task<SpecimenCollectionEntity?> GetByCollectionAndSpecimenAsync(Guid collectionGuid, Guid specimenGuid);
}
