using Zootify_Backend.DTOs.Request;
using Zootify_Backend.DTOs.Response;

namespace Zootify_Backend.Models.Services.Interfaces;

public interface IUserService
{
    public Task<RegisterUserResponseDto>
        RegisterAsync(RegisterUserRequestDto registerUserRequestDto);

    public Task<LoginUserResponseDto> ConfirmRegistrationAsync(string token);
    
    public Task<SendNewRegistrationEmailResponseDto> SendNewRegistrationEmailAsync(
        SendNewRegistrationEmailRequestDto request);
}