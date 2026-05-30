using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class TagRepository : BaseRepository<TagEntity>, ITagRepository
{
    public TagRepository(ZootifyDbContext context) : base(context)
    {
    }

    public Task<TagEntity?> GetTagByGuidAsync(Guid guid)
    {
        return Context
            .GetTrackingSet<TagEntity>()
            .FirstOrDefaultAsync(x => x.Guid == guid);
    }
}