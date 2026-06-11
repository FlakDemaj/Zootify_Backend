using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

public class RejectedSpecimenPurgeService : IRejectedSpecimenPurgeService
{
    private readonly ISpecimenRepository _specimenRepository;

    public RejectedSpecimenPurgeService(ISpecimenRepository specimenRepository)
    {
        _specimenRepository = specimenRepository;
    }

    [Transaction(ReadOnly = false)]
    public async Task<int> PurgeBatchAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-ZootifyConfiguration.RejectedSpecimenPurgeJobRetentionDays);
        var batch = await _specimenRepository.GetRejectedSpecimensForPurgeAsync(
            cutoff, ZootifyConfiguration.RejectedSpecimenPurgeJobBatchSize);

        if (batch.Count == 0)
        {
            return 0;
        }

        await _specimenRepository.MassDeleteAsync(batch);
        return batch.Count;
    }
}
