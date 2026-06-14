using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="GeographicLocationEntity"/> records.
/// </summary>
public class GeographicLocationRepository : BaseRepository<GeographicLocationEntity>, IGeographicLocationRepository
{
    /// <summary>
    /// Initializes a new <see cref="GeographicLocationRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public GeographicLocationRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns a geographic location matching the given name and optional parent GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="locationName">The name of the geographic location to search for.</param>
    /// <param name="parentGuid">The GUID of the parent location, or <c>null</c> for root-level locations.</param>
    public async Task<GeographicLocationEntity?> GetGeographicLocationByLocationNameAsync(
        string locationName,
        Guid? parentGuid = null)
    {
        return await Context
            .GetTrackingSet<GeographicLocationEntity>()
            .FirstOrDefaultAsync(gl => gl.Name  == locationName
            && gl.ParentGuid == parentGuid);
    }
}