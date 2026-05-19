using AutoMapper;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

public class AuditUserFullNameResolver 
    : IMemberValueResolver<UserEntity, GetUserResponseDto, UserEntity?, string?>
{
    public string? Resolve(
        UserEntity source,
        GetUserResponseDto destination,
        UserEntity? sourceMember,
        string? destinationMember,
        ResolutionContext context)
    {
        if (sourceMember == null)
        {
            return null;
        }

        return sourceMember.FirstName + " " + sourceMember.LastName;
    }
}