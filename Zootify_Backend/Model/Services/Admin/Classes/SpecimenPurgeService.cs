using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

/// <summary>
/// Purges soft-deleted specimens that have exceeded the configured retention period.
/// </summary>
public class SpecimenPurgeService : ISpecimenPurgeService
{
    private readonly ISpecimenRepository _specimenRepository;

    /// <summary>
    /// Initializes a new <see cref="SpecimenPurgeService"/> with its required repository.
    /// </summary>
    /// <param name="specimenRepository">Repository for specimen persistence operations.</param>
    public SpecimenPurgeService(ISpecimenRepository specimenRepository)
    {
        _specimenRepository = specimenRepository;
    }

    /// <summary>
    /// Hard-deletes a batch of soft-deleted specimens that have passed the configured retention cutoff.
    /// </summary>
    /// <returns>The number of specimens deleted in this batch.</returns>
    [Transaction(ReadOnly = false)]
    public async Task<int> PurgeBatchAsync()
    {
        var cutoff = DateTime.UtcNow.AddYears(-ZootifyConfiguration.CleanupJobsRetentionYears);
        var batch = await _specimenRepository.GetDeletedSpecimensForPurgeAsync(
            cutoff, ZootifyConfiguration.CleanupJobsBatchSize);

        if (batch.Count == 0)
        {
            return 0;
        }

        await _specimenRepository.MassDeleteAsync(batch);
        return batch.Count;
    }
}
