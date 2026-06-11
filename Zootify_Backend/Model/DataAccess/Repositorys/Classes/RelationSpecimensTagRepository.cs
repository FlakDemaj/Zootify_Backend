using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class RelationSpecimensTagRepository : BaseRepository<RelationSpecimensTagEntity>, IRelationSpecimensTagRepository
{
    public RelationSpecimensTagRepository(ZootifyDbContext context) : base(context)
    {
    }
}