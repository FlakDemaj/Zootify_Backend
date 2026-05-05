using Zootify_Backend.DTOs;

namespace Zootify_Backend.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserDto dto);
    Task<bool> EmailExistsAsync(string email);
    Task<UserDto?> LoginAsync(LoginUserDto dto);
}
