using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllSpecimenDto
{
    public required Guid Guid { get; set; }
    
    public required string Name { get; set; }
    
    public required string LatinName  { get; set; }
    
    public required GeoDto Geo { get; set; }
    
    public required Dictionary<GeographicLevel, string> GeographicLevels { get; set; }
    
    public required DateTime DateOfDiscovery { get; set; }
    
    public string? Comment { get; set; }
}