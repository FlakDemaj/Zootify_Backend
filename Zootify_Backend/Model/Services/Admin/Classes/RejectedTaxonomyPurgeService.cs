using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

/// <summary>
/// Purges rejected taxonomy nodes that have exceeded the configured retention period.
/// </summary>
public class RejectedTaxonomyPurgeService : IRejectedTaxonomyPurgeService
{
    private readonly ITaxonomyRepository _taxonomyRepository;

    /// <summary>
    /// Initializes a new <see cref="RejectedTaxonomyPurgeService"/> with its required repository.
    /// </summary>
    /// <param name="taxonomyRepository">Repository for taxonomy node persistence operations.</param>
    public RejectedTaxonomyPurgeService(ITaxonomyRepository taxonomyRepository)
    {
        _taxonomyRepository = taxonomyRepository;
    }

    /// <summary>
    /// Hard-deletes a batch of rejected taxonomy nodes that have passed the configured retention cutoff.
    /// Only leaf nodes are deleted per batch; a rejected parent is purged once its children are gone.
    /// </summary>
    /// <returns>The number of taxonomy nodes deleted in this batch.</returns>
    [Transaction(ReadOnly = false)]
    public async Task<int> PurgeBatchAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-ZootifyConfiguration.RejectedTaxonomyPurgeJobRetentionDays);
        var batch = await _taxonomyRepository.GetRejectedTaxonomyLeafNodesForPurgeAsync(
            cutoff, ZootifyConfiguration.RejectedTaxonomyPurgeJobBatchSize);

        if (batch.Count == 0)
        {
            return 0;
        }

        await _taxonomyRepository.MassDeleteAsync(batch);
        return batch.Count;
    }
}
