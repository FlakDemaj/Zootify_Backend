namespace Zootify_Backend.DTOs.Common.Response;

public class GetCollectionResponseDto
{
    public required Guid Guid { get; init; }

    public required string Name { get; init; }
    
    public required bool IsPrivate { get; init; }

    public required int SpecimenCount { get; init; }
    
    public DateTime? CreatedAt { get; init; }
}
