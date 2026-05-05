using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Data;
using Zootify_Backend.DTOs;
using Zootify_Backend.Models;

namespace Zootify_Backend.Services;

public class UserService(ZootifyDbContext db) : IUserService
{
    public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
    {
        var user = new User
        {
            FirstName    = dto.FirstName,
            LastName     = dto.LastName,
            Email        = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<bool> EmailExistsAsync(string email)
        => await db.Users.AnyAsync(u => u.Email == email);

    public async Task<UserDto?> LoginAsync(LoginUserDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null;

        return MapToDto(user);
    }

    private static UserDto MapToDto(User u) => new()
    {
        Id        = u.Id,
        FirstName = u.FirstName,
        LastName  = u.LastName,
        Email     = u.Email,
        Role      = u.Role,
        Status    = u.Status,
        CreatedAt = u.CreatedAt
    };
}
