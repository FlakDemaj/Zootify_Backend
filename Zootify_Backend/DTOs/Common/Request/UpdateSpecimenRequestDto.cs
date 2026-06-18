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

    /// <summary>The new storage building identifier, or <c>null</c> to clear it.</summary>
    public string? StorageBuilding { get; init; }

    /// <summary>The new storage cabinet identifier, or <c>null</c> to clear it.</summary>
    public string? StorageCabinet { get; init; }

    /// <summary>The new storage shelf identifier, or <c>null</c> to clear it.</summary>
    public string? StorageShelf { get; init; }

    /// <summary>The new storage compartment identifier, or <c>null</c> to clear it.</summary>
    public string? StorageCompartment { get; init; }
}
