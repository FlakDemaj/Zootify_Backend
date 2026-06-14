using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with specimen image query operations.
/// </summary>
public interface ISpecimenImageRepository : IBaseRepository<SpecimenImageEntity>
{
    /// <summary>
    /// Returns the specimen image with the given GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the specimen image to retrieve.</param>
    Task<SpecimenImageEntity?> GetSpecimenImageByGuid(Guid guid);
}