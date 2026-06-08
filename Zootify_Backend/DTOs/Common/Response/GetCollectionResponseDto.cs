namespace Zootify_Backend.DTOs.Common.Response;

public class GetCollectionResponseDto
{
    public required Guid Guid { get; set; }

    public required string Name { get; set; }
    
    public required bool IsPrivate { get; set; }

    public required int SpecimenCount { get; set; }
}
