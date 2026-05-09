using Zootify_Backend.DTOs.Request;
using Zootify_Backend.DTOs.Response;

namespace Zootify_Backend.Model.Services.Interfaces;

public interface IUserService
{
    public Task<LoginUserResponseDto> LoginAsync(LoginUserRequestDto request);
    
    public Task
        RegisterAsync(RegisterUserRequestDto request);

    public Task ConfirmRegistrationAsync(string token);
    
    public Task SendNewRegistrationEmailAsync(
        SendNewRegistrationEmailRequestDto request);
    
    public Task SendNewResetPasswordEmailAsync(
        SendNewResetPasswordEmailRequestDto request);
    
    public Task ResetPasswordAsync(ResetPasswordRequestDto request, string resetPasswordToken);
}