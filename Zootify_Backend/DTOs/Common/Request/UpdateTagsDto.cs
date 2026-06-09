namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateTagsDto
{
    public Guid? TagGuid { get; set; }
    
    public required string TagName { get; set; }
}