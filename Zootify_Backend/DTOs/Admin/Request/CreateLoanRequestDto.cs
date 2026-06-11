namespace Zootify_Backend.DTOs.Admin.Request;

public class CreateLoanRequestDto
{
    public required Guid SpecimenGuid { get; set; }

    public required Guid BorrowerGuid { get; set; }

    public required DateTime DueDate { get; set; }

    public string? Notes { get; set; }
}
