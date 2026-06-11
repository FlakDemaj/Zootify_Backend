using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

public class SpecimenPurgeService : ISpecimenPurgeService
{
    private readonly ISpecimenRepository _specimenRepository;

    public SpecimenPurgeService(ISpecimenRepository specimenRepository)
    {
        _specimenRepository = specimenRepository;
    }

    [Transaction(ReadOnly = false)]
    public async Task<int> PurgeBatchAsync()
    {
        var cutoff = DateTime.UtcNow.AddYears(-ZootifyConfiguration.CleanupJobsRetentionYears);
        var batch = await _specimenRepository.GetDeletedSpecimensForPurgeAsync(
            cutoff, ZootifyConfiguration.CleanupJobsBatchSize);

        if (batch.Count == 0) return 0;

        await _specimenRepository.MassDeleteAsync(batch);
        return batch.Count;
    }
}
