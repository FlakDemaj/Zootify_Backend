using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ISpecimenRepository : IBaseRepository<SpecimenEntity>
{
    Task<SpecimenEntity?> GetSpecimenByGuidAsync(Guid guid);

    Task<List<SpecimenEntity>> GetAllSpecimensByStatusAsync(
        IAuthentication authentication,
        SpecimenStatus status);

    Task<List<SpecimenEntity>> GetOwnSpecimensAsync(
        Guid userGuid);
}