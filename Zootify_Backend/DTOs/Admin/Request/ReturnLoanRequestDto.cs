namespace Zootify_Backend.DTOs.Admin.Request;

/// <summary>
/// Request payload for returning a loaned specimen.
/// </summary>
public class ReturnLoanRequestDto
{
    /// <summary>Optional notes to append to the loan record on return.</summary>
    public string? AppendedNotes { get; init; }
}
