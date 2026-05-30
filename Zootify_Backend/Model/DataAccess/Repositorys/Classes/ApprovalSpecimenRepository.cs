using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class ApprovalSpecimenRepository: BaseRepository<ApprovalSpecimenEntity>, IApprovalSpecimenRepository
{
    public ApprovalSpecimenRepository(ZootifyDbContext context) : base(context)
    {
    }
}