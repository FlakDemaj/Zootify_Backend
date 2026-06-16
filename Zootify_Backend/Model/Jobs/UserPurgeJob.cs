using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

/// <summary>
/// Background job that permanently purges soft-deleted users in batches (after anonymizing their references).
/// </summary>
public class UserPurgeJob : PurgeJobBase
{
    /// <summary>
    /// Initializes the job with its scope factory and logger.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a fresh DI scope per batch.</param>
    /// <param name="logger">Logger used to report job progress and failures.</param>
    public UserPurgeJob(IServiceScopeFactory scopeFactory, ILogger<UserPurgeJob> logger)
        : base(scopeFactory, logger)
    {
    }

    /// <inheritdoc/>
    protected override string JobName => "UserPurgeJob";

    /// <inheritdoc/>
    protected override async Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<IUserPurgeService>();
        return await service.PurgeBatchAsync();
    }
}
