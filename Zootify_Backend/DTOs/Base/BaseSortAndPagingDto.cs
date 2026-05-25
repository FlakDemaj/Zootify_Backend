namespace Zootify_Backend.DTOs.Base;

public class BaseSortAndPagingDto
{
    public string? SortBy { get; set; }

    public SortDirection SortDirection { get; set; } = SortDirection.ASC;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}