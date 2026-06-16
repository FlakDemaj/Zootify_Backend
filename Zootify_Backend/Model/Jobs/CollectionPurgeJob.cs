using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

/// <summary>
/// Background job that permanently purges soft-deleted collections in batches.
/// </summary>
public class CollectionPurgeJob : PurgeJobBase
{
    /// <summary>
    /// Initializes the job with its scope factory and logger.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a fresh DI scope per batch.</param>
    /// <param name="logger">Logger used to report job progress and failures.</param>
    public CollectionPurgeJob(IServiceScopeFactory scopeFactory, ILogger<CollectionPurgeJob> logger)
        : base(scopeFactory, logger)
    {
    }

    /// <inheritdoc/>
    protected override string JobName => "CollectionPurgeJob";

    /// <inheritdoc/>
    protected override async Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<ICollectionPurgeService>();
        return await service.PurgeBatchAsync();
    }
}
