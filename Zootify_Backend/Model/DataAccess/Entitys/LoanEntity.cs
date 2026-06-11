using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

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
