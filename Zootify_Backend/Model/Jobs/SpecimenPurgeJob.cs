using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Model.Jobs;

public class SpecimenPurgeJob : PurgeJobBase
{
    public SpecimenPurgeJob(IServiceScopeFactory scopeFactory, ILogger<SpecimenPurgeJob> logger)
        : base(scopeFactory, logger)
    {
    }

    protected override string JobName => "SpecimenPurgeJob";

    protected override async Task<int> ExecutePurgeBatchAsync(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<ISpecimenPurgeService>();
        return await service.PurgeBatchAsync();
    }
}
