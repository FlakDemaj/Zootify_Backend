using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.DTOs;
using Zootify_Backend.Services;

namespace Zootify_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    // POST /api/User/register — neuen Benutzer registrieren
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterUserDto dto)
    {
        if (await userService.EmailExistsAsync(dto.Email))
            return Conflict("Diese E-Mail-Adresse ist bereits vergeben.");

        var user = await userService.RegisterAsync(dto);
        return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
    }

    // POST /api/User/login — einloggen und Benutzerdaten zurückgeben
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginUserDto dto)
    {
        var user = await userService.LoginAsync(dto);

        if (user is null)
            return Unauthorized("E-Mail oder Passwort falsch.");

        return Ok(user);
    }
}
