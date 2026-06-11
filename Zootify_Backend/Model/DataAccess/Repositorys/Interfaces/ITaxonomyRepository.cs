using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ITaxonomyRepository : IBaseRepository<TaxonomyNodeEntity>
{
    Task<TaxonomyNodeEntity?> GetTaxonomyNodeByGuidAsync(Guid guid);
    
    Task<List<TaxonomyNodeEntity>> GetTaxonomyNodeByStartsWithNameAsync(
        string? startsWithName,
        TaxonomyRank rank);

    Task<List<TaxonomyNodeEntity>> GetAllNodesForTreeAsync();
}