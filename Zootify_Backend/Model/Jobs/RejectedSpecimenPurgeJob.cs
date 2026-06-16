using Zootify_Backend.Configuration;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

/// <summary>
/// Background job that permanently purges rejected specimens in batches, on its own configurable interval.
/// </summary>
public class RejectedSpecimenPurgeJob : PurgeJobBase
{
    /// <summary>
    /// Initializes the job with its scope factory and logger.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a fresh DI scope per batch.</param>
    /// <param name="logger">Logger used to report job progress and failures.</param>
    public RejectedSpecimenPurgeJob(IServiceScopeFactory scopeFactory, ILogger<RejectedSpecimenPurgeJob> logger)
        : base(scopeFactory, logger)
    {
    }

    /// <inheritdoc/>
    protected override string JobName => "RejectedSpecimenPurgeJob";

    /// <inheritdoc/>
    protected override int IntervalInHours => ZootifyConfiguration.RejectedSpecimenPurgeJobIntervalInHours;

    /// <inheritdoc/>
    protected override async Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<IRejectedSpecimenPurgeService>();
        return await service.PurgeBatchAsync();
    }
}
