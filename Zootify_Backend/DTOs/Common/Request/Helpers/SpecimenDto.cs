using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request.Helpers;

public class SpecimenDto
{
    public Guid SpecimenGuid { get; init; }
    
    public required string Name { get; init; }
    
    public required string LatinName  { get; init; }
    
    public required double Latitude { get; init; }
    
    public required double Longitude { get; init; }
    
    public bool IsPublic { get; init; } = true;
    
    public SpecimenStatus Status { get; init; } =  SpecimenStatus.Active;
}