using Zootify_Backend.DTOs.Common.Response.Helpers;

namespace Zootify_Backend.DTOs.Common.Request;

public class CreateSpecimenRequestDto
{
    public string? Comment { get; init; }

    public required string Name { get; init; }

    public string? LatinName { get; init; }
    
    public required GeoDto Location { get; init; }

    public required DateTime DateOfDiscovery { get; init; }

    public required Guid TaxonomySpeciesGuid { get; init; }
    
    public string? StorageBuilding { get; init; }
    
    public string? StorageCabinet { get; init; }
    
    public string? StorageShelf { get; init; }
    
    public string? StorageCompartment { get; init; }

    public List<Guid> Tags { get; init; } = [];

    public List<string> TagsToCreate { get; init; } = [];

    public string? Base64Image { get; init; }
}