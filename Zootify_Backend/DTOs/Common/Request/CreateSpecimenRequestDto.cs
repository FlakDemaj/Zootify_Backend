using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;

namespace Zootify_Backend.DTOs.Common.Request;

public class CreateSpecimenRequestDto
{
    public string? Comment { get; set; }

    public required string Name { get; set; }

    public string? LatinName { get; set; }
    
    public required GeoDto Location { get; set; }

    public required DateTime DateOfDiscovery { get; set; }

    public required Guid TaxonomySpeciesGuid { get; set; }
    
    public string? StorageBuilding { get; set; }
    
    public string? StorageCabinet { get; set; }
    
    public string? StorageShelf { get; set; }
    
    public string? StorageCompartment { get; set; }
    
    public List<Guid> Tags { get; set; }

    public List<string> TagsToCreate { get; set; }

    public string Base64Image { get; set; }
}