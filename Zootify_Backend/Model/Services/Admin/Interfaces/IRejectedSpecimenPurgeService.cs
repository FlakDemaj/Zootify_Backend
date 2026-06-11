namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface IRejectedSpecimenPurgeService
{
    Task<int> PurgeBatchAsync();
}
