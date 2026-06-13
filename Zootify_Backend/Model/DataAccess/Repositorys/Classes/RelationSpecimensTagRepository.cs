using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for managing <see cref="RelationSpecimensTagEntity"/> records linking specimens to tags.
/// </summary>
public class RelationSpecimensTagRepository : BaseRepository<RelationSpecimensTagEntity>, IRelationSpecimensTagRepository
{
    /// <summary>
    /// Initializes a new <see cref="RelationSpecimensTagRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public RelationSpecimensTagRepository(ZootifyDbContext context) : base(context)
    {
    }
}