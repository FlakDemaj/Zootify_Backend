using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class CollectionRepository : BaseRepository<CollectionEntity>, ICollectionRepository
{
    public CollectionRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<List<CollectionEntity>> GetPublicCollectionsAsync()
    {
        return await Context.GetTrackingSet<CollectionEntity>()
            .Include(c => c.Collection)
            .Where(c => !c.IsPrivate && c.DeletionDate == null)
            .ToListAsync();
    }

    public async Task<CollectionEntity?> GetCollectionByGuidAsync(Guid guid)
    {
        return await Context.GetTrackingSet<CollectionEntity>()
            .Include(c => c.Collection)
            .FirstOrDefaultAsync(c => c.Guid == guid);
    }
}
