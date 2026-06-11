namespace Zootify_Backend.DTOs.Common.Request;

public class CreateCollectionRequestDto
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required bool IsPrivate { get; init; }
}
