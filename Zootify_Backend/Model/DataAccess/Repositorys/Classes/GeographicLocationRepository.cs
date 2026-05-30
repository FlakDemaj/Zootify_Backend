using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class GeographicLocationRepository : BaseRepository<GeographicLocationEntity>, IGeographicLocationRepository
{
    public GeographicLocationRepository(ZootifyDbContext context) : base(context)
    {
    }

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