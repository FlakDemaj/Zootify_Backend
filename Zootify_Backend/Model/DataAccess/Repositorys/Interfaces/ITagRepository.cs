using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ITagRepository : IBaseRepository<TagEntity>
{
    Task<List<TagEntity>> GetTagByGuidAsync(List<Guid> guids);
    
    Task<List<TagEntity>> GetTagsByStartsWithNameAsync(string? startsWith);
    
    Task<List<TagEntity>> GetTagsByNameAsync(List<string> names);
}