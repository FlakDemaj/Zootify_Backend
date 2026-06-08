using AutoMapper;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

public class AuditUserFullNameResolver 
    : IMemberValueResolver<UserEntity, GetUserResponseDto, UserEntity?, string?>
{
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

/// <summary>
/// Resolves the geographic levels dictionary by walking up the parent chain
/// of the GeographicLocationEntity attached to a SpecimenEntity.
/// Requires the full parent chain to be eagerly loaded via ThenInclude.
/// </summary>
public class GeographicLevelsResolver
    : IValueResolver<SpecimenEntity, GetAllSpecimenDto, Dictionary<GeographicLevel, string>>
{
    public Dictionary<GeographicLevel, string> Resolve(
        SpecimenEntity source,
        GetAllSpecimenDto destination,
        Dictionary<GeographicLevel, string> destMember,
        ResolutionContext context)
    {
        var result = new Dictionary<GeographicLevel, string>();

        if (source.GeographicLocation == null)
        {
            return result;
        }

        var current = source.GeographicLocation;

        while (current != null)
        {
            result[current.Level] = current.Name;
            current = current.Parent;
        }

        return result
            .OrderBy(entry => entry.Key)
            .ToDictionary(entry => entry.Key, entry => entry.Value);
    }
}