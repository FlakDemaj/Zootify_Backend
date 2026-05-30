using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ITagRepository : IBaseRepository<TagEntity>
{
    Task<TagEntity?> GetTagByGuidAsync(Guid guid);
}