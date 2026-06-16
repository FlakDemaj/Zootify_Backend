namespace Zootify_Backend.Enums;

/// <summary>
/// The lifecycle state of a specimen.
/// </summary>
public enum SpecimenStatus
{
    /// <summary>Approved and publicly visible.</summary>
    Active = 1,

    /// <summary>Awaiting moderator approval.</summary>
    Pending = 2,

    /// <summary>Reviewed and rejected by a moderator.</summary>
    Rejected = 3,

    /// <summary>Soft-deleted; retained until the purge job removes it permanently.</summary>
    Deleted = 4
}
