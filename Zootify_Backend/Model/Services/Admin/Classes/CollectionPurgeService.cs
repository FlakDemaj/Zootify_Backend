using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

/// <summary>
/// Purges soft-deleted collections that have exceeded the configured retention period.
/// </summary>
public class CollectionPurgeService : ICollectionPurgeService
{
    private readonly ICollectionRepository _collectionRepository;

    /// <summary>
    /// Initializes a new <see cref="CollectionPurgeService"/> with its required repository.
    /// </summary>
    /// <param name="collectionRepository">Repository for collection persistence operations.</param>
    public CollectionPurgeService(ICollectionRepository collectionRepository)
    {
        _collectionRepository = collectionRepository;
    }

    /// <summary>
    /// Hard-deletes a batch of soft-deleted collections that have passed the configured retention cutoff.
    /// </summary>
    /// <returns>The number of collections deleted in this batch.</returns>
    [Transaction(ReadOnly = false)]
    public async Task<int> PurgeBatchAsync()
    {
        var cutoff = DateTime.UtcNow.AddYears(-ZootifyConfiguration.CleanupJobsRetentionYears);
        var batch = await _collectionRepository.GetDeletedCollectionsForPurgeAsync(
            cutoff, ZootifyConfiguration.CleanupJobsBatchSize);

        if (batch.Count == 0)
        {
            return 0;
        }

        await _collectionRepository.MassDeleteAsync(batch);
        return batch.Count;
    }
}
