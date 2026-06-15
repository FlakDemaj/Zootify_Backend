using Zootify_Backend.DTOs.Common.Response.Helpers;

namespace Zootify_Backend.DTOs.Common.Response;

public class PublicSpecimenOverviewDto
{
    public required string Name { get; init; }

    public required string LatinName { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string? City { get; init; }

    public List<GeoLocationNodeDto> LocationPath { get; init; } = [];

    public Guid? ImageGuid { get; init; }

    public DateTime CreatedAt { get; init; }

    public string TaxonGroup { get; init; } = string.Empty;
}
