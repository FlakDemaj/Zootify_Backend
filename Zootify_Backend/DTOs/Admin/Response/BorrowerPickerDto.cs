namespace Zootify_Backend.DTOs.Admin.Response;

/// <summary>
/// Lightweight user entry used to populate a borrower-selection picker.
/// </summary>
public class BorrowerPickerDto
{
    /// <summary>The GUID of the selectable user.</summary>
    public Guid UserGuid { get; init; }

    /// <summary>The user's display name (first and last name combined).</summary>
    public required string FullName { get; init; }
}
