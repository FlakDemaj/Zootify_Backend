namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for updating an existing specimen's core data.
/// </summary>
public class UpdateSpecimenRequestDto
{
    /// <summary>The specimen's new common display name.</summary>
    public required string Name { get; init; }

    /// <summary>The specimen's new latin name.</summary>
    public required string LatinName  { get; init; }

    /// <summary>The new discovery date.</summary>
    public required DateTime DateOfDiscovery { get; init; }

    /// <summary>The new free-text comment, or <c>null</c> to clear it.</summary>
    public required string? Comment { get; init; }

    /// <summary>Whether the specimen should be publicly visible.</summary>
    public required bool IsPublic { get; init; }
}
