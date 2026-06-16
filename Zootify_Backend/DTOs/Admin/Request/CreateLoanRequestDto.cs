namespace Zootify_Backend.DTOs.Admin.Request;

/// <summary>
/// Request payload for creating a new loan of a specimen to a borrower.
/// </summary>
public class CreateLoanRequestDto
{
    /// <summary>The GUID of the specimen being loaned.</summary>
    public required Guid SpecimenGuid { get; init; }

    /// <summary>The GUID of the user borrowing the specimen.</summary>
    public required Guid BorrowerGuid { get; init; }

    /// <summary>The date by which the specimen must be returned.</summary>
    public required DateTime DueDate { get; init; }

    /// <summary>Optional free-text notes about the loan.</summary>
    public string? Notes { get; init; }
}
