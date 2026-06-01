namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllTagsByFilterDto
{
    public required Guid TagGuid { get; set; }
  
    public required string TagName { get; set; }
}