namespace Zootify_Backend.Models.Providers;

public interface IBrevoProvider
{
    public Task<bool> SendRegistrationEmail(
        string token,
        string email,
        string firstname,
        string lastname);
}