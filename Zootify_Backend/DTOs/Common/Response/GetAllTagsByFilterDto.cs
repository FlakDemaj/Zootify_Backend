namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllTagsByFilterDto
{
    public required Guid TagGuid { get; init; }
  
    public required string TagName { get; init; }
}