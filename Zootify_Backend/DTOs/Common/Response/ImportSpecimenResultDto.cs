namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The result of a specimen import: how many rows succeeded, how many failed, and the failure details.
/// </summary>
public class ImportSpecimenResultDto
{
    /// <summary>The number of specimens successfully imported.</summary>
    public int ImportedCount { get; init; }

    /// <summary>The number of rows that failed to import.</summary>
    public int FailedCount { get; init; }

    /// <summary>The details of each failed row.</summary>
    public List<ImportSpecimenFailureDto> Failures { get; init; } = [];
}

/// <summary>
/// Describes a single row that failed during specimen import.
/// </summary>
public class ImportSpecimenFailureDto
{
    /// <summary>The 1-based row number in the source workbook.</summary>
    public int RowNumber { get; init; }

    /// <summary>The reason the row could not be imported.</summary>
    public required string Reason { get; init; }
}
