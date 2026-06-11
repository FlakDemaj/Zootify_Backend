namespace Zootify_Backend.DTOs.Common.Response;

public class GetTaxonomyTreeResponseDto
{
    public int TotalCount { get; init; }
    public List<GetTaxonomyTreeNodeDto> Nodes { get; init; } = [];
}
