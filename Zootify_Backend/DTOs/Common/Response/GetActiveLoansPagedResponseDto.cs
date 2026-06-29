namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A paged list of active loans together with the total match count.
/// </summary>
public class GetActiveLoansPagedResponseDto
{
    /// <summary>The total number of active loans across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>The active loans on the current page.</summary>
    public List<LoanOverviewDto> Loans { get; init; } = [];
}
