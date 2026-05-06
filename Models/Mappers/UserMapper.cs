using AutoMapper;
using Zootify_Backend.DTOs.Request;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Models.Mappers;

public class UserMapper : Profile
{
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