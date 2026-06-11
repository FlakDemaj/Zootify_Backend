namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateTagsDto
{
    public Guid? TagGuid { get; init; }
    
    public required string TagName { get; init; }
}