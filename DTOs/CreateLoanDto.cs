namespace Zootify_Backend.DTOs;

public class CreateLoanDto
{
    public int SpecimenId { get; set; }
    public int LenderId { get; set; }
    public int? BorrowerId { get; set; }
    public string? BorrowerExternalName { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }
}
