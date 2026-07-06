using Zootify_Backend.DTOs.Common.Response.Helpers;

namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for creating a new specimen.
/// </summary>
public class CreateSpecimenRequestDto
{
    /// <summary>Optional free-text comment about the specimen.</summary>
    public string? Comment { get; init; }

    /// <summary>Optional size of the specimen.</summary>
    public double? Size { get; init; }

    /// <summary>Optional weight of the specimen.</summary>
    public double? Weight { get; init; }

    /// <summary>The specimen's common display name.</summary>
    public required string Name { get; init; }

    /// <summary>The specimen's latin name.</summary>
    public required string LatinName { get; init; }

    /// <summary>The geographic location where the specimen was found.</summary>
    public required GeoDto Location { get; init; }

    /// <summary>The date the specimen was discovered.</summary>
    public required DateTime DateOfDiscovery { get; init; }

    /// <summary>The GUID of the taxonomy species the specimen belongs to.</summary>
    public required Guid TaxonomySpeciesGuid { get; init; }

    /// <summary>Optional storage building identifier.</summary>
    public string? StorageBuilding { get; init; }

    /// <summary>Optional storage cabinet identifier.</summary>
    public string? StorageCabinet { get; init; }

    /// <summary>Optional storage shelf identifier.</summary>
    public string? StorageShelf { get; init; }

    /// <summary>Optional storage compartment identifier.</summary>
    public string? StorageCompartment { get; init; }

    /// <summary>GUIDs of existing tags to link to the specimen.</summary>
    public List<Guid> Tags { get; init; } = [];

    /// <summary>Names of new tags to create and link to the specimen.</summary>
    public List<string> TagsToCreate { get; init; } = [];

    /// <summary>Optional base64-encoded image of the specimen.</summary>
    public string? Base64Image { get; init; }
}
