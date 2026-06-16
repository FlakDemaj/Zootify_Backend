using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The full detail view of a specimen, including location, storage, audit, and taxonomy/geo paths.
/// </summary>
public class GetSpecimenDetailResponseDto
{
    /// <summary>The specimen's GUID.</summary>
    public Guid SpecimenGuid { get; init; }

    /// <summary>The specimen's common display name.</summary>
    public required string Name { get; init; }

    /// <summary>The specimen's latin name.</summary>
    public required string LatinName { get; init; }

    /// <summary>The date the specimen was discovered.</summary>
    public DateTime DateOfDiscovery { get; init; }

    /// <summary>The latitude of the discovery location.</summary>
    public double Latitude { get; init; }

    /// <summary>The longitude of the discovery location.</summary>
    public double Longitude { get; init; }

    /// <summary>Optional storage building identifier.</summary>
    public string? StorageBuilding { get; init; }

    /// <summary>Optional storage cabinet identifier.</summary>
    public string? StorageCabinet { get; init; }

    /// <summary>Optional storage shelf identifier.</summary>
    public string? StorageShelf { get; init; }

    /// <summary>Optional storage compartment identifier.</summary>
    public string? StorageCompartment { get; init; }

    /// <summary>Optional free-text comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Whether the specimen is publicly visible.</summary>
    public bool IsPublic { get; init; }

    /// <summary>The specimen's lifecycle status.</summary>
    public SpecimenStatus Status { get; init; }

    /// <summary>The GUID of the specimen's image, if any.</summary>
    public Guid? ImageGuid { get; init; }

    /// <summary>The display name of the user who created the specimen.</summary>
    public string? CreatedByName { get; init; }

    /// <summary>When the specimen was created.</summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>The display name of the user who last updated the specimen.</summary>
    public string? UpdatedByName { get; init; }

    /// <summary>When the specimen was last updated.</summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>The display name of the user who deleted the specimen.</summary>
    public string? DeletedByName { get; init; }

    /// <summary>When the specimen was deleted.</summary>
    public DateTime? DeletedAt { get; init; }

    /// <summary>The resolved geographic path (country → … → house number) of the location.</summary>
    public List<GeoLocationNodeDto> GeoPath { get; init; } = [];

    /// <summary>The taxonomy path (phylum → … → species) the specimen belongs to.</summary>
    public List<GetAllTaxonomyNodesByFilterDto> TaxonomyPath { get; set; } = [];
}
