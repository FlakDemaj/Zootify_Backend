using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetSpecimenDetailResponseDto
{
    public Guid SpecimenGuid { get; init; }
    
    public required string Name { get; init; }
    
    public required string LatinName { get; init; }
    
    public DateTime DateOfDiscovery { get; init; }
    
    public double Latitude { get; init; }
    
    public double Longitude { get; init; }
    
    public string? StorageBuilding { get; init; }
    
    public string? StorageCabinet { get; init; }
    
    public string? StorageShelf { get; init; }
    
    public string? StorageCompartment { get; init; }
    
    public string? Comment { get; init; }
    
    public bool IsPublic { get; init; }
    
    public SpecimenStatus Status { get; init; }
    
    public Guid? ImageGuid { get; init; }
    
    public string? CreatedByName { get; init; }
    
    public DateTime? CreatedAt { get; init; }
    
    public string? UpdatedByName { get; init; }

    public DateTime? UpdatedAt { get; init; }
    
    public string? DeletedByName { get; init; }
    
    public DateTime? DeletedAt { get; init; }
    
    public List<GeoLocationNodeDto> GeoPath { get; init; } = [];
    
    public List<GetAllTaxonomyNodesByFilterDto> TaxonomyPath { get; set; } = [];
}
