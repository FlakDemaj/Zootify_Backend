using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateSpecimenRequestDto
{
    public required string Name { get; init; }
    
    public required string LatinName  { get; init; }
    
    public required DateTime DateOfDiscovery { get; init; }
    
    public required string? Comment { get; init; }

    public required bool IsPublic { get; init; } = true;

    public required SpecimenStatus Status { get; init; } =  SpecimenStatus.Active;

}