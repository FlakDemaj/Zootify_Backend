namespace Zootify_Backend.DTOs.Admin.Request;

/// <summary>
/// Request payload for rejecting a submitted taxonomy node.
/// </summary>
public class RejectTaxonomyRequestDto
{
    /// <summary>The reason the taxonomy submission was rejected.</summary>
    public required string Comment { get; init; }
}
