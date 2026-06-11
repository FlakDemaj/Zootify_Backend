namespace Zootify_Backend.DTOs.Common.Response;

public class GetTaxonomyTreeResponseDto
{
    public int TotalCount { get; set; }
    public List<GetTaxonomyTreeNodeDto> Nodes { get; set; } = [];
}
