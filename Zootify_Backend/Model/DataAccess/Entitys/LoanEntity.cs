using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Represents a loan of a specimen from a lender to a borrower, with due-date and reminder tracking.
/// </summary>
public class LoanEntity : BaseGuidEntity
{
    public Guid SpecimenGuid { get; set; }
    
    public SpecimenEntity Specimen { get; set; } = null!;

    public Guid LenderGuid { get; set; }

    public UserEntity Lender { get; set; } = null!;

    public Guid BorrowerGuid { get; set; }

    public UserEntity Borrower { get; set; } = null!;

    public DateTime LoanDate { get; set; }
    
    public DateTime DueDate { get; set; }
    
    public DateTime? ReturnDate { get; set; }

    public string? Notes { get; set; }

    public DateTime? Stage1ReminderSentAt { get; set; }

    public DateTime? Stage2ReminderSentAt { get; set; }

    public DateTime? Stage3ReminderSentAt { get; set; }

    /// <summary>
    /// Creates a new loan with the loan date set to now.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the loaned specimen.</param>
    /// <param name="lenderGuid">The GUID of the lending user.</param>
    /// <param name="borrowerGuid">The GUID of the borrowing user.</param>
    /// <param name="dueDate">The date by which the specimen must be returned.</param>
    /// <param name="notes">Optional notes attached to the loan.</param>
    /// <returns>The newly created <see cref="LoanEntity"/>.</returns>
    public static LoanEntity CreateLoanEntity(
        Guid specimenGuid,
        Guid lenderGuid,
        Guid borrowerGuid,
        DateTime dueDate,
        string? notes)
    {
        return new LoanEntity
        {
            SpecimenGuid = specimenGuid,
            LenderGuid   = lenderGuid,
            BorrowerGuid = borrowerGuid,
            LoanDate     = DateTime.UtcNow,
            DueDate      = dueDate,
            Notes        = notes,
        }; 
    }
}
