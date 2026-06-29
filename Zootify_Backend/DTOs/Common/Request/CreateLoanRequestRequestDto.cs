namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for a user requesting to borrow a specimen.
/// </summary>
public class CreateLoanRequestRequestDto
{
    /// <summary>The GUID of the specimen the user wants to borrow.</summary>
    public required Guid SpecimenGuid { get; init; }

    /// <summary>The requester's desired return date, used as the loan due date once approved.</summary>
    public required DateTime DesiredDueDate { get; init; }

    /// <summary>Optional free-text note from the requester.</summary>
    public string? Note { get; init; }
}
