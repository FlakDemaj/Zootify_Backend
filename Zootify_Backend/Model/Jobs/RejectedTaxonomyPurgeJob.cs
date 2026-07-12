using Zootify_Backend.Configuration;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

/// <summary>
/// Background job that permanently purges rejected taxonomy nodes in batches, on its own configurable interval.
/// </summary>
public class RejectedTaxonomyPurgeJob : PurgeJobBase
{
    /// <summary>
    /// Initializes the job with its scope factory and logger.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a fresh DI scope per batch.</param>
    /// <param name="logger">Logger used to report job progress and failures.</param>
    public RejectedTaxonomyPurgeJob(IServiceScopeFactory scopeFactory, ILogger<RejectedTaxonomyPurgeJob> logger)
        : base(scopeFactory, logger)
    {
    }

    /// <inheritdoc/>
    protected override string JobName => "RejectedTaxonomyPurgeJob";

    /// <inheritdoc/>
    protected override int IntervalInHours => ZootifyConfiguration.RejectedTaxonomyPurgeJobIntervalInHours;

    /// <inheritdoc/>
    protected override async Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<IRejectedTaxonomyPurgeService>();
        return await service.PurgeBatchAsync();
    }
}
