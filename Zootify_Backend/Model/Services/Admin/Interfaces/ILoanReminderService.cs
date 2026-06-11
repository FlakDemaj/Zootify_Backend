namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface ILoanReminderService
{
    Task<int> SendReminderBatchAsync();
}
