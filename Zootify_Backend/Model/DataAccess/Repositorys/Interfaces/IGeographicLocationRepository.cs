using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface IGeographicLocationRepository : IBaseRepository<GeographicLocationEntity>
{
    Task<GeographicLocationEntity?> GetGeographicLocationByLocationNameAsync(
        string locationName, Guid? parentGuid = null);
}