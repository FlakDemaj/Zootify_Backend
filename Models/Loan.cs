namespace Zootify_Backend.Models;

public class Loan
{
    public int Id { get; set; }

    public int SpecimenId { get; set; }
    public Specimen Specimen { get; set; } = null!;

    public int LenderId { get; set; }
    public User Lender { get; set; } = null!;

    public int? BorrowerId { get; set; }
    public User? Borrower { get; set; }

    public string? BorrowerExternalName { get; set; }

    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? Notes { get; set; }
}
