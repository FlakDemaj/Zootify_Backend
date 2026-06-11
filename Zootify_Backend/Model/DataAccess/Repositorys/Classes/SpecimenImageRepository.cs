using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class SpecimenImageRepository : BaseRepository<SpecimenImageEntity>, ISpecimenImageRepository
{
    public SpecimenImageRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<SpecimenImageEntity?> GetSpecimenImageByGuid(Guid guid)
    {
        return await Context
            .GetTrackingSet<SpecimenImageEntity>()
            .FirstOrDefaultAsync(im => im.Guid == guid);
    }
}