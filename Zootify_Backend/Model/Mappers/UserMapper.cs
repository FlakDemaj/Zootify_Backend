using AutoMapper;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Auth.Request;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
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

        CreateMap<UserEntity, GetUserResponseDto>()
            .ForMember(d => d.UserGuid,
                o => o.MapFrom(
                    s => s.Guid))
            .ForMember(d => d.Email,
                o => o.MapFrom(
                    s => s.Email))
            .ForMember(d => d.FirstName,
                o => o.MapFrom(
                    s => s.FirstName))
            .ForMember(d => d.LastName,
                o => o.MapFrom(
                    s => s.LastName))
            .ForMember(d => d.UserStatus,
                o => o.MapFrom(
                    s => s.Status))
            .ForMember(d => d.LoginAttempts,
                o => o.MapFrom(
                    s => s.LoginAttempts))
            .ForMember(
                d => d.UserRole,
                o => o.MapFrom(
                    s => s.Role))
            .ForMember(
                d => d.CreatedAt,
                o => o.MapFrom(
                    s => s.CreationDate))
            .ForMember(
                d => d.UpdatedAt,
                o => o.MapFrom(
                    s => s.UpdatedDate))
            .ForMember(d => d.UpdatedBy,
                o => o.MapFrom<AuditUserFullNameResolver, UserEntity?>(
                    s => s.UpdatedByUser))
            .ForMember(d => d.DeletedAt,
                o => o.MapFrom(
                    s => s.DeletionDate))
            .ForMember(d => d.DeletedBy,
                o => o.MapFrom<AuditUserFullNameResolver, UserEntity?>(
                    s => s.DeletedByUser));

        CreateMap<UpdateUserRequestDto, UserEntity>(MemberList.None)
            .ForMember(d => d.Role,
                o => o.MapFrom(s => s.Role))
            .ForMember(d => d.FirstName,
                o => o.MapFrom(s => s.FirstName))
            .ForMember(d => d.LastName,
                o => o.MapFrom(s => s.LastName));

        CreateMap<UpdateOwnUserRequestDto, UserEntity>(MemberList.None)
            .ForMember(d => d.FirstName,
                o => o.MapFrom(s => s.FirstName))
            .ForMember(d => d.LastName,
                o => o.MapFrom(s => s.LastName))
            .ForMember(d => d.Email,
                o => o.MapFrom(s => s.Email));
        
        CreateMap<UserEntity, GetOwnUserData>()
            .ForMember(d => d.FirstName,
                o => o.MapFrom(s => s.FirstName))
            .ForMember(d => d.LastName,
                o => o.MapFrom(s => s.LastName))
            .ForMember(d => d.Email,
                o => o.MapFrom(s => s.Email));

    }
}
