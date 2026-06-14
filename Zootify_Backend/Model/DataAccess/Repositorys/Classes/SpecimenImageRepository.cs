using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="SpecimenImageEntity"/> records.
/// </summary>
public class SpecimenImageRepository : BaseRepository<SpecimenImageEntity>, ISpecimenImageRepository
{
    /// <summary>
    /// Initializes a new <see cref="SpecimenImageRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public SpecimenImageRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the specimen image with the given GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the specimen image to retrieve.</param>
    public async Task<SpecimenImageEntity?> GetSpecimenImageByGuid(Guid guid)
    {
        return await Context
            .GetTrackingSet<SpecimenImageEntity>()
            .FirstOrDefaultAsync(im => im.Guid == guid);
    }
}