using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request.Helpers;

public class SpecimenDto
{
    public Guid SpecimenGuid { get; set; }
    
    public required string Name { get; set; }
    
    public required string LatinName  { get; set; }
    
    public required double Latitude { get; set; }
    
    public required double Longitude { get; set; }
    
    public bool IsPublic { get; set; } = true;
    
    public SpecimenStatus Status { get; set; } =  SpecimenStatus.Active;
}