namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface ISpecimenPurgeService
{
    Task<int> PurgeBatchAsync();
}
