namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllCollectionsPagedResponseDto
{
    public int TotalCount { get; init; }

    public List<GetCollectionResponseDto> Collections { get; init; } = [];
}
