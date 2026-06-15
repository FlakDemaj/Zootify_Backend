namespace Zootify_Backend.DTOs.Common.Response;

public class GetPublicSpecimenOverviewResponseDto
{
    public int TotalCount { get; init; }

    public List<PublicSpecimenOverviewDto> Specimens { get; init; } = [];
}