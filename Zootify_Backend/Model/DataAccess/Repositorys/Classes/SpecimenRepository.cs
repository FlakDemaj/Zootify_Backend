using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class SpecimenRepository : BaseRepository<SpecimenEntity>, ISpecimenRepository
{
    public SpecimenRepository(ZootifyDbContext context) : base(context)
    {
    }
}