using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A lightweight species node used by the specimen-creation species picker. Carries only the data the
/// picker needs (GUID, name, approval status) instead of the full taxonomy filter DTO.
/// </summary>
public class GetSpeciesForCreationDto
{
    public required Guid TaxonomyGuid { get; init; }

    public required string Name { get; init; }

    public required ApprovalStatus ApprovalStatus { get; init; }
}
