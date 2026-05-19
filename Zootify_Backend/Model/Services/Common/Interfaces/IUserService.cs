using Zootify_Backend.DTOs.Common.Response;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface IUserService
{
    Task<GetUserResponseDto> GetUserByGuidAsync(Guid guid);
}