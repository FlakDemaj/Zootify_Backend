using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class RelationSpecimensTagRepository : BaseRepository<RelationSpecimensTagEntity>, IRelationSpecimensTagRepository
{
    public RelationSpecimensTagRepository(ZootifyDbContext context) : base(context)
    {
    }
}