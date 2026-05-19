using AutoMapper;
using Zootify_Backend.DTOs.Auth.Request;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

/// <summary>
/// AutoMapper profile that defines mappings for user-related types.
/// </summary>
public class UserMapper : Profile
{
    /// <summary>
    /// Configures the mapping from <see cref="RegisterUserRequestDto"/> to <see cref="UserEntity"/>.
    /// </summary>
    public UserMapper()
    {
        CreateMap<RegisterUserRequestDto, UserEntity>(MemberList.None)
            .ForMember(d => d.Email,
                o => o.MapFrom(s => s.Email))
            .ForMember(d => d.FirstName,
                o => o.MapFrom(s => s.FirstName))
            .ForMember(d => d.LastName,
                o => o.MapFrom(s => s.LastName))
            .ForMember(
                d => d.PasswordHash,
                o => o.MapFrom(s => s.Password));
    }
}
