namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllCollectionsPagedResponseDto
{
    public int TotalCount { get; set; }

    public List<GetCollectionResponseDto> Collections { get; set; } = [];
}
