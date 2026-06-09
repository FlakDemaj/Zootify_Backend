using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateSpecimenRequestDto
{
    public required string Name { get; set; }
    
    public required string LatinName  { get; set; }
    
    public required DateTime DateOfDiscovery { get; set; }
    
    public required string? Comment { get; set; }

    public required bool IsPublic { get; set; } = true;

    public required SpecimenStatus Status { get; set; } =  SpecimenStatus.Active;

}