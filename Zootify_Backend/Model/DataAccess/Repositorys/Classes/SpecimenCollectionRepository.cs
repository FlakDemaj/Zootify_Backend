using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="SpecimenCollectionEntity"/> link records.
/// </summary>
public class SpecimenCollectionRepository : BaseRepository<SpecimenCollectionEntity>, ISpecimenCollectionRepository
{
    /// <summary>
    /// Initializes a new <see cref="SpecimenCollectionRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public SpecimenCollectionRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the specimen-collection link matching both GUIDs, or <c>null</c> if no such link exists.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection.</param>
    /// <param name="specimenGuid">The GUID of the specimen.</param>
    public async Task<SpecimenCollectionEntity?> GetByCollectionAndSpecimenAsync(Guid collectionGuid, Guid specimenGuid)
    {
        return await Context.GetTrackingSet<SpecimenCollectionEntity>()
            .FirstOrDefaultAsync(sc => sc.CollectionGuid == collectionGuid 
                                       && sc.SpecimenGuid == specimenGuid);
    }
}
