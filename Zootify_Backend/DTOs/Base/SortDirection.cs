namespace Zootify_Backend.DTOs.Base;

/// <summary>
/// The direction in which a list result should be sorted.
/// </summary>
public enum SortDirection
{
    /// <summary>Ascending order (A→Z, 0→9, oldest first).</summary>
    Asc,

    /// <summary>Descending order (Z→A, 9→0, newest first).</summary>
    Desc,
};
