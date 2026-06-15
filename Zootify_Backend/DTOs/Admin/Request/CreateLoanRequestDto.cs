namespace Zootify_Backend.DTOs.Admin.Request;

public class CreateLoanRequestDto
{
    public required Guid SpecimenGuid { get; init; }

    public required Guid BorrowerGuid { get; init; }

    public required DateTime DueDate { get; init; }

    public string? Notes { get; init; }
}
