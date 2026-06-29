namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The full detail view of a single active loan, including specimen, borrower, and lender information.
/// </summary>
public class LoanDetailDto
{
    /// <summary>The loan's GUID.</summary>
    public Guid LoanGuid { get; init; }

    /// <summary>The GUID of the loaned specimen.</summary>
    public Guid SpecimenGuid { get; init; }

    /// <summary>The loaned specimen's common display name.</summary>
    public required string SpecimenName { get; init; }

    /// <summary>The loaned specimen's latin name.</summary>
    public required string SpecimenLatinName { get; init; }

    /// <summary>The GUID of the specimen's image, if any.</summary>
    public Guid? ImageGuid { get; init; }

    /// <summary>The GUID of the borrower.</summary>
    public Guid BorrowerGuid { get; init; }

    /// <summary>The borrower's display name (first and last name combined).</summary>
    public required string BorrowerName { get; init; }

    /// <summary>The borrower's email address.</summary>
    public required string BorrowerEmail { get; init; }

    /// <summary>The GUID of the lender.</summary>
    public Guid LenderGuid { get; init; }

    /// <summary>The lender's display name (first and last name combined).</summary>
    public required string LenderName { get; init; }

    /// <summary>The date the loan was created.</summary>
    public DateTime LoanDate { get; init; }

    /// <summary>The date by which the specimen must be returned.</summary>
    public DateTime DueDate { get; init; }

    /// <summary>Optional free-text notes attached to the loan.</summary>
    public string? Notes { get; init; }
}
