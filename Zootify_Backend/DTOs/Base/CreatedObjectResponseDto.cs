namespace Zootify_Backend.DTOs.Base;

/// <summary>
/// Generic response returning the GUID of a newly created object.
/// </summary>
public class CreatedObjectResponseDto
{
    /// <summary>The GUID of the object that was just created.</summary>
    public Guid CreatedObjectGuid { get; init; }
}
