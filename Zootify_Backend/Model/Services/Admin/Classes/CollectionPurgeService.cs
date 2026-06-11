using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

public class CollectionPurgeService : ICollectionPurgeService
{
    private readonly ICollectionRepository _collectionRepository;

    public CollectionPurgeService(ICollectionRepository collectionRepository)
    {
        _collectionRepository = collectionRepository;
    }

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
