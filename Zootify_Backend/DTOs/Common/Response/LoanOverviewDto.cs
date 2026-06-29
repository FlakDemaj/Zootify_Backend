namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A lightweight active-loan entry used within the loan overview listings.
/// </summary>
public class LoanOverviewDto
{
    /// <summary>The loan's GUID.</summary>
    public Guid LoanGuid { get; init; }

    /// <summary>The loaned specimen's common display name.</summary>
    public required string SpecimenName { get; init; }

    /// <summary>The loaned specimen's latin name.</summary>
    public required string SpecimenLatinName { get; init; }

    /// <summary>The GUID of the borrower.</summary>
    public Guid BorrowerGuid { get; init; }

    /// <summary>The borrower's display name (first and last name combined).</summary>
    public required string BorrowerName { get; init; }

    /// <summary>The date the loan was created.</summary>
    public DateTime LoanDate { get; init; }

    /// <summary>The date by which the specimen must be returned.</summary>
    public DateTime DueDate { get; init; }
    
    /// <summary>The Guid to the Image of the Specimen. </summary>
    public Guid ImageGuid { get; init; }
}
