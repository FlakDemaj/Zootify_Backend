namespace Zootify_Backend.DTOs;

public class LoanDto
{
    public int Id { get; set; }
    public int SpecimenId { get; set; }
    public int LenderId { get; set; }
    public string LenderName { get; set; } = string.Empty;
    public int? BorrowerId { get; set; }
    public string? BorrowerName { get; set; }
    public string? BorrowerExternalName { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsReturned => ReturnDate.HasValue;
    public string? Notes { get; set; }
}
