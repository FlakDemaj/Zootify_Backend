namespace Zootify_Backend.DTOs.Base;

/// <summary>
/// Base parameters shared by paged list requests: sorting plus clamped page and page-size values.
/// </summary>
public class BaseSortAndPagingDto
{
    /// <summary>The largest page size a caller may request; larger values are clamped to this.</summary>
    public const int MaxPageSize = 50;

    /// <summary>Backing field for <see cref="Page"/>, defaulting to the first page.</summary>
    private int _page = 1;

    /// <summary>Backing field for <see cref="PageSize"/>, defaulting to 20 items.</summary>
    private int _pageSize = 20;

    /// <summary>Optional name of the field to sort by.</summary>
    public string? SortBy { get; set; }

    /// <summary>The sort direction; defaults to ascending.</summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;

    /// <summary>The 1-based page number; values below 1 are clamped to 1.</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>The page size; clamped to the range [1, <see cref="MaxPageSize"/>].</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : value > MaxPageSize ? MaxPageSize : value;
    }
}
