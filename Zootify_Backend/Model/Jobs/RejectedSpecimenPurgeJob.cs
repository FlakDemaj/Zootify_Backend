using Zootify_Backend.Configuration;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

public class RejectedSpecimenPurgeJob : PurgeJobBase
{
    public RejectedSpecimenPurgeJob(IServiceScopeFactory scopeFactory, ILogger<RejectedSpecimenPurgeJob> logger)
        : base(scopeFactory, logger)
    {
    }

    protected override string JobName => "RejectedSpecimenPurgeJob";

    protected override int IntervalInHours => ZootifyConfiguration.RejectedSpecimenPurgeJobIntervalInHours;

    protected override async Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<IRejectedSpecimenPurgeService>();
        return await service.PurgeBatchAsync();
    }
}
