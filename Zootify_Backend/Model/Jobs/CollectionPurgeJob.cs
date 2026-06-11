using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

public class CollectionPurgeJob : PurgeJobBase
{
    public CollectionPurgeJob(IServiceScopeFactory scopeFactory, ILogger<CollectionPurgeJob> logger)
        : base(scopeFactory, logger)
    {
    }

    protected override string JobName => "CollectionPurgeJob";

    protected override async Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<ICollectionPurgeService>();
        return await service.PurgeBatchAsync();
    }
}
