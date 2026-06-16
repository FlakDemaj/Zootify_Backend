using Zootify_Backend.DTOs.Common.Response.Helpers;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A public-facing overview of a specimen, used on map/list views without sensitive details.
/// </summary>
public class PublicSpecimenOverviewDto
{
    /// <summary>The specimen's common display name.</summary>
    public required string Name { get; init; }

    /// <summary>The specimen's latin name.</summary>
    public required string LatinName { get; init; }

    /// <summary>The latitude of the discovery location.</summary>
    public double Latitude { get; init; }

    /// <summary>The longitude of the discovery location.</summary>
    public double Longitude { get; init; }

    /// <summary>The resolved geographic path (country → … → house number) of the location.</summary>
    public List<GeoLocationNodeDto> LocationPath { get; init; } = [];

    /// <summary>The GUID of the specimen's image, if any.</summary>
    public Guid? ImageGuid { get; init; }

    /// <summary>When the specimen was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>The high-level taxon group (e.g. the phylum or class name) the specimen belongs to.</summary>
    public string TaxonGroup { get; init; } = string.Empty;
}
