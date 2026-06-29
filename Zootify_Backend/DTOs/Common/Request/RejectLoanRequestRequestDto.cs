namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for rejecting a loan request, carrying the reason shown to the requester.
/// </summary>
public class RejectLoanRequestRequestDto
{
    /// <summary>The rejection reason communicated to the requester.</summary>
    public required string Reason { get; init; }
}
