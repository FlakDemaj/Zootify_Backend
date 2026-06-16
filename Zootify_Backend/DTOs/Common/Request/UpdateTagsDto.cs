namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// A single tag entry used when updating a specimen's tags; either references an existing tag or names a new one.
/// </summary>
public class UpdateTagsDto
{
    /// <summary>The GUID of an existing tag to link, or <c>null</c> to create a new tag from <see cref="TagName"/>.</summary>
    public Guid? TagGuid { get; init; }

    /// <summary>The tag's name (used when creating a new tag).</summary>
    public required string TagName { get; init; }
}
