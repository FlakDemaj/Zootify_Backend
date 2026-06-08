namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateCollectionRequestDto
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public required bool IsPrivate { get; set; }
}
