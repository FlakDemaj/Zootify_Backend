using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

public class UserPurgeJob : PurgeJobBase
{
    public UserPurgeJob(IServiceScopeFactory scopeFactory, ILogger<UserPurgeJob> logger)
        : base(scopeFactory, logger)
    {
    }

    protected override string JobName => "UserPurgeJob";

    protected override async Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<IUserPurgeService>();
        return await service.PurgeBatchAsync();
    }
}
