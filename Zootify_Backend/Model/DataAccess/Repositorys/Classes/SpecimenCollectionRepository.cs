using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class SpecimenCollectionRepository : BaseRepository<SpecimenCollectionEntity>, ISpecimenCollectionRepository
{
    public SpecimenCollectionRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<SpecimenCollectionEntity?> GetByCollectionAndSpecimenAsync(Guid collectionGuid, Guid specimenGuid)
    {
        return await Context.GetTrackingSet<SpecimenCollectionEntity>()
            .FirstOrDefaultAsync(sc => sc.CollectionGuid == collectionGuid 
                                       && sc.SpecimenGuid == specimenGuid);
    }
}
