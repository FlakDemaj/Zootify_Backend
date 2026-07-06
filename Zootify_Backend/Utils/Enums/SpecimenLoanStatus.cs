namespace Zootify_Backend.Enums;

/// <summary>
/// Indicates whether a specimen is currently loaned out.
/// </summary>
public enum SpecimenLoanStatus
{
    /// <summary>The specimen is not loaned out (has no active loan).</summary>
    Available = 1,

    /// <summary>The specimen is currently loaned out (has a loan that has not been returned).</summary>
    OnLoan = 2
}
