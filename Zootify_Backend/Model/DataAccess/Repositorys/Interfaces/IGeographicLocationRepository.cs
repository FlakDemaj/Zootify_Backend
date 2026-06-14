using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with geographic location query operations.
/// </summary>
public interface IGeographicLocationRepository : IBaseRepository<GeographicLocationEntity>
{
    /// <summary>
    /// Returns a geographic location matching the given name and optional parent GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="locationName">The name of the geographic location to search for.</param>
    /// <param name="parentGuid">The GUID of the parent location, or <c>null</c> for root-level locations.</param>
    Task<GeographicLocationEntity?> GetGeographicLocationByLocationNameAsync(
        string locationName, Guid? parentGuid = null);
}