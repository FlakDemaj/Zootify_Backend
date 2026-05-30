namespace Zootify_Backend.DTOs.Common.Request;

public class CreateSpecimenRequestDto
{
    public string? Comment { get; set; }

    public required string Name { get; set; }

    public string? LatinName { get; set; }
    
    public required GeoDto Location { get; set; }

    public required DateTime DateOfDiscovery { get; set; }

    public required List<TaxonomyDto> Taxonomies { get; set; }
    
    public List<Guid> Tags { get; set; }

    public List<string> TagsToCreate { get; set; }

    public List<string> Base64Images { get; set; }
}