namespace Zootify_Backend.Models.Providers;

public interface IBrevoProvider
{
    public Task<bool> SendRegistrationEmail(
        string token,
        string email,
        string firstname,
        string lastname);

    public Task<bool> SendConfirmationResetPasswordEmail(
        string firstname,
        string lastname,
        string email);

    public Task<bool> SendResetPasswordEmail(
        string resetToken,
        string email,
        string firstname,
        string lastname);
}