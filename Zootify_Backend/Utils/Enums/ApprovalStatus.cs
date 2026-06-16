namespace Zootify_Backend.Enums;

/// <summary>
/// The moderation state of a submitted item (e.g. a taxonomy node).
/// </summary>
public enum ApprovalStatus
{
    /// <summary>Awaiting moderator review.</summary>
    Pending = 0,

    /// <summary>Reviewed and approved.</summary>
    Approved = 1,

    /// <summary>Reviewed and rejected.</summary>
    Rejected = 2
}
