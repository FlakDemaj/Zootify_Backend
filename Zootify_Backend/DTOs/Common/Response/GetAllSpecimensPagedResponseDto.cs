namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A paged list of specimens together with the total match count.
/// </summary>
public class GetAllSpecimensPagedResponseDto
{
    /// <summary>The total number of matching specimens across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>The specimens on the current page.</summary>
    public List<GetAllSpecimensDto> Specimens { get; init; } = [];
}
