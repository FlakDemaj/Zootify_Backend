using AutoMapper;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

/// <summary>
/// AutoMapper value resolver that turns an audit-user navigation into a display name, falling back to
/// "Deleted User" when the referenced user no longer exists.
/// </summary>
public class AuditUserFullNameResolver
    : IMemberValueResolver<UserEntity, GetUserResponseDto, UserEntity?, string?>
{
    /// <summary>
    /// Resolves the audit user's full name, or "Deleted User" when the source member is <c>null</c>.
    /// </summary>
    /// <param name="source">The user being mapped (unused).</param>
    /// <param name="destination">The destination DTO (unused).</param>
    /// <param name="sourceMember">The audit user navigation to resolve a name from.</param>
    /// <param name="destinationMember">The current destination value (unused).</param>
    /// <param name="context">The AutoMapper resolution context.</param>
    /// <returns>The audit user's full name, or "Deleted User" if none.</returns>
    public string Resolve(
        UserEntity source,
        GetUserResponseDto destination,
        UserEntity? sourceMember,
        string? destinationMember,
        ResolutionContext context)
    {
        if (sourceMember == null)
        {
            return "Deleted User";
        }

        return sourceMember.FirstName + " " + sourceMember.LastName;
    }
}