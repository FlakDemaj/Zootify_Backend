namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines operations for sending staged due-date reminder emails to loan borrowers and lenders.
/// </summary>
public interface ILoanReminderService
{
    /// <summary>
    /// Sends reminder emails for all three reminder stages and marks processed loans accordingly.
    /// </summary>
    /// <returns>The total number of loans for which reminders were sent across all stages.</returns>
    Task<int> SendReminderBatchAsync();
}
