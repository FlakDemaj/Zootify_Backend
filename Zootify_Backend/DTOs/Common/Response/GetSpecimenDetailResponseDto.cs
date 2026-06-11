using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetSpecimenDetailResponseDto
{
    public Guid SpecimenGuid { get; set; }
    
    public required string Name { get; set; }
    
    public required string LatinName { get; set; }
    
    public DateTime DateOfDiscovery { get; set; }
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    public string? StorageBuilding { get; set; }
    
    public string? StorageCabinet { get; set; }
    
    public string? StorageShelf { get; set; }
    
    public string? StorageCompartment { get; set; }
    
    public string? Comment { get; set; }
    
    public bool IsPublic { get; set; }
    
    public SpecimenStatus Status { get; set; }
    
    public Guid? ImageGuid { get; set; }
    
    public string? CreatedByName { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    
    public string? UpdatedByName { get; set; }

    public DateTime? UpdatedAt { get; set; }
    
    public string? DeletedByName { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    
    public List<GeoLocationNodeDto> GeoPath { get; set; } = [];
    
    public List<GetAllTaxonomyNodesByFilterDto> TaxonomyPath { get; set; } = [];
}
