namespace Zootify_Backend.DTOs.Common.Response.Helpers;

/// <summary>
/// A single tag entry returned by filtered tag queries.
/// </summary>
public class GetAllTagsByFilterDto
{
    /// <summary>The tag's GUID.</summary>
    public required Guid TagGuid { get; init; }

    /// <summary>The tag's name.</summary>
    public required string TagName { get; init; }
}
